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

        private int _reloadTimerId;

        public WeaponInstance(WeaponConfig config)
        {
            Config = config;
            CurrentAmmo = config.clipSize;
            CurrentSpreadIncrement = 0;
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
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, Config.clipSize);

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
            if (IsReloading || CurrentAmmo >= Config.clipSize) return;

            IsReloading = true;
            GameEvent.Get<IWeaponEvent>().OnReloadStateChanged(ownerId, true);

            _reloadTimerId = GameModule.Timer.AddTimer((args) =>
            {
                CurrentAmmo = Config.clipSize;
                IsReloading = false;
                _reloadTimerId = 0;
                GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, Config.clipSize);
                GameEvent.Get<IWeaponEvent>().OnReloadStateChanged(ownerId, false);

                // TODO: 播放换弹完成音效
            }, Config.reloadTime);

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
            CurrentAmmo = Mathf.Clamp(ammo, 0, Config.clipSize);
            GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, Config.clipSize);
        }
#endif
    }
}
