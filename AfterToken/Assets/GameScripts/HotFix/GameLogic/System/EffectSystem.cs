using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 特效系统：特效加载、播放、池化回收的统一入口。
    /// 玩法代码只发 IEffectEvent 或直调 Play，不碰 GameObject。
    /// 按 YooAsset 资源地址（文件名寻址）播放，池按地址隔离；
    /// 时长/池容量/挂载方式等元数据挂在 prefab 的 EffectDriver 序列化字段上，加载后读取缓存。
    /// 进战斗 Preload 预热注册列表（PreloadEffects），出战斗 ClearAll。
    /// </summary>
    public class EffectSystem : MonoBehaviour
    {
        public static EffectSystem Instance { get; private set; }

        /// <summary>
        /// 进战斗预热的特效地址注册列表（新增需预热的特效在此登记）。
        /// </summary>
        private static readonly string[] PreloadEffects =
        {
            EffectIds.Explosion,
            EffectIds.MuzzleFlash,
            EffectIds.HitSpark,
            EffectIds.HitSparkEnv,
            EffectIds.PickupGlow,
        };

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private readonly Dictionary<string, EffectPool> _pools = new Dictionary<string, EffectPool>();
        private readonly Stack<EffectInstance> _handlePool = new Stack<EffectInstance>();
        private Transform _root;

        /// <summary>
        /// 每特效地址一个 GameObject 池（含容量上限与最老强制回收）。
        /// 通用池 GameObjectPool 无容量/在播追踪语义，此处按特效场景自研。
        /// 元数据在 prefab 首次加载后从其 EffectDriver 序列化字段读取缓存。
        /// </summary>
        public class EffectPool
        {
            public string Address;
            public GameObject Prefab;
            public Transform Root;
            public bool Loading;
            public float Duration = 0.5f;
            public bool ScaleByRadius;
            public EffectDriver.AttachMode AttachMode = EffectDriver.AttachMode.World;
            public int Capacity = 8;
            public readonly Stack<GameObject> Idle = new Stack<GameObject>();
            /// <summary>在播实例，索引 0 为最老。</summary>
            public readonly List<EffectInstance> Active = new List<EffectInstance>();
        }

        private void Awake()
        {
            Instance = this;

            _root = new GameObject("Effects").transform;
            _root.SetParent(transform, false);

            _eventMgr.AddEvent<string, Vector3, Quaternion, EffectContext>(IEffectEvent_Event.OnPlayEffect, OnPlayEffectEvent);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
            ClearAllInternal();
        }

        #region 静态入口

        /// <summary>
        /// 一次性定点播放（枪口、爆炸、命中）。address 为 YooAsset 资源定位名，见 EffectIds。
        /// </summary>
        public static EffectInstance Play(string address, Vector3 pos, Quaternion rot)
        {
            return Play(address, pos, rot, EffectContext.Default);
        }

        /// <summary>
        /// 带参播放（爆炸半径等），参数进 EffectContext 由驱动解释。
        /// </summary>
        public static EffectInstance Play(string address, Vector3 pos, Quaternion rot, in EffectContext ctx)
        {
            return Instance != null ? Instance.PlayInternal(address, pos, rot, in ctx, null) : null;
        }

        /// <summary>
        /// 跟随播放（buff 光环、持续燃烧）。返回句柄可 Stop 提前结束。
        /// </summary>
        public static EffectInstance PlayAttached(string address, Transform target)
        {
            return PlayAttached(address, target, EffectContext.Default);
        }

        /// <summary>
        /// 带参跟随播放。
        /// </summary>
        public static EffectInstance PlayAttached(string address, Transform target, in EffectContext ctx)
        {
            if (Instance == null || target == null)
            {
                return null;
            }
            return Instance.PlayInternal(address, target.position, target.rotation, in ctx, target);
        }

        /// <summary>
        /// 预热 PreloadEffects 注册列表中标记了 preload 的特效（异步加载 + 分帧实例化一半容量）。
        /// 在进入战斗流程时调用。
        /// </summary>
        public static void Preload()
        {
            if (Instance != null)
            {
                Instance.PreloadAsync().Forget();
            }
        }

        /// <summary>
        /// 场景切换时全量回收，防跨场景泄漏。
        /// </summary>
        public static void ClearAll()
        {
            Instance?.ClearAllInternal();
        }

        /// <summary>
        /// 诊断：取特效池的 在播/闲置 计数。
        /// </summary>
        public static bool GetPoolStats(string address, out int active, out int idle)
        {
            active = 0;
            idle = 0;
            if (Instance != null && Instance._pools.TryGetValue(address, out var pool))
            {
                active = pool.Active.Count;
                idle = pool.Idle.Count;
                return true;
            }
            return false;
        }

        #endregion

        private void OnPlayEffectEvent(string address, Vector3 pos, Quaternion rot, EffectContext ctx)
        {
            PlayInternal(address, pos, rot, in ctx, null);
        }

        private EffectInstance PlayInternal(string address, Vector3 pos, Quaternion rot, in EffectContext ctx, Transform followTarget)
        {
            if (string.IsNullOrEmpty(address))
            {
                Log.Warning("[EffectSystem] 特效地址为空");
                return null;
            }

            var pool = GetOrCreatePool(address);
            if (pool.Prefab == null)
            {
                // 未预热的特效首次播放：触发懒加载，本次播放丢弃（加载完成后后续播放生效）。
                if (!pool.Loading)
                {
                    LoadPrefabAsync(pool).Forget();
                }
                Log.Warning($"[EffectSystem] 特效 {address} 尚未加载，本次播放丢弃（建议加入 PreloadEffects 预热）");
                return null;
            }

            // 池满强制回收最老实例（特效可丢弃，不可卡顿）。
            if (pool.Idle.Count == 0 && pool.Active.Count >= pool.Capacity)
            {
                RecycleInstance(pool.Active[0]);
            }

            GameObject go;
            if (pool.Idle.Count > 0)
            {
                go = pool.Idle.Pop();
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(pool.Prefab, pool.Root);
            }

            var tf = go.transform;
            tf.position = pos;
            tf.rotation = rot;

            var driver = go.GetComponent<EffectDriver>();
            if (driver != null)
            {
                // scaleByRadius=false 的特效不接收 Scale 参数，用默认缩放。
                var effCtx = pool.ScaleByRadius ? ctx : EffectContext.Default;
                driver.Init(in effCtx);
            }

            var inst = ObtainHandle();
            float endTime = pool.Duration > 0f ? Time.time + pool.Duration : float.MaxValue;
            Transform follow = pool.AttachMode == EffectDriver.AttachMode.Follow ? followTarget : null;
            inst.Setup(pool, go, driver, follow, endTime);
            pool.Active.Add(inst);
            return inst;
        }

        internal void StopInternal(EffectInstance inst)
        {
            if (inst.Pool == null)
            {
                return;
            }
            RecycleInstance(inst);
        }

        private void Update()
        {
            foreach (var pool in _pools.Values)
            {
                var active = pool.Active;
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    var inst = active[i];
                    if (inst.Stopped || inst.Go == null)
                    {
                        RecycleInstance(inst);
                        continue;
                    }

                    if (!ReferenceEquals(inst.FollowTarget, null))
                    {
                        // 跟随目标已销毁（Unity 假 null）：直接回收。
                        // 外层必须用 ReferenceEquals——销毁后的 Transform 对 != null 也返回 false，
                        // 用 != 会让已销毁目标永远进不了回收分支。
                        if (inst.FollowTarget == null)
                        {
                            RecycleInstance(inst);
                            continue;
                        }
                        var tf = inst.Go.transform;
                        tf.position = inst.FollowTarget.position;
                        tf.rotation = inst.FollowTarget.rotation;
                    }

                    if (Time.time >= inst.EndTime)
                    {
                        RecycleInstance(inst);
                    }
                }
            }
        }

        private void RecycleInstance(EffectInstance inst)
        {
            var pool = inst.Pool;
            var go = inst.Go;
            var driver = inst.Driver;

            int idx = pool.Active.IndexOf(inst);
            if (idx >= 0)
            {
                pool.Active.RemoveAt(idx);
            }

            if (driver != null)
            {
                driver.OnRecycle();
            }
            if (go != null)
            {
                go.SetActive(false);
                go.transform.SetParent(pool.Root, false);
                pool.Idle.Push(go);
            }
            ReleaseHandle(inst);
        }

        private EffectPool GetOrCreatePool(string address)
        {
            if (_pools.TryGetValue(address, out var pool))
            {
                return pool;
            }

            pool = new EffectPool { Address = address };
            var rootGo = new GameObject($"Effect_{address}");
            rootGo.transform.SetParent(_root, false);
            pool.Root = rootGo.transform;
            _pools[address] = pool;
            return pool;
        }

        private async UniTask LoadPrefabAsync(EffectPool pool)
        {
            pool.Loading = true;
            try
            {
                var ct = this.GetCancellationTokenOnDestroy();
                pool.Prefab = await GameModule.Resource.LoadAssetAsync<GameObject>(pool.Address, ct);
            }
            catch (Exception e)
            {
                Log.Error($"[EffectSystem] 特效资源加载异常: {pool.Address}, {e.GetType().Name}: {e.Message}");
            }
            finally
            {
                pool.Loading = false;
            }

            if (pool.Prefab == null)
            {
                Log.Warning($"[EffectSystem] 特效资源加载失败: {pool.Address}");
                return;
            }

            // 元数据从 prefab 资产的 EffectDriver 序列化字段读取（无需实例化）。
            var driver = pool.Prefab.GetComponent<EffectDriver>();
            if (driver != null)
            {
                pool.Duration = driver.Duration;
                pool.Capacity = Mathf.Max(1, driver.PoolCapacity);
                pool.AttachMode = driver.Mode;
                pool.ScaleByRadius = driver.ScaleByRadius;
            }
            else
            {
                Log.Warning($"[EffectSystem] 特效 {pool.Address} 未挂 EffectDriver，使用默认元数据");
            }
        }

        private async UniTaskVoid PreloadAsync()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            foreach (var address in PreloadEffects)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(address))
                {
                    continue;
                }

                var pool = GetOrCreatePool(address);
                if (pool.Prefab == null)
                {
                    await LoadPrefabAsync(pool);
                }
                if (pool.Prefab == null)
                {
                    continue;
                }

                // preload=false 的特效不预热实例（仅加载元数据）。
                var driver = pool.Prefab.GetComponent<EffectDriver>();
                if (driver != null && !driver.Preload)
                {
                    continue;
                }

                int count = Mathf.Max(1, pool.Capacity / 2);
                for (int i = 0; i < count && pool.Idle.Count + pool.Active.Count < pool.Capacity; i++)
                {
                    var go = Instantiate(pool.Prefab, pool.Root);
                    go.SetActive(false);
                    pool.Idle.Push(go);
                    // 分帧实例化，防预热尖刺。
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                Log.Info($"[EffectSystem] 预热特效 {address}，池容量 {pool.Capacity}，预热 {pool.Idle.Count}");
            }
        }

        private void ClearAllInternal()
        {
            foreach (var pool in _pools.Values)
            {
                for (int i = 0; i < pool.Active.Count; i++)
                {
                    var inst = pool.Active[i];
                    if (inst.Go != null)
                    {
                        Destroy(inst.Go);
                    }
                    ReleaseHandle(inst);
                }
                pool.Active.Clear();

                while (pool.Idle.Count > 0)
                {
                    var go = pool.Idle.Pop();
                    if (go != null)
                    {
                        Destroy(go);
                    }
                }

                if (pool.Root != null)
                {
                    Destroy(pool.Root.gameObject);
                }
                pool.Prefab = null;
            }
            _pools.Clear();
        }

        private EffectInstance ObtainHandle()
        {
            return _handlePool.Count > 0 ? _handlePool.Pop() : new EffectInstance();
        }

        private void ReleaseHandle(EffectInstance inst)
        {
            inst.Reset();
            _handlePool.Push(inst);
        }
    }
}
