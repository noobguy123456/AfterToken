using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友交战状态（护主）：优先攻击 Attack 标点目标，其次离队友最近的激活威胁；
    /// 与玩家保持 ≤8m 护航距离，超出先收拢。真实开火走 IWeaponEvent.OnFire 弹道链路。
    /// </summary>
    public class CompanionEngageState : CompanionStateBase
    {
        public override string StateName => "Engage";

        /// <summary>护航半径：与玩家距离超过该值优先收拢。</summary>
        private const float LEASH_DISTANCE = 8f;
        /// <summary>收拢到位距离。</summary>
        private const float LEASH_STOP_DISTANCE = 3f;
        /// <summary>
        /// 最小交战距离兜底值（TbCompanion.engageMinDist 缺失时用）：贴脸时 SphereCast 起点
        /// （枪口在中心前方）已钻进敌人碰撞体导致命中丢失，且步枪贴脸也不合理——小于该距离边退边打。
        /// </summary>
        private const float MIN_ENGAGE_DISTANCE_FALLBACK = 1.5f;

        private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle");

        private readonly CompanionPathMover _mover = new CompanionPathMover();
        private float _fireCooldown;
        private float _minEngageDistance = MIN_ENGAGE_DISTANCE_FALLBACK;
        private WeaponConfig _weapon;

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            _mover.Reset();
            _fireCooldown = 0.2f; // 进战斗小幅延迟，避免同帧瞬发
            var persona = CompanionConfigMgr.Instance.Get();
            if (persona != null && persona.EngageMinDist > 0.01f)
            {
                _minEngageDistance = persona.EngageMinDist;
            }
            _weapon = WeaponConfigMgr.Instance?.Get(Owner.WeaponConfigId);
            if (_weapon == null)
            {
                Log.Warning($"[CompanionEngage] 找不到队友武器配置 {Owner.WeaponConfigId}，队友将不开火");
            }
        }

        protected override void OnUpdateState(IFsm<CompanionEntity> fsm, float elapse, float real)
        {
            _fireCooldown -= elapse;

            // 目标选择：Attack 标点 > LLM 指定 > 最近威胁 > 警戒半径内最近可见敌人（先发制人）
            var target = Context.PingTarget != null && !Context.PingTarget.IsDead
                ? Context.PingTarget
                : (Context.LlmTarget != null && !Context.LlmTarget.IsDead
                    ? Context.LlmTarget
                    : (Context.NearestThreat != null ? Context.NearestThreat : Context.NearestVisibleEnemy));

            Vector2 ownerPos = Owner.transform.position.ToXZ();

            // 护航收拢：离玩家太远先回防
            if (Context.PlayerExists)
            {
                float distToPlayer = Vector2.Distance(ownerPos, Context.PlayerPosition);
                if (distToPlayer > LEASH_DISTANCE)
                {
                    _mover.MoveTowards(Owner, Context.NavigationSystem, Context.PlayerPosition, elapse, LEASH_STOP_DISTANCE);
                    // 收拢途中仍可开火
                    TryFire(target, ownerPos);
                    return;
                }
            }

            if (target == null)
            {
                StopMoving();
                return;
            }

            Vector2 targetPos = target.transform.position.ToXZ();
            float weaponRange = _weapon != null && _weapon.maxRange > 0f ? _weapon.maxRange : 20f;
            float distToTarget = Vector2.Distance(ownerPos, targetPos);
            bool hasLoS = !Physics.Linecast(ownerPos.ToWorld(0.5f), targetPos.ToWorld(0.5f), ObstacleMask);

            // 超出射程或视线被挡：寻路逼近（在射程内但隔墙时必须继续绕，否则原地死锁）。
            // 注意到位阈值必须远小于射程：逼近的终止条件是"有视线且在射程内"（由外层分支每帧判定），
            // 把大阈值传给 mover 会让它立刻判定到位而原地停死。
            if (distToTarget > weaponRange * 0.9f || !hasLoS)
            {
                _mover.MoveTowards(Owner, Context.NavigationSystem, targetPos, elapse, 0.8f);
            }
            else if (distToTarget < _minEngageDistance)
            {
                // 贴脸：边退边打（ SphereCast 起点在枪口，贴脸时已钻进敌人碰撞体导致命中丢失 ）
                Vector2 away = ownerPos - targetPos;
                if (away.sqrMagnitude > 1e-6f && Owner.Rigidbody != null)
                {
                    Owner.Rigidbody.linearVelocity = new Vector3(away.normalized.x, 0f, away.normalized.y) * Owner.MoveSpeed;
                }
                Owner.SetFacing(targetPos - ownerPos);
            }
            else
            {
                StopMoving();
                Owner.SetFacing(targetPos - ownerPos);
            }

            TryFire(target, ownerPos);
        }

        protected override void OnLeaveState(IFsm<CompanionEntity> fsm, bool isShutdown)
        {
            _mover.Stop(Owner);
        }

        private void TryFire(EnemyEntity target, Vector2 ownerPos)
        {
            if (target == null || _weapon == null || _fireCooldown > 0f) return;

            Vector2 targetPos = target.transform.position.ToXZ();
            Vector2 origin = Owner.GetMuzzleWorldPos().ToXZ();
            Vector2 direction = targetPos - origin;
            if (direction.sqrMagnitude < 1e-6f) return;

            float weaponRange = _weapon.maxRange > 0f ? _weapon.maxRange : 20f;
            if (direction.magnitude > weaponRange) return;

            // 视线遮挡不开火（与敌人 Linecast 语义一致）
            if (Physics.Linecast(ownerPos.ToWorld(0.5f), targetPos.ToWorld(0.5f), ObstacleMask)) return;

            direction.Normalize();
            Owner.SetFacing(direction);
            _fireCooldown = 1f / Mathf.Max(0.1f, _weapon.fireRate);
            GameEvent.Get<IWeaponEvent>()?.OnFire(origin, direction, _weapon.id, Owner.OwnerId);
        }
    }
}
