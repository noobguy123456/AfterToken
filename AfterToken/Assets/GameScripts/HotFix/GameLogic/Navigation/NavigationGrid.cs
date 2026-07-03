using UnityEngine;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 二维导航网格数据。
    /// 只读数据结构，寻路算法不应修改其内容。
    /// </summary>
    public class NavigationGrid
    {
        /// <summary>
        /// 网格左下角世界坐标。
        /// </summary>
        public Vector2 Origin;

        /// <summary>
        /// 单个格子边长。
        /// </summary>
        public float CellSize;

        /// <summary>
        /// X 方向格子数。
        /// </summary>
        public int Width;

        /// <summary>
        /// Y 方向格子数。
        /// </summary>
        public int Height;

        /// <summary>
        /// 可行走标记数组，长度为 Width * Height。
        /// true 表示可行走。
        /// </summary>
        public bool[] Walkable;

        public int TotalCells => Width * Height;

        public int GetIndex(int x, int y)
        {
            return y * Width + x;
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsWalkable(int x, int y)
        {
            if (!IsInside(x, y)) return false;
            return Walkable[GetIndex(x, y)];
        }

        public bool IsWalkable(Vector2 worldPos)
        {
            if (!TryGetGridCoordinates(worldPos, out int x, out int y)) return false;
            return Walkable[GetIndex(x, y)];
        }

        public Vector2 GetWorldPosition(int x, int y)
        {
            return Origin + new Vector2((x + 0.5f) * CellSize, (y + 0.5f) * CellSize);
        }

        public bool TryGetGridCoordinates(Vector2 worldPos, out int x, out int y)
        {
            Vector2 local = worldPos - Origin;
            x = Mathf.FloorToInt(local.x / CellSize);
            y = Mathf.FloorToInt(local.y / CellSize);
            return IsInside(x, y);
        }
    }
}
