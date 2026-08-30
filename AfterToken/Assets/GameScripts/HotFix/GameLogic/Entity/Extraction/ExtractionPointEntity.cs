using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 撤离点实体（搜打撤）：地图中的特殊点位，带一圈撤离范围。
    /// 玩家进入范围触发撤离倒计时（TbLevel.extractionTime，默认 10s）；
    /// 圈内有存活敌人时倒计时暂停，离开范围重置；倒计时归零且玩家存活则撤离回经营场景。
    /// 倒计时逻辑在 <see cref="ExtractionSystem"/>，本组件只提供位置与半径。
    /// </summary>
    public class ExtractionPointEntity : MonoBehaviour
    {
        /// <summary>
        /// 场景内全部撤离点（OnEnable/OnDisable 自动注册）。
        /// </summary>
        public static readonly List<ExtractionPointEntity> Instances = new List<ExtractionPointEntity>();

        [SerializeField, Tooltip("撤离范围半径（米）")]
        private float _radius = 4f;

        /// <summary>
        /// 撤离范围半径（米）。
        /// </summary>
        public float Radius => _radius;

        private static readonly Color ZoneColor = new Color(0.2f, 0.9f, 0.35f);

        private void Awake()
        {
            EnsureVisual();
        }

        private void OnEnable()
        {
            if (!Instances.Contains(this))
            {
                Instances.Add(this);
            }
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        /// <summary>
        /// 占位视觉：贴地绿色圆盘（正式美术资源接入后替换）。
        /// </summary>
        private void EnsureVisual()
        {
            if (transform.Find("Visual") != null) return;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            var col = visual.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col); // 纯视觉，不参与碰撞
            }
            visual.transform.SetParent(transform, false);
            // Cylinder 枢轴在中心、默认高 2m：scale.y=0.02 → 厚 4cm，底面贴地
            visual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            visual.transform.localScale = new Vector3(_radius * 2f, 0.02f, _radius * 2f);
            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = ZoneColor;
            }
        }

        private void OnDrawGizmos()
        {
            // 线框圆（XZ 平面），场景编辑时直观确认撤离范围
            Gizmos.color = ZoneColor;
            const int seg = 48;
            Vector3 prev = transform.position + new Vector3(_radius, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
