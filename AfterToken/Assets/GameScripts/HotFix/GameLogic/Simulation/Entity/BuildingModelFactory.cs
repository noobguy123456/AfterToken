using EBuildingType = GameConfig.cfg.EBuildingType;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 建筑占位模型工厂：正式模型资源到位前，用代码拼装的方块组合区分建筑类型。
    /// 尺寸由配置表 footprintX/Z（1m 基础格格数）决定，摆放预览与场景实体共用。
    /// </summary>
    public static class BuildingModelFactory
    {
        /// <summary>
        /// 按建筑类型创建占位模型（根节点位于建筑中心、底部贴地 y=0）。
        /// </summary>
        public static GameObject CreatePlaceholder(EBuildingType type, int footprintX, int footprintZ)
        {
            var root = new GameObject($"Placeholder_{type}");
            float sizeX = footprintX * MapGrid.BaseCellSize - 0.2f; // 留出 0.2m 缝，格界可见
            float sizeZ = footprintZ * MapGrid.BaseCellSize - 0.2f;

            switch (type)
            {
                case EBuildingType.Workshop:
                    // 工坊：灰棕主体厂房 + 深灰烟囱
                    AddCube(root, "Body", new Vector3(0f, 1.4f, 0f), new Vector3(sizeX, 2.8f, sizeZ), new Color(0.55f, 0.45f, 0.35f));
                    AddCube(root, "Chimney", new Vector3(sizeX * 0.25f, 3.4f, -sizeZ * 0.25f), new Vector3(0.8f, 1.6f, 0.8f), new Color(0.3f, 0.3f, 0.3f));
                    break;
                case EBuildingType.Farm:
                    // 农场：黄褐矮田块 + 四块绿色作物垄
                    AddCube(root, "Field", new Vector3(0f, 0.25f, 0f), new Vector3(sizeX, 0.5f, sizeZ), new Color(0.65f, 0.5f, 0.25f));
                    for (int i = 0; i < 4; i++)
                    {
                        float px = (i % 2 == 0 ? -1f : 1f) * sizeX * 0.25f;
                        float pz = (i < 2 ? -1f : 1f) * sizeZ * 0.25f;
                        AddCube(root, $"Crop{i}", new Vector3(px, 0.75f, pz), new Vector3(sizeX * 0.3f, 0.5f, sizeZ * 0.3f), new Color(0.3f, 0.7f, 0.3f));
                    }
                    break;
                case EBuildingType.Trade:
                    // 贸易站：蓝色主体 + 深蓝大平顶（雨棚感）
                    AddCube(root, "Body", new Vector3(0f, 1f, 0f), new Vector3(sizeX, 2f, sizeZ), new Color(0.3f, 0.45f, 0.7f));
                    AddCube(root, "Roof", new Vector3(0f, 2.15f, 0f), new Vector3(sizeX + 0.4f, 0.3f, sizeZ + 0.4f), new Color(0.2f, 0.3f, 0.55f));
                    break;
                case EBuildingType.Decor:
                    // 装饰：白色底座 + 金色立柱
                    AddCube(root, "Base", new Vector3(0f, 0.2f, 0f), new Vector3(sizeX * 0.8f, 0.4f, sizeZ * 0.8f), new Color(0.85f, 0.85f, 0.85f));
                    AddCube(root, "Pillar", new Vector3(0f, 1.2f, 0f), new Vector3(sizeX * 0.35f, 1.6f, sizeZ * 0.35f), new Color(0.85f, 0.7f, 0.2f));
                    break;
                default:
                    // 未知类型：通用灰色方块（按占地缩放）
                    AddCube(root, "Body", new Vector3(0f, 1f, 0f), new Vector3(sizeX, 2f, sizeZ), new Color(0.6f, 0.6f, 0.6f));
                    break;
            }
            return root;
        }

        /// <summary>
        /// 占位模型的建议标签高度（略高于模型顶部）。
        /// </summary>
        public static float GetLabelHeight(EBuildingType type)
        {
            return type switch
            {
                EBuildingType.Workshop => 4.8f,
                EBuildingType.Farm => 1.6f,
                EBuildingType.Trade => 3.0f,
                EBuildingType.Decor => 2.4f,
                _ => 2.5f,
            };
        }

        private static void AddCube(GameObject root, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            // 占位模型不需要参与物理（占地由网格占用表管理）
            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }
    }
}
