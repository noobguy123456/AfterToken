using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 动态导航障碍：启用/禁用时用自身 Collider 的 bounds（外扩代理半径）
    /// 触发导航网格局部更新。仅用于战斗中运行时生成/销毁的障碍；
    /// 场景烘焙前已存在的静态障碍无需挂载（网格烘焙时已包含）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NavObstacle : MonoBehaviour
    {
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            UpdateNavRegion();
        }

        private void OnDisable()
        {
            // 防御：非 Play / 导航未初始化 / 场景切换销毁时静默跳过
            if (!Application.isPlaying) return;
            if (_collider == null) return;
            var nav = NavigationSystem.Instance;
            if (nav == null) return;

            // 项目关闭了 Physics.autoSyncTransforms，运行时移动后立刻读 bounds 会拿到旧值，先手动同步
            Physics.SyncTransforms();

            // Bounds.Expand 的参数为总增量，各侧实际外扩一半，故传入 2 倍半径
            Bounds bounds = _collider.bounds;
            bounds.Expand(ColliderGridBuilder.AgentRadius * 2f);

            // OnDisable 阶段自身 Collider 仍在物理场景中，立即重扫会把格子误判为阻挡；
            // 延迟一帧，待销毁/停用的 Collider 从物理场景移除后再更新。
            // 协程挂在 NavigationSystem 上，自身销毁后仍能执行。
            try
            {
                nav.StartCoroutine(DelayedUpdateRegion(nav, bounds));
            }
            catch (System.Exception)
            {
                // 场景切换销毁时 NavigationSystem 可能已在回收，静默忽略
            }
        }

        private static System.Collections.IEnumerator DelayedUpdateRegion(NavigationSystem nav, Bounds bounds)
        {
            yield return null;
            if (nav != null)
            {
                nav.UpdateRegion(bounds);
            }
        }

        private void UpdateNavRegion()
        {
            // 防御：非 Play / 导航未初始化 / 场景切换销毁时静默跳过
            if (!Application.isPlaying) return;
            if (_collider == null) return;
            var nav = NavigationSystem.Instance;
            if (nav == null) return;

            // 项目关闭了 Physics.autoSyncTransforms，运行时生成后立刻读 bounds 会拿到移动前的旧值，
            // 先手动同步物理变换保证 bounds 与 CheckSphere 结果准确（障碍生成是低频操作，开销可接受）
            Physics.SyncTransforms();

            // Bounds.Expand 的参数为总增量，各侧实际外扩一半，故传入 2 倍半径
            Bounds bounds = _collider.bounds;
            bounds.Expand(ColliderGridBuilder.AgentRadius * 2f);
            nav.UpdateRegion(bounds);
        }
    }
}
