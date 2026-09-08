using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 特效驱动。挂在特效 prefab 根节点，视觉网格由 prefab 子节点提供。
    /// 特效元数据（时长/池容量/挂载方式/缩放/预热）以序列化字段挂在 prefab 上，
    /// EffectSystem 加载 prefab 后直接从资产上读取（无需实例化）。
    /// 两条时间轴路径（按序列化字段自动分派）：
    /// 1. 爆炸（_fireball 非空）：预警压缩闪光（10% 尺度高亮白闪）→ 主体（火球膨胀+噪声侵蚀、
    ///    冲击波领先外扩，沿用 docs/Proposal/combat/explosion-shader-proposal.md §3.3）→
    ///    余韵（_scorchDecal 焦痕线性淡出，方案 §7.2 三段式）。
    /// 2. 通用面片（_fireball 为空、_genericVisual 非空）：单面片缩放曲线 + _Progress 推进
    ///    （枪口火焰/命中火花/拾取光晕等，颜色与形状由各自 shader/材质承担）。
    /// 生命周期由 EffectSystem 按 duration 到点回收；duration=0 = 持久特效（跟随目标销毁时回收），
    /// 也可由驱动自报 OnFinished。材质参数一律走 MaterialPropertyBlock，禁止 renderer.material 实例化。
    /// </summary>
    public class EffectDriver : MonoBehaviour
    {
        /// <summary>
        /// 挂载方式：World = 生成即定死；Follow = 跟随传入的 Transform。
        /// </summary>
        public enum AttachMode
        {
            World,
            Follow,
        }

        [Header("特效元数据")]
        [Tooltip("播放时长（秒），到时回收；0 = 持久特效（跟随目标销毁时回收）或由驱动自报完成")]
        [SerializeField] private float _duration = 0.5f;
        [Tooltip("池上限（满则强制回收最老实例）")]
        [SerializeField] private int _poolCapacity = 8;
        [Tooltip("World = 生成即定死；Follow = 跟随传入的 Transform")]
        [SerializeField] private AttachMode _attachMode = AttachMode.World;
        [Tooltip("是否按半径缩放（EffectContext.Scale = 半径）")]
        [SerializeField] private bool _scaleByRadius = true;
        [Tooltip("进战斗是否预热（仅对 EffectSystem.PreloadEffects 注册列表中的特效生效）")]
        [SerializeField] private bool _preload = true;

        [Header("爆炸视觉节点（_fireball 非空时走爆炸时间轴）")]
        [SerializeField] private Renderer _fireball;
        [SerializeField] private Renderer _shockwave;
        [Tooltip("预警段时长（秒）：火球 10% 尺度高亮白闪（_Flash=1），冲击波不露头")]
        [SerializeField] private float _warningDuration = 0.08f;
        [Tooltip("主体段总时长（秒，含预警段）：火球膨胀+侵蚀、冲击波外扩")]
        [SerializeField] private float _blastDuration = 0.6f;
        [Tooltip("焦痕 decal（可选）：主体段开始显现，主体段结束后缓慢淡出")]
        [SerializeField] private Renderer _scorchDecal;
        [Tooltip("焦痕淡出时长（秒）")]
        [SerializeField] private float _scorchFadeDuration = 4f;

        [Header("通用面片视觉（_fireball 为空时生效）")]
        [SerializeField] private Renderer _genericVisual;
        [Tooltip("起始缩放系数（乘子节点设计尺寸与 EffectContext.Scale）")]
        [SerializeField] private float _startScale = 0.6f;
        [Tooltip("结束缩放系数")]
        [SerializeField] private float _endScale = 1.2f;

        private const float FALLBACK_DURATION = 0.5f;

        /// <summary>播放时长（秒）。</summary>
        public float Duration => _duration;
        /// <summary>池上限。</summary>
        public int PoolCapacity => _poolCapacity;
        /// <summary>挂载方式。</summary>
        public AttachMode Mode => _attachMode;
        /// <summary>是否按半径缩放。</summary>
        public bool ScaleByRadius => _scaleByRadius;
        /// <summary>进战斗是否预热。</summary>
        public bool Preload => _preload;

        private Transform _fireballTf;
        private Transform _shockwaveTf;
        private Transform _scorchTf;
        private Transform _genericTf;
        private Vector3 _genericBaseScale = Vector3.one;
        private MaterialPropertyBlock _mpb;
        private float _t;
        private float _durationRt = FALLBACK_DURATION;
        private float _radius = 1f;
        private bool _playing;

        /// <summary>
        /// duration=0 时由驱动自报播放完成（当前爆炸/面片走 EffectSystem 定时或跟随回收，此事件备用）。
        /// </summary>
        public event Action<EffectDriver> OnFinished;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (_fireball != null)
            {
                _fireballTf = _fireball.transform;
            }
            if (_shockwave != null)
            {
                _shockwaveTf = _shockwave.transform;
            }
            if (_scorchDecal != null)
            {
                _scorchTf = _scorchDecal.transform;
            }
            if (_genericVisual != null)
            {
                _genericTf = _genericVisual.transform;
                _genericBaseScale = _genericTf.localScale;
            }
        }

        /// <summary>
        /// 播放前由 EffectSystem 调用。ctx.Scale 按爆炸半径（或通用缩放系数）解释；时长取序列化 _duration。
        /// </summary>
        public void Init(in EffectContext ctx)
        {
            _t = 0f;
            _radius = ctx.Scale > 0f ? ctx.Scale : 1f;
            _durationRt = _duration > 0f ? _duration : FALLBACK_DURATION;
            _playing = true;
            DispatchFrame();
        }

        /// <summary>
        /// 池回收时复位状态（时间轴归零、视觉回到初始帧）。
        /// </summary>
        public void OnRecycle()
        {
            _playing = false;
            _t = 0f;
            OnFinished = null;
            DispatchFrame();
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _t += Time.deltaTime;
            DispatchFrame();

            // duration=0 = 持久特效（跟随目标销毁时由 EffectSystem 回收），不到点自报。
            if (_duration > 0f && _t >= _durationRt)
            {
                // 正常路径由 EffectSystem 按 duration 到点回收；此处兜底自报，防驱动游离在外永不回收。
                var handler = OnFinished;
                if (handler != null)
                {
                    handler(this);
                }
                else
                {
                    _playing = false;
                }
            }
        }

        private void DispatchFrame()
        {
            if (_fireball != null)
            {
                ApplyExplosionFrame(_t);
            }
            else if (_genericVisual != null)
            {
                float p = _duration > 0f ? Mathf.Clamp01(_t / _durationRt) : 0f;
                ApplyGenericFrame(p);
            }
        }

        /// <summary>
        /// 爆炸三段式时间轴：预警压缩闪光（0~_warningDuration）→ 主体火球/冲击波（~_blastDuration）→ 焦痕淡出。
        /// </summary>
        private void ApplyExplosionFrame(float t)
        {
            float warn = Mathf.Clamp(_warningDuration, 0f, _blastDuration);
            float blast = Mathf.Max(_blastDuration, warn + 0.01f);

            bool warning = t < warn;
            // 主体段归一化进度（预警段 p=0：噪声侵蚀阈值≈0、位移幅度≈0，呈高亮静态球）
            float p = warning ? 0f : Mathf.Clamp01((t - warn) / (blast - warn));

            // 火球膨胀曲线：预警段定死 10% 尺度（压缩闪光），主体段 0.1→0.6 快膨再缓增至 100%
            float grow = warning
                ? 0.1f
                : p < 0.16f
                    ? Mathf.Lerp(0.1f, 0.6f, p / 0.16f)
                    : Mathf.Lerp(0.6f, 1f, (p - 0.16f) / 0.84f);
            // 球体网格直径 1，放大到爆炸直径
            if (_fireballTf != null)
            {
                _fireballTf.localScale = Vector3.one * (_radius * 2f * grow);
            }

            // 冲击波快速外扩并越过火球 footprint（火球是不透明半球，同心同速会被完全遮住），
            // ease-out 到 1.5 倍半径，俯视下圆环始终领先火球轮廓可见；预警段不露头
            if (_shockwaveTf != null)
            {
                float waveScale = warning ? 0f : _radius * 2f * Mathf.Lerp(0.4f, 1.5f, Mathf.Sqrt(p));
                _shockwaveTf.localScale = new Vector3(waveScale, waveScale, 1f);
            }

            // 焦痕：预警段不显现，主体段起定住（被火球遮住无妨），主体段结束后线性淡出
            float scorchFade = 0f;
            if (_scorchDecal != null)
            {
                if (t >= blast)
                {
                    scorchFade = 1f - Mathf.Clamp01((t - blast) / Mathf.Max(_scorchFadeDuration, 0.01f));
                }
                else if (!warning)
                {
                    scorchFade = 1f;
                }
                if (_scorchTf != null)
                {
                    float scorchScale = _radius * 2f * 0.85f;
                    _scorchTf.localScale = new Vector3(scorchScale, scorchScale, 1f);
                }
            }

            _mpb.SetFloat("_Progress", p);
            _mpb.SetFloat("_Flash", warning ? 1f : 0f);
            if (_fireball != null)
            {
                _fireball.SetPropertyBlock(_mpb);
            }
            if (_shockwave != null)
            {
                _shockwave.SetPropertyBlock(_mpb);
            }
            if (_scorchDecal != null)
            {
                _mpb.SetFloat("_Fade", scorchFade);
                _scorchDecal.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>
        /// 通用面片时间轴：缩放 ease-out 从 _startScale 到 _endScale，_Progress 推进给 shader 做衰减。
        /// </summary>
        private void ApplyGenericFrame(float p)
        {
            if (_genericTf != null)
            {
                float s = Mathf.Lerp(_startScale, _endScale, Mathf.Sqrt(p)) * _radius;
                _genericTf.localScale = _genericBaseScale * s;
            }

            _mpb.SetFloat("_Progress", p);
            if (_genericVisual != null)
            {
                _genericVisual.SetPropertyBlock(_mpb);
            }
        }
    }
}
