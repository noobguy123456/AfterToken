using GameConfig.cfg;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器运行时实例。
    /// </summary>
    public class WeaponInstance
    {
        public WeaponConfig Config { get; private set; }
        public int CurrentAmmo { get; private set; }
        public float CurrentSpreadIncrement { get; private set; }
        public float LastFireTime { get; private set; } = -100f;
        public bool IsReloading { get; private set; }
        public bool IsFiring { get; set; }

        /// <summary>
        /// 狙击镜 FOV（越小放大倍率越高；<b>0 = 无放大</b>，纯视觉镜窗：灰色蒙版 + 镜窗图案）。
        /// 基础值读 TbWeapon.scopeFov；后续武器配件系统接入时在此叠加配件修正
        /// （如高倍镜配件乘以配件系数），调用方（WeaponSystem/狙击镜 UI）只读本属性，不关心来源。
        /// </summary>
        public float ScopeFov => Config != null ? Config.scopeFov : 0f;

        /// <summary>
        /// 技能树加成后的弹匣容量（武器系统为玩家专属，直接读玩家技能加成）。
        /// 所有弹药上限判断/换弹填充/UI 显示统一走该值，不再直接用 Config.clipSize。
        /// </summary>
        public int EffectiveClipSize => Config != null
            ? Config.clipSize + Mathf.RoundToInt(SkillSystem.GetEffect(ESkillEffect.ClipSizeAdd))
            : 0;

        /// <summary>
        /// 技能树加成后的换弹时间（秒）。
        /// </summary>
        public float EffectiveReloadTime => Config != null
            ? Config.reloadTime * (1f - SkillSystem.GetEffect(ESkillEffect.ReloadTimePct))
            : 0f;

        private int _reloadTimerId;

        public WeaponInstance(WeaponConfig config)
        {
            Config = config;
            CurrentAmmo = EffectiveClipSize;
            CurrentSpreadIncrement = 0;
        }

        /// <summary>
        /// 释放武器实例资源（如未完成的换弹计时器）。
        /// </summary>
        public void Dispose()
        {
            if (IsReloading && _reloadTimerId != 0)
            {
                GameModule.Timer.RemoveTimer(_reloadTimerId);
                _reloadTimerId = 0;
            }
            IsReloading = false;
            Config = null;
        }

        /// <summary>
        /// 设置当前弹药数（用于跨场景状态保留）。
        /// </summary>
        public void SetAmmo(int ammo)
        {
            if (Config == null) return;
            CurrentAmmo = Mathf.Clamp(ammo, 0, EffectiveClipSize);
        }

        public void Tick(float deltaTime, bool isMoving, bool isAiming)
        {
            // 扩散恢复
            if (CurrentSpreadIncrement > 0)
            {
                CurrentSpreadIncrement -= Config.spreadRecoveryRate * deltaTime;
                if (CurrentSpreadIncrement < 0)
                {
                    CurrentSpreadIncrement = 0;
                }
            }
        }

        public bool CanFire(float currentTime)
        {
            if (IsReloading) return false;
            if (CurrentAmmo <= 0) return false;
            return currentTime - LastFireTime >= 1f / Config.fireRate;
        }

        public void Fire(Vector2 origin, Vector2 direction, int ownerId)
        {
            CurrentAmmo--;
            LastFireTime = Time.time;
            CurrentSpreadIncrement += Config.fireSpreadIncrement;

            GameEvent.Get<IWeaponEvent>().OnFire(origin, direction, Config.id, ownerId);
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, EffectiveClipSize);

            // 弹匣打空后自动换弹
            if (CurrentAmmo <= 0)
            {
                Reload(ownerId);
            }

            // TODO: 播放射击音效
            // GameModule.Audio.Play(AudioType.Sound, Config.fireSound);
        }

        public void Reload(int ownerId)
        {
            if (IsReloading || CurrentAmmo >= EffectiveClipSize) return;

            IsReloading = true;
            GameEvent.Get<IWeaponEvent>().OnReloadStateChanged(ownerId, true);

            _reloadTimerId = GameModule.Timer.AddTimer((args) =>
            {
                CurrentAmmo = EffectiveClipSize;
                IsReloading = false;
                _reloadTimerId = 0;
                GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, EffectiveClipSize);
                GameEvent.Get<IWeaponEvent>().OnReloadStateChanged(ownerId, false);

                // TODO: 播放换弹完成音效
            }, EffectiveReloadTime);

            // TODO: 播放换弹音效
        }

        /// <summary>
        /// 取消当前换弹。用于切换武器等需要中断换弹的场景。
        /// </summary>
        public void CancelReload(int ownerId)
        {
            if (!IsReloading) return;

            if (_reloadTimerId != 0)
            {
                GameModule.Timer.RemoveTimer(_reloadTimerId);
                _reloadTimerId = 0;
            }

            IsReloading = false;
            GameEvent.Get<IWeaponEvent>().OnReloadStateChanged(ownerId, false);
        }

        public float CalculateSpread(bool isMoving, bool isAiming)
        {
            float spread = Config.baseSpread + CurrentSpreadIncrement;

            if (isMoving) spread *= Config.moveSpreadMultiplier;
            if (isAiming) spread *= Config.aimSpreadMultiplier;

            return spread;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// GM：设置当前弹药。
        /// </summary>
        public void GM_SetAmmo(int ammo)
        {
            CurrentAmmo = Mathf.Clamp(ammo, 0, EffectiveClipSize);
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, EffectiveClipSize);
        }
#endif
    }
}
