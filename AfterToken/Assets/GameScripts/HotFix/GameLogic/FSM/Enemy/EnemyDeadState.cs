using TEngine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人死亡状态。
    /// </summary>
    public class EnemyDeadState : EnemyStateBase
    {
        public override string StateName => "Dead";

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            Context.IsDead = true;
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector2.zero;
                Owner.Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            // 禁用碰撞体，避免死亡后继续被命中/造成伤害
            var colliders = Owner.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // TODO: 播放死亡动画、触发掉落
            DespawnAsync().Forget();;
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            // 死亡状态常驻，等待回收
        }

        private async UniTask DespawnAsync()
        {
            await UniTask.Delay(1000, cancellationToken: Owner.GetCancellationTokenOnDestroy());
            if (Owner != null)
            {
                GameEvent.Get<IEnemyEvent>().OnEnemyDied(Owner.GetInstanceID());
                Object.Destroy(Owner.gameObject);
            }
        }
    }
}
