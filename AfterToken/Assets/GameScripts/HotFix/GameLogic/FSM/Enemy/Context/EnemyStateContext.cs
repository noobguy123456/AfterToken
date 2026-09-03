using UnityEngine;
using GameLogic.Navigation;

namespace GameLogic
{
    /// <summary>
    /// 敌人状态黑板。
    /// </summary>
    public class EnemyStateContext
    {
        public bool IsDead;
        public bool WantsToChase;
        public bool WantsToAttack;
        // 玩家是否超出追踪距离（仅 Chase 状态的丢失判定用，与 WantsToChase 的检测语义形成滞回）
        public bool PlayerOutOfPursuit;
        public Vector2 PlayerPosition;
        public INavigationSystem NavigationSystem;
        public StateTransitionRequest PendingRequest;

        public void ResetIntent()
        {
            WantsToChase = false;
            WantsToAttack = false;
        }
    }
}
