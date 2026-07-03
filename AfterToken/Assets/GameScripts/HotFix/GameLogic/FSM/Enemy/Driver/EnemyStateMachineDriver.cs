using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人状态机统一驱动器。
    /// </summary>
    public class EnemyStateMachineDriver
    {
        private static EnemyStateMachineDriver _instance;
        public static EnemyStateMachineDriver Instance => _instance ??= new();

        private readonly List<EnemyStateInterceptor> _interceptors = new();

        public EnemyStateMachineDriver()
        {
            RegisterInterceptor(new EnemyDeathInterceptor());
            RegisterInterceptor(new EnemyAttackInterceptor());
            RegisterInterceptor(new EnemyChaseInterceptor());
        }

        public void RegisterInterceptor(EnemyStateInterceptor interceptor)
        {
            if (interceptor == null) return;
            _interceptors.Add(interceptor);
            _interceptors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public bool TryGetInterruptRequest(EnemyStateContext context, Type currentStateType, out StateTransitionRequest request)
        {
            request = null;
            if (context == null || currentStateType == null) return false;
            foreach (var interceptor in _interceptors)
            {
                if (interceptor == null) continue;
                if (interceptor.TryIntercept(context, currentStateType, out request))
                {
                    if (request?.TargetStateType == currentStateType)
                    {
                        request = null;
                        continue;
                    }
                    return true;
                }
            }
            return false;
        }

        public void UpdateContext(EnemyStateContext context, EnemyEntity owner)
        {
            if (context == null || owner == null) return;
            context.ResetIntent();

            if (context.IsDead)
            {
                context.WantsToChase = false;
                context.WantsToAttack = false;
                return;
            }

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null)
            {
                context.WantsToChase = false;
                context.WantsToAttack = false;
                return;
            }

            Vector2 ownerPos = owner.transform.position;
            Vector2 playerPos = player.transform.position;
            context.PlayerPosition = playerPos;
            float distance = Vector2.Distance(ownerPos, playerPos);

            // 攻击范围优先于追击范围；具体数值后续接入 TbEnemy 配置
            float attackRange = 1.2f;
            float chaseRange = 8f;

            context.WantsToAttack = distance <= attackRange;
            context.WantsToChase = !context.WantsToAttack && distance <= chaseRange;
        }
    }
}
