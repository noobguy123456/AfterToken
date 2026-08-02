using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 全局地图网格：全项目唯一的网格尺寸来源，所有场景严格统一。
    /// 基础格 1m——建筑占地、地块、关卡设计、地面纹理全部按它对齐；
    /// 寻路子格 0.5m = 基础格 / 2（A* 保持既有精度，4 个子格严丝合缝拼成 1 个基础格）。
    /// 约定：所有场景地面尺寸必须取基础格的整数倍，格中心位于 1m 的整数倍世界坐标。
    /// </summary>
    public static class MapGrid
    {
        /// <summary>基础格尺寸（米）：建筑/地块/关卡的统一网格单位。</summary>
        public const float BaseCellSize = 1f;

        /// <summary>寻路子格尺寸（米）：A* 寻路用，基础格的 1/2，原点对齐。</summary>
        public const float NavCellSize = BaseCellSize / 2f;

        /// <summary>
        /// 将世界坐标按建筑占地吸附到网格。
        /// 奇数格（如 1x1、3x3）：中心对齐格中心（1m 整数倍）；
        /// 偶数格（如 2x2、4x4）：中心对齐格缝（x.5 坐标），保证覆盖的格子不错位。
        /// </summary>
        public static Vector3 Snap(Vector3 position, int footprintX = 1, int footprintZ = 1)
        {
            return new Vector3(
                SnapAxis(position.x, footprintX),
                0f,
                SnapAxis(position.z, footprintZ));
        }

        private static float SnapAxis(float value, int cells)
        {
            float offset = cells % 2 == 0 ? 0.5f : 0f;
            return Mathf.Round((value - offset) / BaseCellSize) * BaseCellSize + offset;
        }

        /// <summary>
        /// 计算建筑（中心已吸附）的占地覆盖的所有格子坐标（格子坐标 = 格中心世界坐标 / 基础格尺寸）。
        /// </summary>
        public static List<Vector2Int> GetFootprintCells(Vector3 snappedCenter, int footprintX, int footprintZ)
        {
            var result = new List<Vector2Int>(footprintX * footprintZ);
            float startX = snappedCenter.x - (footprintX - 1) * BaseCellSize * 0.5f;
            float startZ = snappedCenter.z - (footprintZ - 1) * BaseCellSize * 0.5f;
            for (int x = 0; x < footprintX; x++)
            {
                for (int z = 0; z < footprintZ; z++)
                {
                    result.Add(new Vector2Int(
                        Mathf.RoundToInt((startX + x * BaseCellSize) / BaseCellSize),
                        Mathf.RoundToInt((startZ + z * BaseCellSize) / BaseCellSize)));
                }
            }
            return result;
        }

        /// <summary>格子坐标转世界坐标（格中心，y 由调用方决定）。</summary>
        public static Vector3 CellToWorld(Vector2Int cell, float y = 0f)
        {
            return new Vector3(cell.x * BaseCellSize, y, cell.y * BaseCellSize);
        }
    }
}
