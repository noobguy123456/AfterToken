using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家死亡处理。
    /// 监听死亡事件，弹出确认提示，确认后返回关卡选择（ProcedureLobby）。
    /// </summary>
    public class PlayerDeathHandler : MonoBehaviour
    {
        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private bool _isDead;
        private bool _confirmed;

        private void Awake()
        {
            _eventMgr.AddEvent(IPlayerEvent_Event.OnPlayerDied, OnPlayerDied);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
        }

        private void OnPlayerDied()
        {
            if (_isDead) return;
            _isDead = true;

            GamePauseManager.PushTimeScale(0f);
            CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
            CursorManager.Instance?.ForceShowCursor();
        }

        private void OnGUI()
        {
            if (!_isDead || _confirmed) return;

            const int w = 840;
            const int h = 540;
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            var titleStyle = new GUIStyle(GUI.skin.box);
            titleStyle.fontSize = 48;
            titleStyle.alignment = TextAnchor.UpperCenter;

            var labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 36;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.wordWrap = true;

            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 36;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Box(rect, "玩家死亡", titleStyle);
            GUI.Label(new Rect(rect.x + 60, rect.y + 120, w - 120, 100), "你已被击败，请选择下一步操作", labelStyle);

            if (GUI.Button(new Rect(rect.x + 220, rect.y + 260, 400, 100), "重新开始", buttonStyle))
            {
                RestartBattle();
            }

            if (GUI.Button(new Rect(rect.x + 220, rect.y + 380, 400, 100), "返回关卡选择", buttonStyle))
            {
                ConfirmReturnToLobby();
            }
        }

        /// <summary>
        /// 重新开始当前关卡。
        /// </summary>
        public void RestartBattle()
        {
            if (_confirmed) return;
            _confirmed = true;
            GamePauseManager.PopTimeScale();
            GameApp.ChangeProcedure<ProcedureBattle>();
        }

        /// <summary>
        /// 确认返回关卡选择。
        /// </summary>
        public void ConfirmReturnToLobby()
        {
            if (_confirmed) return;
            _confirmed = true;
            GamePauseManager.PopTimeScale();
            GameApp.ChangeProcedure<ProcedureLobby>();
        }
    }
}
