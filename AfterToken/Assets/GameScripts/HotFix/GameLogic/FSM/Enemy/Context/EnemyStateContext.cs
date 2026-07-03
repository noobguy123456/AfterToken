using UnityEngine;

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
        public Vector2 PlayerPosition;
        public StateTransitionRequest PendingRequest;

        public void ResetIntent()
        {
            WantsToChase = false;
            WantsToAttack = false;
        }
    }
}
