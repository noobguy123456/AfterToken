using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// NPC 实体（场景挂载，目前在经营场景使用）。
    /// 负责触发区检测与占位视觉（胶囊体 + 头顶名字牌）。
    /// 玩家进入触发区后按 E 交谈（NpcSystem 处理）。
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class NpcEntity : MonoBehaviour
    {
        [SerializeField] private int _npcId = 1;

        /// <summary>
        /// 占位视觉颜色（钢蓝色），与玩家/敌人占位区分。
        /// </summary>
        private static readonly Color PlaceholderColor = new Color(0.4f, 0.55f, 0.75f, 1f);

        public int NpcId => _npcId;
        public bool PlayerInside { get; private set; }

        private TextMeshPro _nameLabel;

        private void Awake()
        {
            var collider = GetComponent<SphereCollider>();
            collider.isTrigger = true;
            if (collider.radius <= 0.5f)
            {
                // 默认 0.5m 触发半径太小，放宽到 1.5m 便于交互
                collider.radius = 1.5f;
            }

            EnsureVisual();
        }

        private void Start()
        {
            // 配置在启动时已预加载，这里填名字牌
            var cfg = NpcConfigMgr.Instance.Get(_npcId);
            if (cfg != null)
            {
                SetNameLabel(cfg.Name, cfg.Role);
            }
            else
            {
                Log.Warning($"[NpcEntity] 找不到 NPC 配置 id={_npcId}");
            }
        }

        /// <summary>
        /// 占位视觉：胶囊体 + 头顶名字牌（Billboard 面向相机）。
        /// 后续换正式 NPC 模型时整体替换 Visual 子节点即可。
        /// </summary>
        private void EnsureVisual()
        {
            if (transform.Find("Visual") == null)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                // 占位视觉自带碰撞体会挡住玩家移动与触发区检测，删掉
                var visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    Destroy(visualCollider);
                }
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = PlaceholderColor;
                }
            }

            if (_nameLabel == null)
            {
                var labelGo = new GameObject("NameLabel");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
                _nameLabel = labelGo.AddComponent<TextMeshPro>();
                // 俯视相机下 fontSize 3 会显得过大并容易顶出屏幕，用小字号+缩放控制显示尺寸
                _nameLabel.fontSize = 2f;
                labelGo.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                _nameLabel.alignment = TextAlignmentOptions.Center;
                _nameLabel.color = Color.white;
                labelGo.AddComponent<BillboardFaceCamera>();
            }
        }

        private void SetNameLabel(string npcName, string role)
        {
            if (_nameLabel == null) return;
            _nameLabel.text = string.IsNullOrEmpty(role) ? npcName : $"{npcName}\n<size=70%><color=#AAAAAA>{role}</color></size>";
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = true;
            NpcSystem.Instance?.OnPlayerEnteredNpc(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = false;
            NpcSystem.Instance?.OnPlayerExitedNpc(this);
        }
    }
}
