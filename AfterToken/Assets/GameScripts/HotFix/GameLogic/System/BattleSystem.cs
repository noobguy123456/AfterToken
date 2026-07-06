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

            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 worldPos = damageInfo.HitPoint;
            if (worldPos == Vector3.zero && damageInfo.TargetGameObject != null)
            {
                worldPos = damageInfo.TargetGameObject.transform.position;
            }

            var screenPos = mainCamera.WorldToScreenPoint(worldPos);
            DamageNumberUI.Show((int)damageInfo.Damage, screenPos, false);
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
                        damageable.TakeDamage((int)damageInfo.Damage, damageInfo.HitDirection);

                        // 命中反馈：在目标位置显示受击标记 + 伤害飘字
                        var mainCamera = Camera.main;
                        if (mainCamera != null)
                        {
                            var screenPos = mainCamera.WorldToScreenPoint(damageInfo.TargetGameObject.transform.position);
                            GameEvent.Get<IHitFeedbackEvent>()?.OnHitTarget(false, screenPos);
                        }
                        ShowDamageNumber(damageInfo);
                    }
                    else
                    {
                        Log.Debug($"[BattleSystem] 命中非可受伤目标: {damageInfo.TargetGameObject.name}");
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
