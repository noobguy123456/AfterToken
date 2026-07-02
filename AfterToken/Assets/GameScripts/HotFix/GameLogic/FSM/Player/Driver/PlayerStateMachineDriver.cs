using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态机统一驱动器。
    /// 负责维护拦截器列表，并供 PlayerStateBase 查询全局打断条件。
    /// 注意：TEngine FsmModule 会自动轮询 FSM，本类不驱动 FSM Update。
    /// </summary>
    public class PlayerStateMachineDriver
    {
        private static PlayerStateMachineDriver _instance;
        public static PlayerStateMachineDriver Instance => _instance ??= new PlayerStateMachineDriver();

        private readonly List<PlayerStateInterceptor> _interceptors = new List<PlayerStateInterceptor>();

        public PlayerStateMachineDriver()
        {
            RegisterInterceptor(new DeathInterceptor());
            RegisterInterceptor(new DodgeInterceptor());
            RegisterInterceptor(new ReloadStartInterceptor());
        }

        /// <summary>
        /// 注册拦截器。
        /// </summary>
        public void RegisterInterceptor(PlayerStateInterceptor interceptor)
        {
            if (interceptor == null) return;
            _interceptors.Add(interceptor);
            _interceptors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// 由 PlayerStateBase.OnUpdate 调用，尝试获取全局拦截器产生的切换请求。
        /// </summary>
        public bool TryGetInterruptRequest(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
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

        /// <summary>
        /// 每帧由 PlayerSystem 调用，更新黑板意图。
        /// </summary>
        public void UpdateContext(PlayerStateContext context)
        {
            if (context == null) return;

            context.ResetIntent();

            context.WantsToDodge = context.DodgePressed;
            context.WantsToReload = context.ReloadPressed &&
                                    context.CurrentWeapon != null &&
                                    context.CurrentWeapon.CurrentAmmo < context.CurrentWeapon.Config.clipSize &&
                                    !context.CurrentWeapon.IsReloading;
            context.WantsToFire = context.FirePressed;

            // 消费一次性输入标志，避免重复触发
            context.DodgePressed = false;
            context.ReloadPressed = false;
            context.FirePressed = false;
        }
    }
}
