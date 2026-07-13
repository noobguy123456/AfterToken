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

            const int w = 420;
            const int h = 220;
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            GUI.Box(rect, "玩家死亡");
            GUI.Label(new Rect(rect.x + 30, rect.y + 60, w - 60, 40), "你已被击败，是否返回关卡选择？");

            if (GUI.Button(new Rect(rect.x + 110, rect.y + 130, 200, 50), "确认"))
            {
                _confirmed = true;
                GamePauseManager.PopTimeScale();
                GameApp.ChangeProcedure<ProcedureLobby>();
            }
        }
    }
}
