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
                    var enemy = damageInfo.TargetGameObject.GetComponent<EnemyEntity>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage((int)damageInfo.Damage, damageInfo.HitDirection);

                        // 命中反馈：准心闪烁 + 伤害飘字
                        GameEvent.Get<IHitFeedbackEvent>()?.OnHitTarget(false);
                        ShowDamageNumber(damageInfo);
                    }
                    else
                    {
                        // 简化：如果目标是玩家自己，处理自伤（避免友军伤害可扩展）
                        var playerSystem = PlayerSystem.Instance;
                        if (playerSystem != null && damageInfo.TargetGameObject == playerSystem.GetPlayerEntity()?.gameObject)
                        {
                            playerSystem.TakeDamage((int)damageInfo.Damage, damageInfo.HitDirection);
                        }
                        else
                        {
                            Log.Debug($"[BattleSystem] 命中非敌人目标: {damageInfo.TargetGameObject.name}");
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
