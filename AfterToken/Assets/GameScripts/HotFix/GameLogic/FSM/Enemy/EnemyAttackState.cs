using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人攻击状态。
    /// 从敌人实体读取攻击伤害与攻击间隔，对玩家造成伤害。
    /// </summary>
    public class EnemyAttackState : EnemyStateBase
    {
        public override string StateName => "Attack";

        private float _elapsed;
        private bool _hasAttacked;

        private float AttackInterval => Owner?.AttackInterval > 0.01f ? Owner.AttackInterval : 0.5f;
        private int AttackDamage => Owner?.AttackDamage ?? 0;

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            _elapsed = 0f;
            _hasAttacked = false;
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector2.zero;
            }
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            _elapsed += elapse;

            if (!_hasAttacked && _elapsed >= AttackInterval * 0.5f)
            {
                _hasAttacked = true;
                ApplyDamageToPlayer();
            }

            if (_elapsed >= AttackInterval)
            {
                RequestState<EnemyIdleState>();
            }
        }

        private void ApplyDamageToPlayer()
        {
            if (AttackDamage <= 0) return;

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            var damageInfo = MemoryPool.Acquire<DamageInfo>();
            damageInfo.AttackerId = Owner.GetInstanceID();
            damageInfo.TargetGameObject = player.gameObject;
            damageInfo.Damage = AttackDamage;
            damageInfo.HitDirection = ((Vector2)player.transform.position - (Vector2)Owner.transform.position).normalized;
            damageInfo.HitPoint = player.transform.position;

            GameEvent.Get<IBattleEvent>().OnEntityDamaged(damageInfo);
        }
    }
}
