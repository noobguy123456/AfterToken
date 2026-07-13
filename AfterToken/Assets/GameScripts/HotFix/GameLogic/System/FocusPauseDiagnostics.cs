using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 用于诊断 Alt+Tab 后战斗卡死问题的临时日志组件。
    /// </summary>
    public class FocusPauseDiagnostics : MonoBehaviour
    {
        private static bool _domainReloaded;
        private bool _lastFocused = true;
        private bool _lastPaused;
        private float _lastTimeScale = 1f;
        private int _frameCounter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnDomainReload()
        {
            _domainReloaded = true;
            Debug.LogWarning("[FocusDiag] >>> DOMAIN RELOAD DETECTED <<<");
        }

        private void OnEnable()
        {
            Debug.LogWarning("[FocusDiag] OnEnable");
            if (_domainReloaded)
            {
                _domainReloaded = false;
                LogState("AfterDomainReload");
            }
        }

        private void Update()
        {
            _frameCounter++;
            bool focused = Application.isFocused;
            bool paused = Time.timeScale <= Mathf.Epsilon;
            if (focused != _lastFocused || paused != _lastPaused || Mathf.Abs(Time.timeScale - _lastTimeScale) > 0.01f)
            {
                _lastFocused = focused;
                _lastPaused = paused;
                _lastTimeScale = Time.timeScale;
                LogState($"Frame{_frameCounter}");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.LogWarning($"[FocusDiag] OnApplicationPause: {pauseStatus}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.LogWarning($"[FocusDiag] OnApplicationFocus: {hasFocus}");
        }

        private void LogState(string tag)
        {
            var playerSystem = PlayerSystem.Instance;
            var weaponSystem = WeaponSystem.Instance;
            var battleRoot = GameObject.Find("BattleRoot");
            var playerEntity = playerSystem != null ? "?" : "null";
            var currentWeapon = weaponSystem?.CurrentWeapon != null ? $"{weaponSystem.CurrentWeapon.Config?.id}" : "null";

            Debug.LogWarning($"[FocusDiag:{tag}] " +
                $"focused={Application.isFocused} " +
                $"isPaused={Time.timeScale <= Mathf.Epsilon} " +
                $"timeScale={Time.timeScale} " +
                $"PlayerSystem={(playerSystem != null)} " +
                $"WeaponSystem={(weaponSystem != null)} " +
                $"BattleRoot={(battleRoot != null)} " +
                $"CurrentWeapon={currentWeapon}");
        }
    }
}
