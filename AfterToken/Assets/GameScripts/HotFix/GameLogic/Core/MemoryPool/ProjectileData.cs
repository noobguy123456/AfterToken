using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 子弹运行时数据。
    /// </summary>
    public class ProjectileData : IMemory
    {
        public int Id;
        public int ConfigId;
        public int OwnerId;
        public int LayerMask;
        public Vector2 Position;
        public Vector2 Direction;
        public float Speed;
        public float LifeTime;
        public float Damage;
        public int PenetrateCount;
        public int BounceCount;
        public bool IsActive;
        public float Radius;
        public bool IsTracking;
        public int TargetId;

        public void Clear()
        {
            Id = 0;
            ConfigId = 0;
            OwnerId = 0;
            LayerMask = 0;
            Position = Vector2.zero;
            Direction = Vector2.zero;
            Speed = 0;
            LifeTime = 0;
            Damage = 0;
            PenetrateCount = 0;
            BounceCount = 0;
            IsActive = false;
            Radius = 0;
            IsTracking = false;
            TargetId = 0;
        }
    }
}
