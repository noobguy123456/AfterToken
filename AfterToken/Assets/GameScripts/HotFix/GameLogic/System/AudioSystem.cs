using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig.cfg;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 语音子通道（玩家/NPC 分开调音量，共用 Voice mixer 组）。
    /// </summary>
    public enum VoiceChannel
    {
        Player,
        Npc,
    }

    /// <summary>
    /// 音频系统（业务门面，persistent）：统一封装 GameModule.Audio，游戏代码只跟它打交道。
    /// - BGM：场景键表驱动（TbAudio.sceneKey），双 agent 交叉淡入淡出；战斗态切战斗 BGM 并 duck（压低 BGM 突出战场音效）。
    /// - SFX：Play2D / Play3D（世界坐标 + spatialBlend=1 + 表配衰减距离）/ PlayUI。
    /// - Voice：玩家/NPC 两个子通道独立音量；对话跳过即停；<see cref="VoiceProvider"/> 为 AI TTS 预留。
    /// - 战斗态判定：订阅 IEnemyEvent 聚合追击/攻击敌人数（CompanionSystem 同款模式），归零 3s 后退出。
    /// 音量语义：mixer 组音量=用户设置（VolumeSetting）；agent 音量=表配 volume × duck（BGM）或 × 子通道音量（Voice）。
    /// </summary>
    public class AudioSystem : MonoBehaviour
    {
        /// <summary>BGM 交叉淡入淡出时长（秒）。</summary>
        private const float BgmFadeSeconds = 1f;
        /// <summary>战斗态 BGM duck 系数（压低到 50%）。</summary>
        private const float CombatDuckFactor = 0.5f;
        /// <summary>duck 系数平滑速度（每秒变化量）。</summary>
        private const float DuckLerpSpeed = 2f;
        /// <summary>脱战缓冲（秒）：威胁归零后该时长内无新威胁才退出战斗态。</summary>
        private const float CombatExitDelay = 3f;

        public static AudioSystem Instance { get; private set; }

        /// <summary>AI 语音提供器（TTS 预留，默认 null = 全走占位音）。</summary>
        public IVoiceProvider VoiceProvider { get; set; }

        // ── BGM 状态 ──
        private AudioAgent _bgmCurrent;
        private AudioAgent _bgmFading; // 正在淡出的旧 agent
        private float _bgmFadeTimer;
        private float _bgmTargetVolume = 1f; // 表配音量（fade 完成后的目标）
        private string _sceneKey;
        private float _duckFactor = 1f;
        private bool _inCombat;
        private float _combatExitTimer;

        // ── 语音状态 ──
        private readonly Dictionary<VoiceChannel, AudioAgent> _voiceAgents = new Dictionary<VoiceChannel, AudioAgent>(2);
        private CancellationTokenSource _voiceCts;

        // ── 战斗态感知 ──
        private readonly HashSet<int> _threats = new HashSet<int>();
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        /// <summary>启动时由 GameApp 调用一次（persistent）。</summary>
        public static void EnsureCreated()
        {
            if (Instance != null)
            {
                return;
            }
            var go = new GameObject("AudioSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioSystem>();
        }

        private void Awake()
        {
            Instance = this;
            _voiceCts = new CancellationTokenSource();

            _eventMgr.AddEvent<int, string, string>(IEnemyEvent_Event.OnEnemyStateChanged, OnEnemyStateChanged);
            _eventMgr.AddEvent<int, int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
            _eventMgr.AddEvent<Vector2, Vector2, int, int>(IWeaponEvent_Event.OnFire, OnWeaponFire);
            _eventMgr.AddEvent<int>(IWeaponEvent_Event.OnReload, OnWeaponReload);
            _eventMgr.AddEvent<int, int>(IBattleEvent_Event.OnPickupCollected, OnPickupCollected);
            _eventMgr.AddEvent<string, Vector3, Quaternion, EffectContext>(IEffectEvent_Event.OnPlayEffect, OnPlayEffect);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            _voiceCts?.Cancel();
            _voiceCts?.Dispose();
            _voiceCts = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // duck 系数平滑趋近目标
            float duckTarget = _inCombat ? CombatDuckFactor : 1f;
            if (!Mathf.Approximately(_duckFactor, duckTarget))
            {
                _duckFactor = Mathf.MoveTowards(_duckFactor, duckTarget, DuckLerpSpeed * dt);
                ApplyBgmVolume();
            }

            // BGM 交叉淡入淡出
            if (_bgmFading != null)
            {
                _bgmFadeTimer += dt;
                float t = Mathf.Clamp01(_bgmFadeTimer / BgmFadeSeconds);
                _bgmFading.Volume = Mathf.Lerp(_bgmFading.Volume, 0f, t);
                if (_bgmCurrent != null)
                {
                    _bgmCurrent.Volume = Mathf.Lerp(0f, _bgmTargetVolume * _duckFactor, t);
                }
                if (t >= 1f)
                {
                    _bgmFading.Stop(fadeout: false);
                    _bgmFading = null;
                    ApplyBgmVolume();
                }
            }

            // 脱战缓冲计时
            if (_inCombat && _threats.Count == 0)
            {
                _combatExitTimer += dt;
                if (_combatExitTimer >= CombatExitDelay)
                {
                    ExitCombat();
                }
            }
        }

        // ── BGM ──

        /// <summary>切场景 BGM（Procedure 在场景加载完成后调用）。同键重复调用忽略。</summary>
        public void PlaySceneBgm(string sceneKey)
        {
            if (sceneKey == _sceneKey && _bgmCurrent != null)
            {
                return;
            }
            _sceneKey = sceneKey;

            // 战斗态只在战斗场景生效；切走立即复原
            var cfg = AudioConfigMgr.Instance.GetSceneBgm(EffectiveSceneKey());
            if (cfg == null)
            {
                Log.Warning($"[AudioSystem] 场景 {sceneKey} 无 BGM 配置");
                return;
            }
            CrossfadeTo(cfg);
        }

        /// <summary>当前应播的 BGM 场景键：战斗中且身处战斗场景 → Combat。</summary>
        private string EffectiveSceneKey()
        {
            return _inCombat && _sceneKey == "Battle" ? "Combat" : _sceneKey;
        }

        private void CrossfadeTo(Audio cfg)
        {
            _bgmTargetVolume = cfg.Volume > 0.001f ? cfg.Volume : 1f;
            var agent = GameModule.Audio.Play(TEngine.AudioType.Music, cfg.Name, bLoop: true, volume: 0f, bAsync: true);
            if (agent == null)
            {
                return;
            }

            // Play 可能复用正在播放的 agent（同 agent 直接换曲，不交叉）
            if (agent == _bgmCurrent)
            {
                _bgmCurrent = agent;
                _bgmFading = null;
                ApplyBgmVolume();
                return;
            }

            if (_bgmFading != null)
            {
                _bgmFading.Stop(fadeout: false);
            }
            _bgmFading = _bgmCurrent;
            _bgmCurrent = agent;
            _bgmFadeTimer = 0f;

            if (_bgmFading == null)
            {
                // 无旧曲：直接到位，不淡入
                ApplyBgmVolume();
            }
        }

        private void ApplyBgmVolume()
        {
            if (_bgmCurrent != null && _bgmFading == null)
            {
                _bgmCurrent.Volume = _bgmTargetVolume * _duckFactor;
            }
        }

        // ── 战斗态（需求5：切战斗 BGM + duck 突出战场信息）──

        private void OnEnemyStateChanged(int enemyId, string stateName, string previousStateName)
        {
            if (stateName == "Chase" || stateName == "Attack")
            {
                _threats.Add(enemyId);
            }
            else
            {
                _threats.Remove(enemyId);
            }
            RefreshCombatState();
        }

        private void OnEnemyDied(int enemyId, int configId)
        {
            _threats.Remove(enemyId);
            RefreshCombatState();
        }

        private void RefreshCombatState()
        {
            if (_threats.Count > 0)
            {
                _combatExitTimer = 0f;
                if (!_inCombat)
                {
                    _inCombat = true;
                    SwitchCombatBgm();
                }
            }
            // 归零不立即退出，由 Update 的缓冲计时处理
        }

        private void ExitCombat()
        {
            _inCombat = false;
            _combatExitTimer = 0f;
            SwitchCombatBgm();
        }

        private void SwitchCombatBgm()
        {
            if (_sceneKey != "Battle")
            {
                return; // 非战斗场景只 duck 不切曲（战斗信号本就只在战斗场景产生）
            }
            var cfg = AudioConfigMgr.Instance.GetSceneBgm(EffectiveSceneKey());
            if (cfg != null)
            {
                CrossfadeTo(cfg);
            }
        }

        // ── SFX ──

        /// <summary>2D 音效（玩家自身开火、换弹、拾取等近场音）。</summary>
        public void Play2D(string audioName)
        {
            var cfg = AudioConfigMgr.Instance.GetByName(audioName);
            if (cfg == null)
            {
                Log.Warning($"[AudioSystem] 未找到音频配置: {audioName}");
                return;
            }
            var agent = GameModule.Audio.Play(TEngine.AudioType.Sound, cfg.Name, cfg.Loop, cfg.Volume, bAsync: true);
            if (agent != null)
            {
                // agent 池复用：清掉上一次 Play3D 留下的空间化设置
                agent.AudioResource().spatialBlend = 0f;
            }
        }

        /// <summary>3D 空间音效（敌人开火/脚步/爆炸）：世界坐标定位 + 表配距离衰减。</summary>
        public void Play3D(string audioName, Vector3 worldPos)
        {
            var cfg = AudioConfigMgr.Instance.GetByName(audioName);
            if (cfg == null)
            {
                Log.Warning($"[AudioSystem] 未找到音频配置: {audioName}");
                return;
            }
            var agent = GameModule.Audio.Play(TEngine.AudioType.Sound, cfg.Name, cfg.Loop, cfg.Volume, bAsync: true);
            if (agent == null)
            {
                return;
            }
            var src = agent.AudioResource();
            src.spatialBlend = 1f;
            if (cfg.Spatial)
            {
                src.minDistance = cfg.MinDist;
                src.maxDistance = cfg.MaxDist;
            }
            agent.Position = worldPos;
        }

        /// <summary>UI 音效（按钮点击等）。</summary>
        public void PlayUI(string audioName)
        {
            var cfg = AudioConfigMgr.Instance.GetByName(audioName);
            if (cfg == null)
            {
                return; // UI 音效静默缺失，不刷警告
            }
            GameModule.Audio.Play(TEngine.AudioType.UISound, cfg.Name, cfg.Loop, cfg.Volume, bAsync: true);
        }

        // ── Voice（需求4：跳过即停；需求6：AI TTS 预留）──

        /// <summary>
        /// 播对话语音：voiceClip 为空走占位 blip；speaker 决定子通道音量。
        /// 同通道新语音自动顶掉旧语音（=跳过语义）。
        /// </summary>
        public void PlayDialogueVoice(string voiceClip, VoiceChannel channel = VoiceChannel.Npc)
        {
            PlayVoiceClip(string.IsNullOrEmpty(voiceClip) ? "voice_blip" : voiceClip, channel);
        }

        /// <summary>
        /// AI 队友语音入口（预留）：已注册 VoiceProvider 时按文本合成，否则回退占位 blip。
        /// </summary>
        public void PlayCompanionVoice(string speakerId, string text)
        {
            if (VoiceProvider == null || string.IsNullOrWhiteSpace(text))
            {
                PlayVoiceClip("voice_blip", VoiceChannel.Npc);
                return;
            }
            PlayAiVoiceAsync(speakerId, text).Forget();
        }

        private async UniTaskVoid PlayAiVoiceAsync(string speakerId, string text)
        {
            var clip = await VoiceProvider.RequestClipAsync(speakerId, text, _voiceCts.Token);
            if (clip == null)
            {
                PlayVoiceClip("voice_blip", VoiceChannel.Npc);
                return;
            }
            PlayClipDirect(clip, VoiceChannel.Npc);
        }

        private void PlayVoiceClip(string audioName, VoiceChannel channel)
        {
            var cfg = AudioConfigMgr.Instance.GetByName(audioName);
            if (cfg == null)
            {
                return;
            }
            var agent = GameModule.Audio.Play(TEngine.AudioType.Voice, cfg.Name, bLoop: false,
                volume: cfg.Volume * GetVoiceChannelVolume(channel), bAsync: true);
            if (agent != null)
            {
                _voiceAgents[channel] = agent;
            }
        }

        /// <summary>直接播一个 AudioClip（AI TTS 用）：复用 Voice 通道 agent 的输出组，clip 手动装载。</summary>
        private void PlayClipDirect(AudioClip clip, VoiceChannel channel)
        {
            // Voice agent 由 AudioModule 池管理、只认表内地址；TTS 动态 clip 走独立临时源
            var go = new GameObject("AiVoiceSource");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.clip = clip;
            src.volume = GetVoiceChannelVolume(channel);
            src.Play();
            Destroy(go, clip.length + 0.1f);
        }

        private float GetVoiceChannelVolume(VoiceChannel channel)
        {
            return channel == VoiceChannel.Player ? VolumeSetting.VoicePlayer : VolumeSetting.VoiceNpc;
        }

        /// <summary>停指定子通道语音（对话跳过/结束时调用）。</summary>
        public void StopVoice(VoiceChannel channel)
        {
            if (_voiceAgents.TryGetValue(channel, out var agent) && agent != null && !agent.IsFree)
            {
                agent.Stop(fadeout: false);
            }
            _voiceAgents.Remove(channel);
        }

        // ── 事件接线（需求3：空间音效）──

        private void OnWeaponFire(Vector2 origin, Vector2 direction, int weaponConfigId, int ownerId)
        {
            var weapon = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            string fireSound = weapon != null ? weapon.fireSound : null;
            if (string.IsNullOrEmpty(fireSound))
            {
                return;
            }

            if (IsPlayerOwned(ownerId))
            {
                Play2D(fireSound); // 玩家自己开火：2D 近场
            }
            else
            {
                Play3D(fireSound, origin.ToWorld(0.5f)); // 敌人/队友开火：3D 定位
            }
        }

        private void OnWeaponReload(int ownerId)
        {
            if (IsPlayerOwned(ownerId))
            {
                Play2D("sfx_reload");
            }
        }

        private void OnPickupCollected(int pickupId, int collectorId)
        {
            Play2D("sfx_pickup");
        }

        private void OnPlayEffect(string address, Vector3 pos, Quaternion rot, EffectContext ctx)
        {
            if (address == EffectIds.Explosion)
            {
                Play3D("sfx_explosion", pos);
            }
        }

        private static bool IsPlayerOwned(int ownerId)
        {
            var player = PlayerSystem.Instance != null ? PlayerSystem.Instance.GetPlayerEntity() : null;
            return player != null && ownerId == player.gameObject.GetInstanceID();
        }
    }
}
