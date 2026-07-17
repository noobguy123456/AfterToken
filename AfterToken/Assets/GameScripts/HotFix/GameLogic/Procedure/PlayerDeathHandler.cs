using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家死亡处理：监听死亡事件并弹出死亡确认 UI。
    /// </summary>
    public class PlayerDeathHandler : MonoBehaviour
    {
        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private bool _isDead;
        private bool _confirmed;

        /// <summary>
        /// 玩家点击“重新开始”时触发。
        /// </summary>
        public event System.Action OnRestartRequested;

        /// <summary>
        /// 玩家点击“返回关卡选择”时触发。
        /// </summary>
        public event System.Action OnReturnToLobbyRequested;

        private void Awake()
        {
            _eventMgr.AddEvent(IPlayerEvent_Event.OnPlayerDied, OnPlayerDied);
        }

        private void OnDestroy() => _eventMgr.Clear();

        private void OnPlayerDied()
        {
            if (_isDead) return;
            _isDead = true;
            GamePauseManager.PushTimeScale(0f);
            CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
            CursorManager.Instance?.ForceShowCursor();
            GameModule.UI.ShowUIAsync<PlayerDeathUI>(this);
        }

        /// <summary>
        /// 由 PlayerDeathUI 调用：确认重新开始。
        /// </summary>
        public void ConfirmRestart()
        {
            if (_confirmed) return;
            _confirmed = true;
            OnRestartRequested?.Invoke();
            RestartBattle();
        }

        /// <summary>
        /// 由 PlayerDeathUI 调用：确认返回关卡选择。
        /// </summary>
        public void ConfirmReturnToLobby()
        {
            if (_confirmed) return;
            _confirmed = true;
            OnReturnToLobbyRequested?.Invoke();
            ReturnToLobby();
        }

        private void RestartBattle()
        {
            GamePauseManager.PopTimeScale();
            GameApp.ChangeProcedure<ProcedureBattle>();
        }

        private void ReturnToLobby()
        {
            GamePauseManager.PopTimeScale();
            GameApp.ChangeProcedure<ProcedureLobby>();
        }
    }
}
