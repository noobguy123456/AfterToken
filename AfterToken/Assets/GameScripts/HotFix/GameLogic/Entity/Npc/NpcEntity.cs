using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// NPC 实体（场景挂载，目前在经营场景使用）。
    /// 负责触发区检测与占位视觉（胶囊体 + 头顶名字牌 + MMO 式任务标记 !/?）。
    /// 玩家进入触发区后按 E 交谈（NpcSystem 处理）。
    /// 任务标记规则：黄 ? 可交付 > 黄 ! 可接取 > 灰 ! 进行中 > 无任务时隐藏。
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

        /// <summary>巡逻路径点（空 = 站桩）。</summary>
        private Vector3[] _waypoints;
        private int _wpIndex;
        private int _wpDir = 1;
        private float _moveSpeed;
        /// <summary>到点停留时间（秒）。</summary>
        private const float WaypointDwell = 1f;
        private float _dwellTimer;
        private Transform _player;

        private TextMeshPro _nameLabel;
        private TextMeshPro _questMarker;
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        // ---- 头顶任务标记（MMO 惯例）：黄 ? 可交付 > 黄 ! 可接取 > 灰 ! 进行中 > 无 ----
        private static readonly Color QuestReadyColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color QuestAvailableColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color QuestInProgressColor = new Color(0.6f, 0.6f, 0.6f, 1f);

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
                InitPatrol(cfg.MoveSpeed, cfg.PatrolPath);
            }
            else
            {
                Log.Warning($"[NpcEntity] 找不到 NPC 配置 id={_npcId}");
            }

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                _player = playerGo.transform;
            }

            // 任务标记：跟随任务事件即时刷新
            RefreshQuestMarker();
            _eventMgr.AddEvent<int>(IQuestEvent_Event.OnQuestAccepted, _ => RefreshQuestMarker());
            _eventMgr.AddEvent<int>(IQuestEvent_Event.OnQuestReadyToTurnIn, _ => RefreshQuestMarker());
            _eventMgr.AddEvent<int>(IQuestEvent_Event.OnQuestCompleted, _ => RefreshQuestMarker());
            _eventMgr.AddEvent<int, int, int, int>(IQuestEvent_Event.OnObjectiveProgress, (_, _, _, _) => RefreshQuestMarker());
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
        }

        /// <summary>
        /// 刷新头顶任务标记：可交付 黄? > 可接取 黄! > 进行中 灰! > 隐藏。
        /// </summary>
        private void RefreshQuestMarker()
        {
            EnsureQuestMarker();

            var quests = QuestConfigMgr.Instance.GetQuestsByGiver(_npcId);
            bool anyReady = false;
            bool anyAcceptable = false;
            bool anyInProgress = false;
            foreach (var quest in quests)
            {
                switch (QuestSystem.GetState(quest.Id))
                {
                    case QuestState.ReadyToTurnIn:
                        anyReady = true;
                        break;
                    case QuestState.Active:
                        anyInProgress = true;
                        break;
                    case QuestState.Inactive:
                        if (QuestSystem.CanAccept(quest.Id))
                        {
                            anyAcceptable = true;
                        }
                        break;
                }
            }

            if (anyReady)
            {
                _questMarker.text = "?";
                _questMarker.color = QuestReadyColor;
                _questMarker.gameObject.SetActive(true);
            }
            else if (anyAcceptable)
            {
                _questMarker.text = "!";
                _questMarker.color = QuestAvailableColor;
                _questMarker.gameObject.SetActive(true);
            }
            else if (anyInProgress)
            {
                _questMarker.text = "!";
                _questMarker.color = QuestInProgressColor;
                _questMarker.gameObject.SetActive(true);
            }
            else
            {
                _questMarker.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 头顶任务标记占位：TextMeshPro 文本符号 + Billboard（同名字牌范式，后续美术替换为图标贴片）。
        /// </summary>
        private void EnsureQuestMarker()
        {
            if (_questMarker != null) return;
            var markerGo = new GameObject("QuestMarker");
            markerGo.transform.SetParent(transform, false);
            markerGo.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            _questMarker = markerGo.AddComponent<TextMeshPro>();
            _questMarker.fontSize = 3.5f;
            markerGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            _questMarker.alignment = TextAlignmentOptions.Center;
            markerGo.AddComponent<BillboardFaceCamera>();
            markerGo.SetActive(false);
        }

        /// <summary>
        /// 初始化巡逻：patrolPath 格式 "x,z|x,z"，moveSpeed<=0 或路径不足两点时站桩。
        /// </summary>
        private void InitPatrol(float moveSpeed, string patrolPath)
        {
            _moveSpeed = moveSpeed;
            if (_moveSpeed <= 0f || string.IsNullOrWhiteSpace(patrolPath)) return;

            var list = new System.Collections.Generic.List<Vector3>();
            foreach (var item in patrolPath.Split('|'))
            {
                var pair = item.Split(',');
                if (pair.Length == 2 &&
                    float.TryParse(pair[0].Trim(), out float x) &&
                    float.TryParse(pair[1].Trim(), out float z))
                {
                    list.Add(new Vector3(x, 0f, z));
                }
                else
                {
                    Log.Warning($"[NpcEntity] 巡逻路径点格式错误: {item}（应为 x,z）");
                }
            }
            if (list.Count >= 2)
            {
                _waypoints = list.ToArray();
            }
        }

        private void Update()
        {
            // 正在与本 NPC 对话：站住并转身面向玩家
            if (DialogueSystem.Instance != null &&
                DialogueSystem.Instance.IsPlaying &&
                DialogueSystem.Instance.CurrentNpcId == _npcId)
            {
                // 玩家生成晚于场景加载，Start 里可能拿不到，这里懒获取
                if (_player == null)
                {
                    var playerGo = GameObject.FindGameObjectWithTag("Player");
                    if (playerGo != null)
                    {
                        _player = playerGo.transform;
                    }
                }
                if (_player != null)
                {
                    FaceTowards(_player.position);
                }
                return;
            }

            Patrol();
        }

        /// <summary>
        /// 巡逻：路径点间往返（ping-pong），到点停留 1 秒。
        /// </summary>
        private void Patrol()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            if (_dwellTimer > 0f)
            {
                _dwellTimer -= Time.deltaTime;
                return;
            }

            var pos = transform.position;
            var target = _waypoints[_wpIndex];
            var flat = new Vector3(target.x - pos.x, 0f, target.z - pos.z);

            if (flat.magnitude < 0.05f)
            {
                // 到点：停留后走向下一点
                _dwellTimer = WaypointDwell;
                int next = _wpIndex + _wpDir;
                if (next < 0 || next >= _waypoints.Length)
                {
                    _wpDir = -_wpDir;
                    next = _wpIndex + _wpDir;
                }
                _wpIndex = next;
                return;
            }

            FaceTowards(target);
            transform.position = pos + flat.normalized * (_moveSpeed * Time.deltaTime);
        }

        /// <summary>平滑转身面向目标点（仅 Y 轴）。</summary>
        private void FaceTowards(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            var look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.deltaTime);
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
