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

            // TODO: 播放射击音效
            // GameModule.Audio.Play(AudioType.Sound, Config.fireSound);
        }

        public void Reload()
        {
            if (IsReloading || CurrentAmmo >= Config.clipSize) return;

            IsReloading = true;

            GameModule.Timer.AddTimer((args) =>
            {
                CurrentAmmo = Config.clipSize;
                IsReloading = false;
                GameEvent.Get<IPlayerEvent>().OnAmmoChanged(CurrentAmmo, Config.clipSize);

                // TODO: 播放换弹完成音效
            }, Config.reloadTime);

            // TODO: 播放换弹音效
        }

        public float CalculateSpread(bool isMoving, bool isAiming)
        {
            float spread = Config.baseSpread + CurrentSpreadIncrement;

            if (isMoving) spread *= Config.moveSpreadMultiplier;
            if (isAiming) spread *= Config.aimSpreadMultiplier;

            return spread;
        }
    }
}
