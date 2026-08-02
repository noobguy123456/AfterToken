using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 经营时间系统：推进时间、暂停、加速，并广播时间事件。
    /// </summary>
    public class SimTimeSystem : MonoBehaviour
    {
        private float _currentTime;
        private ESimSpeed _speed = ESimSpeed.Normal;
        private bool _isPaused;

        public float CurrentTime => _currentTime;
        public ESimSpeed Speed => _speed;
        public bool IsPaused => _isPaused || _speed == ESimSpeed.Pause;

        private void Update()
        {
            if (IsPaused)
            {
                return;
            }

            if (Time.frameCount % 15 == 0) Log.Info($"[hb] SimTime f={Time.frameCount}");

            float speedMultiplier = GetSpeedMultiplier(_speed);
            float deltaTime = Time.deltaTime * speedMultiplier;
            _currentTime += deltaTime;

            GameEvent.Get<ISimulationEvent>().OnSimulationTimeAdvanced(deltaTime, _currentTime);
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public void SetSpeed(ESimSpeed speed)
        {
            if (_speed == speed)
            {
                return;
            }
            _speed = speed;
            GameEvent.Get<ISimulationEvent>().OnSimulationSpeedChanged(speed);
        }

        public void ResetTime()
        {
            _currentTime = 0f;
        }

        private float GetSpeedMultiplier(ESimSpeed speed)
        {
            var cfg = SimTimeConfigMgr.Instance;
            return speed switch
            {
                ESimSpeed.Pause => 0f,
                ESimSpeed.Normal => cfg.BaseSpeed,
                ESimSpeed.Fast => cfg.FastSpeed,
                ESimSpeed.Max => cfg.MaxSpeed,
                _ => 1f,
            };
        }
    }
}
