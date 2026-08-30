using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器挂载视图（占位 3D 模型）。
    /// 挂在玩家身上，在角色右侧生成一个" WeaponMount "挂点，
    /// 按当前槽位武器类型显示不同尺寸/颜色的长方体，切换武器时联动显隐。
    /// 仅做表现：不参与弹道/碰撞（生成时已移除 Collider）。
    /// 无 WeaponSystem 的场景（如纯演出场景）保持隐藏。
    /// </summary>
    public class WeaponMountView : MonoBehaviour
    {
        /// <summary>挂点相对玩家的本地偏移（右侧、腰部偏上、略微靠前）。</summary>
        private static readonly Vector3 MountLocalPos = new Vector3(0.38f, 0.7f, 0.1f);

        private readonly Dictionary<int, GameObject> _slotModels = new Dictionary<int, GameObject>();
        // 各槽位模型的外形尺寸（EnsureModel 时记录），用于推算枪口位置
        private readonly Dictionary<int, Vector3> _slotSizes = new Dictionary<int, Vector3>();
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private Transform _mount;
        private int _ownerId;

        /// <summary>
        /// 当前槽位武器的枪口世界坐标（模型前端）。无模型时退化为挂点位置。
        /// </summary>
        public Vector3 GetMuzzleWorldPos()
        {
            int slot = WeaponSystem.Instance != null ? WeaponSystem.Instance.CurrentSlotIndex : -1;
            if (slot >= 0
                && _slotModels.TryGetValue(slot, out var model) && model != null && model.activeSelf
                && _slotSizes.TryGetValue(slot, out var size))
            {
                // 模型长轴沿 Z，枪口 = 模型中心再向前半长
                return model.transform.position + model.transform.forward * (size.z * 0.5f);
            }
            return _mount != null ? _mount.position : transform.position;
        }

        private void Awake()
        {
            // ownerId 必须与 IWeaponOwner.OwnerId 同源（PlayerEntity 的 InstanceID），
            // 不能用本组件的 GetInstanceID()——同一 GameObject 上不同组件 ID 不同，事件会被过滤掉
            var player = GetComponent<PlayerEntity>();
            _ownerId = player != null ? ((IWeaponOwner)player).OwnerId : GetInstanceID();

            var mountGo = new GameObject("WeaponMount");
            mountGo.transform.SetParent(transform, false);
            mountGo.transform.localPosition = MountLocalPos;
            _mount = mountGo.transform;

            _eventMgr.AddEvent<int, int, int>(IWeaponEvent_Event.OnWeaponEquipped, OnWeaponEquipped);
            _eventMgr.AddEvent<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
        }

        private void Start()
        {
            // WeaponSystem 装备广播早于玩家创建（ownerId 还是 0），事件必然错过，
            // 这里直接读槽位现状补齐模型。
            SyncFromWeaponSystem();
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
        }

        /// <summary>
        /// 从 WeaponSystem 当前槽位状态同步模型（处理错过的事件）。
        /// </summary>
        private void SyncFromWeaponSystem()
        {
            var ws = WeaponSystem.Instance;
            if (ws == null || ws.Slots == null) return;

            for (int i = 0; i < ws.Slots.Length; i++)
            {
                var weapon = ws.Slots[i];
                if (weapon?.Config != null)
                {
                    EnsureModel(i, weapon.Config.weaponType);
                }
            }
            ShowSlot(ws.CurrentSlotIndex);
        }

        private void OnWeaponEquipped(int ownerId, int slot, int weaponConfigId)
        {
            if (ownerId != _ownerId) return;

            var cfg = WeaponConfigMgr.Instance?.Get(weaponConfigId);
            if (cfg == null) return;

            // 换装：重建该槽位模型
            if (_slotModels.TryGetValue(slot, out var old) && old != null)
            {
                Destroy(old);
                _slotModels.Remove(slot);
                _slotSizes.Remove(slot);
            }
            EnsureModel(slot, cfg.weaponType);

            if (WeaponSystem.Instance != null && WeaponSystem.Instance.CurrentSlotIndex == slot)
            {
                ShowSlot(slot);
            }
        }

        private void OnWeaponSwitched(int ownerId, int slot)
        {
            if (ownerId != _ownerId) return;
            ShowSlot(slot);
        }

        /// <summary>
        /// 只显示指定槽位的武器模型，其余隐藏。
        /// </summary>
        private void ShowSlot(int slot)
        {
            foreach (var pair in _slotModels)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(pair.Key == slot);
                }
            }
        }

        private void EnsureModel(int slot, WeaponType type)
        {
            if (_slotModels.ContainsKey(slot)) return;

            GetShape(type, out Vector3 size, out Color color);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"WeaponModel_Slot{slot}_{type}";
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col); // 占位模型不参与碰撞/弹道
            }
            go.layer = gameObject.layer;
            go.transform.SetParent(_mount, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            // 长轴沿 Z（玩家面朝方向），枪口朝前：中心前移半长，让挂点位于握把处
            go.transform.localPosition = new Vector3(0f, 0f, size.z * 0.4f);
            go.GetComponent<MeshRenderer>().material.color = color;

            _slotModels[slot] = go;
            _slotSizes[slot] = size;
            go.SetActive(false);
        }

        /// <summary>
        /// 各武器类型的占位外形（长盒尺寸 + 颜色）。正式模型接入后整体替换本方法。
        /// </summary>
        private static void GetShape(WeaponType type, out Vector3 size, out Color color)
        {
            switch (type)
            {
                case WeaponType.Pistol:
                    size = new Vector3(0.10f, 0.16f, 0.28f);
                    color = new Color(0.30f, 0.30f, 0.32f);
                    break;
                case WeaponType.SMG:
                    size = new Vector3(0.12f, 0.20f, 0.45f);
                    color = new Color(0.20f, 0.28f, 0.45f);
                    break;
                case WeaponType.Rifle:
                    size = new Vector3(0.11f, 0.20f, 0.70f);
                    color = new Color(0.25f, 0.40f, 0.25f);
                    break;
                case WeaponType.Sniper:
                    size = new Vector3(0.09f, 0.15f, 1.00f);
                    color = new Color(0.12f, 0.12f, 0.14f);
                    break;
                case WeaponType.Rocket:
                    size = new Vector3(0.22f, 0.22f, 0.85f);
                    color = new Color(0.85f, 0.45f, 0.10f);
                    break;
                default:
                    size = new Vector3(0.14f, 0.18f, 0.40f);
                    color = Color.gray;
                    break;
            }
        }
    }
}
