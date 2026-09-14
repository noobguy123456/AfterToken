using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗系统。
    /// 负责伤害计算、死亡判定。
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private void Awake()
        {
            _eventMgr.AddEvent<DamageInfo>(IBattleEvent_Event.OnEntityDamaged, OnEntityDamaged);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
        }

        private void ShowDamageNumber(DamageInfo damageInfo)
        {
            if (damageInfo == null) return;

            var mainCamera = CameraSystem3D.Instance?.GetMainCamera();
            if (mainCamera == null) return;

            // 命中点为玩法平面坐标 (x, z)，转世界坐标时使用目标的实际高度
            float targetY = damageInfo.TargetGameObject != null ? damageInfo.TargetGameObject.transform.position.y : 0f;
            Vector3 worldPos = damageInfo.HitPoint.ToWorld(targetY);
            if (damageInfo.HitPoint == Vector2.zero && damageInfo.TargetGameObject != null)
            {
                worldPos = damageInfo.TargetGameObject.transform.position;
            }

            // 开镜狙击：伤害数字显示在狙击镜镜窗内（按镜相机取景变换换算），不走主相机屏幕坐标
            if (WeaponSystem.Instance != null && WeaponSystem.Instance.IsScopedSniping
                && SniperScopeUI.ShowScopeDamage((int)damageInfo.Damage, worldPos, false))
            {
                return;
            }

            var screenPos = mainCamera.WorldToScreenPoint(worldPos);
            DamageNumberUI.Show((int)damageInfo.Damage, screenPos, false);
        }

        /// <summary>
        /// 判断伤害来源是否为玩家（玩家武器开火传的 ownerId 即 PlayerEntity 的 InstanceID）。
        /// </summary>
        private static bool IsPlayerAttack(int attackerId)
        {
            var player = PlayerSystem.Instance != null ? PlayerSystem.Instance.GetPlayerEntity() : null;
            return player != null && attackerId == player.GetInstanceID();
        }

        /// <summary>
        /// 判断伤害来源是否为 AI 队友（队友武器开火传的 ownerId 即 CompanionEntity 的 OwnerId）。
        /// </summary>
        private static bool IsCompanionAttack(int attackerId)
        {
            var companion = CompanionSystem.Instance != null ? CompanionSystem.Instance.Companion : null;
            return companion != null && attackerId == companion.OwnerId;
        }

        private void OnEntityDamaged(DamageInfo damageInfo)
        {
            if (damageInfo == null) return;

            try
            {
                if (damageInfo.TargetGameObject != null)
                {
                    var damageable = damageInfo.TargetGameObject.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        bool tookDamage = damageable.TakeDamage((int)damageInfo.Damage, damageInfo.HitDirection);
                        // 命中标记 + 伤害飘字只对玩家造成的伤害生效（敌人打玩家时不应触发玩家侧的命中反馈）
                        if (tookDamage && IsPlayerAttack(damageInfo.AttackerId))
                        {
                            // 命中反馈：在目标位置显示受击标记 + 伤害飘字
                            var mainCamera = CameraSystem3D.Instance?.GetMainCamera();
                            if (mainCamera != null)
                            {
                                var screenPos = mainCamera.WorldToScreenPoint(damageInfo.TargetGameObject.transform.position);
                                GameEvent.Get<IHitFeedbackEvent>()?.OnHitTarget(damageInfo.IsCritical, screenPos);
                            }
                            ShowDamageNumber(damageInfo);
                        }

                        // 护驾好感：队友击杀正在追击玩家的敌人（数值/每局上限走 TbCompanionAffinityGain.protect）
                        if (tookDamage && damageable is EnemyEntity enemy && enemy.IsDead
                            && IsCompanionAttack(damageInfo.AttackerId)
                            && CompanionSystem.Instance != null
                            && CompanionSystem.Instance.Threats.Contains(enemy.GetInstanceID()))
                        {
                            CompanionAffinitySystem.AddFromSource("protect");
                        }
                    }
                }

                // 命中特效
                // GameEvent.Get<IEffectEvent>()?.OnEffectCreated(EffectType.HitSpark, ...);
            }
            finally
            {
                MemoryPool.Release(damageInfo);
            }
        }
    }
}
