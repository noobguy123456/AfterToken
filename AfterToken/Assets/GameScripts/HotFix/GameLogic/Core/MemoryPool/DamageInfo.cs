using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 伤害信息。
    /// </summary>
    public class DamageInfo : IMemory
    {
        public int AttackerId;
        public int TargetId;
        public GameObject TargetGameObject;
        public float Damage;
        public bool IsCritical;
        public int WeaponConfigId;
        public int BulletConfigId;
        // 受击方向，玩法平面 (x, z) 上的归一化向量
        public Vector2 HitDirection;
        // 命中点，玩法平面坐标 (x, z)
        public Vector2 HitPoint;

        public void Clear()
        {
            AttackerId = 0;
            TargetId = 0;
            TargetGameObject = null;
            Damage = 0;
            IsCritical = false;
            WeaponConfigId = 0;
            BulletConfigId = 0;
            HitDirection = Vector2.zero;
            HitPoint = Vector2.zero;
        }
    }
}
