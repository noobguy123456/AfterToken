using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Editor
{
    /// <summary>
    /// 对话编辑器：可视化编辑 TbDialogue / TbDialogueNode（CSV 数据源）。
    /// 左栏对话列表；右侧上半为节点图（卡片 + 连线，按对话流自动布局），下半为选中节点详情。
    /// 保存写回 Configs/GameConfig/Datas/dialogue.csv / dialoguenode.csv；
    /// "保存并导表"额外跑 gen_code_bin_to_project.bat 生成运行时代码与 json。
    /// 菜单：Tools/Dialogue/Dialogue Editor
    /// </summary>
    public class DialogueEditorWindow : EditorWindow
    {
        // ---------- 数据模型 ----------

        private class DialogueRow
        {
            public int Id;
            public string Name = "";
            public int StartNode;
            public bool OnceOnly;
            public int Priority;
        }

        private class NodeRow
        {
            public int Id;
            public int DialogueId;
            public string Type = "line";
            public string Speaker = "";
            public string Text = "";
            public string Choices = "";
            public string Condition = "";
            public string Action = "";
            public int Next;
        }

        private static readonly string[] NodeTypes = { "line", "choice", "setflag", "end" };

        /// <summary>节点类型标记色（卡片左侧色条与标题）。</summary>
        private static readonly Dictionary<string, Color> TypeColors = new Dictionary<string, Color>
        {
            { "line", new Color(0.45f, 0.65f, 0.95f) },
            { "choice", new Color(0.95f, 0.75f, 0.30f) },
            { "setflag", new Color(0.45f, 0.85f, 0.45f) },
            { "end", new Color(0.90f, 0.45f, 0.45f) },
        };

        // 节点图卡片尺寸与间距
        private const float NodeW = 200f;
        private const float NodeH = 74f;
        private const float ColGap = 70f;
        private const float RowGap = 34f;

        private const string DialogueCsv = "Configs/GameConfig/Datas/dialogue.csv";
        private const string NodeCsv = "Configs/GameConfig/Datas/dialoguenode.csv";
        private const string GenBat = "Configs\\GameConfig\\gen_code_bin_to_project.bat";

        private readonly List<DialogueRow> _dialogues = new List<DialogueRow>();
        private readonly List<NodeRow> _nodes = new List<NodeRow>();

        private int _selDialogue = -1;
        private int _selNode = -1;
        private Vector2 _dlgScroll, _graphScroll, _detailScroll;
        private string _status = "";

        /// <summary>节点图布局方向：false=从左到右（深度=列），true=从上到下（深度=行）。</summary>
        private bool _verticalLayout;
        private const string LayoutPrefKey = "DialogueEditor.VerticalLayout";

        // 可拖拽分隔条：左栏宽度 / 节点图高度（EditorPrefs 记忆）
        private float _leftWidth = 240f;
        private float _graphHeight = 340f;
        private bool _draggingV, _draggingH;
        private Rect _vSplitterRect, _hSplitterRect;
        private const string LeftWidthKey = "DialogueEditor.LeftWidth";
        private const string GraphHeightKey = "DialogueEditor.GraphHeight";
        private const float ToolbarH = 20f;
        private const float StatusH = 36f;
        private const float SplitterW = 5f;

        // 样式（EditorStyles 只能在 OnGUI 流程里取，延迟初始化）
        private GUIStyle _dlgRowStyle, _dlgRowSelStyle, _headerStyle;
        private GUIStyle _cardStyle, _cardTitleStyle, _cardBodyStyle, _edgeLabelStyle;

        [MenuItem("Tools/Dialogue/Dialogue Editor")]
        private static void Open()
        {
            var w = GetWindow<DialogueEditorWindow>("Dialogue Editor");
            w.minSize = new Vector2(900f, 520f);
            w.Show();
        }

        /// <summary>窗口打开/域重载后自动加载数据，避免空列表引发空引用。</summary>
        private void OnEnable()
        {
            _verticalLayout = EditorPrefs.GetBool(LayoutPrefKey, false);
            _leftWidth = EditorPrefs.GetFloat(LeftWidthKey, 240f);
            _graphHeight = EditorPrefs.GetFloat(GraphHeightKey, 340f);
            LoadAll();
        }

        private void InitStyles()
        {
            if (_dlgRowStyle != null) return;

            _dlgRowStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(8, 4, 4, 4),
                fontSize = 12,
            };
            _dlgRowSelStyle = new GUIStyle(_dlgRowStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.85f, 1f) },
            };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(2, 0, 4, 4),
            };
            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 6, 4, 4),
            };
            _cardTitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _cardBodyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.88f, 0.88f) },
            };
            _edgeLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.85f, 0.4f) },
                fontSize = 9,
            };
        }

        // ---------- GUI ----------

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();
            HandleSplitterInput();

            float top = ToolbarH;
            float statusH = string.IsNullOrEmpty(_status) ? 0f : StatusH;
            float availW = position.width;
            float availH = position.height - top - statusH;
            if (availH < 100f) return;

            float leftW = Mathf.Clamp(_leftWidth, 160f, Mathf.Max(180f, availW - 320f));
            float graphH = Mathf.Clamp(_graphHeight, 140f, Mathf.Max(160f, availH - 120f));
            bool hasSel = _selDialogue >= 0 && _selDialogue < _dialogues.Count;

            // 左栏：对话列表
            GUILayout.BeginArea(new Rect(0f, top, leftW, availH));
            DrawDialogueList();
            GUILayout.EndArea();

            // 右栏：节点图（上）+ 详情（下）
            float rightX = leftW + SplitterW;
            float rightW = availW - rightX;

            GUILayout.BeginArea(new Rect(rightX, top, rightW, graphH));
            if (hasSel)
            {
                DrawNodeGraph(_dialogues[_selDialogue]);
            }
            else
            {
                EditorGUILayout.HelpBox("选择或新建一个对话", MessageType.Info);
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(rightX, top + graphH + SplitterW, rightW, availH - graphH - SplitterW));
            if (hasSel)
            {
                DrawNodeDetail();
            }
            GUILayout.EndArea();

            // 分隔条（竖：左栏 | 右栏；横：节点图 | 详情）
            _vSplitterRect = new Rect(leftW, top, SplitterW, availH);
            _hSplitterRect = new Rect(rightX, top + graphH, rightW, SplitterW);
            if (Event.current.type == EventType.Repaint)
            {
                var c = (_draggingV || _draggingH) ? new Color(0.55f, 0.85f, 1f) : new Color(1f, 1f, 1f, 0.12f);
                GUI.DrawTexture(_vSplitterRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, _draggingV ? new Color(0.55f, 0.85f, 1f) : c, 0f, 0f);
                GUI.DrawTexture(_hSplitterRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, _draggingH ? new Color(0.55f, 0.85f, 1f) : c, 0f, 0f);
            }
            EditorGUIUtility.AddCursorRect(_vSplitterRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(_hSplitterRect, MouseCursor.ResizeVertical);

            // 状态栏
            if (statusH > 0f)
            {
                EditorGUI.HelpBox(new Rect(0f, position.height - statusH, availW, statusH), _status, MessageType.None);
            }
        }

        /// <summary>分隔条拖拽：竖条调左栏宽度，横条调节点图高度；松手时写 EditorPrefs。</summary>
        private void HandleSplitterInput()
        {
            var e = Event.current;
            switch (e.rawType)
            {
                case EventType.MouseDown:
                    if (_vSplitterRect.Contains(e.mousePosition)) { _draggingV = true; e.Use(); }
                    else if (_hSplitterRect.Contains(e.mousePosition)) { _draggingH = true; e.Use(); }
                    break;
                case EventType.MouseDrag:
                    if (_draggingV) { _leftWidth = e.mousePosition.x; Repaint(); e.Use(); }
                    else if (_draggingH) { _graphHeight = e.mousePosition.y - ToolbarH; Repaint(); e.Use(); }
                    break;
                case EventType.MouseUp:
                    if (_draggingV) EditorPrefs.SetFloat(LeftWidthKey, _leftWidth);
                    if (_draggingH) EditorPrefs.SetFloat(GraphHeightKey, _graphHeight);
                    _draggingV = _draggingH = false;
                    break;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("重新加载", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                LoadAll();
            }
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SaveAll();
            }
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("保存并导表", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                if (SaveAll())
                {
                    RunGen();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.Label("数据源: dialogue.csv / dialoguenode.csv", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDialogueList()
        {
            EditorGUILayout.LabelField("对话（TbDialogue）", _headerStyle);
            _dlgScroll = EditorGUILayout.BeginScrollView(_dlgScroll, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < _dialogues.Count; i++)
            {
                var d = _dialogues[i];
                bool selected = i == _selDialogue;

                EditorGUILayout.BeginHorizontal();
                if (selected)
                {
                    var mark = EditorGUILayout.GetControlRect(false, 0f, GUILayout.Width(4));
                    EditorGUI.DrawRect(mark, new Color(0.55f, 0.85f, 1f));
                }

                string label = $"{d.Id}  {d.Name}" + (d.OnceOnly ? "  ①" : "");
                if (GUILayout.Button(label, selected ? _dlgRowSelStyle : _dlgRowStyle))
                {
                    _selDialogue = i;
                    _selNode = -1;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    GUI.backgroundColor = Color.white;
                    if (EditorUtility.DisplayDialog("删除对话", $"删除对话 {d.Id} 及其全部节点？", "删除", "取消"))
                    {
                        _nodes.RemoveAll(n => n.DialogueId == d.Id);
                        _dialogues.RemoveAt(i);
                        _selDialogue = -1;
                        _selNode = -1;
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            if (GUILayout.Button("+ 新建对话"))
            {
                var d = new DialogueRow { Id = NextDialogueId(), Name = "new_dialogue", StartNode = 0 };
                _dialogues.Add(d);
                _selDialogue = _dialogues.Count - 1;
                _selNode = -1;
            }
            GUI.backgroundColor = Color.white;

            if (_selDialogue >= 0 && _selDialogue < _dialogues.Count)
            {
                var d = _dialogues[_selDialogue];
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("对话属性", _headerStyle);
                d.Id = EditorGUILayout.IntField("ID", d.Id);
                d.Name = EditorGUILayout.TextField("注释名", d.Name);
                d.StartNode = EditorGUILayout.IntField("起始节点", d.StartNode);
                d.OnceOnly = EditorGUILayout.Toggle("只触发一次", d.OnceOnly);
                d.Priority = EditorGUILayout.IntField("优先级", d.Priority);
                EditorGUILayout.EndVertical();
            }
        }

        // ---------- 节点图 ----------

        private void DrawNodeGraph(DialogueRow dlg)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"节点图（对话 {dlg.Id}）", _headerStyle);
            GUILayout.FlexibleSpace();
            // 布局方向切换：从左到右 / 从上到下
            var newVertical = GUILayout.Toggle(_verticalLayout, _verticalLayout ? "↓ 纵向" : "→ 横向", EditorStyles.miniButton, GUILayout.Width(64));
            if (newVertical != _verticalLayout)
            {
                _verticalLayout = newVertical;
                EditorPrefs.SetBool(LayoutPrefKey, _verticalLayout);
            }
            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            if (GUILayout.Button("+ 新建节点", GUILayout.Width(90)))
            {
                var n = new NodeRow { Id = NextNodeId(), DialogueId = dlg.Id };
                _nodes.Add(n);
                _selNode = _nodes.IndexOf(n);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            var nodes = NodesOf(dlg.Id);
            if (nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("该对话还没有节点，点右上角 + 新建节点", MessageType.Info);
                return;
            }

            var layout = ComputeLayout(dlg, nodes, _verticalLayout);
            float contentW = 0f, contentH = 0f;
            foreach (var kv in layout)
            {
                contentW = Mathf.Max(contentW, kv.Value.xMax + 20f);
                contentH = Mathf.Max(contentH, kv.Value.yMax + 20f);
            }

            _graphScroll = EditorGUILayout.BeginScrollView(_graphScroll, "box", GUILayout.ExpandHeight(true));
            GUILayoutUtility.GetRect(contentW, contentH);

            // 先画连线（压在卡片下面）；横向从右缘出/左缘入，纵向从底缘出/顶缘入
            Handles.BeginGUI();
            foreach (var n in nodes)
            {
                if (!layout.TryGetValue(n.Id, out var r)) continue;
                var from = _verticalLayout ? new Vector3(r.center.x, r.yMax) : new Vector3(r.xMax, r.center.y);

                if (n.Type == "choice")
                {
                    foreach (var opt in ParseChoiceTargets(n.Choices))
                    {
                        if (layout.TryGetValue(opt.Value, out var tr))
                        {
                            var to = _verticalLayout ? new Vector3(tr.center.x, tr.yMin) : new Vector3(tr.xMin, tr.center.y);
                            DrawEdge(from, to, TypeColors["choice"], _verticalLayout);
                            // 选项文本标在线中段
                            var mid = (from + to) * 0.5f;
                            GUI.Label(new Rect(mid.x - 60f, mid.y - 8f, 120f, 16f), Truncate(opt.Key, 14), _edgeLabelStyle);
                        }
                    }
                }
                else if (n.Next != 0 && layout.TryGetValue(n.Next, out var tr))
                {
                    var to = _verticalLayout ? new Vector3(tr.center.x, tr.yMin) : new Vector3(tr.xMin, tr.center.y);
                    DrawEdge(from, to, new Color(0.55f, 0.7f, 1f), _verticalLayout);
                }
            }
            Handles.EndGUI();

            // 再画卡片
            foreach (var n in nodes)
            {
                if (!layout.TryGetValue(n.Id, out var r)) continue;
                DrawNodeCard(dlg, n, r);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawEdge(Vector3 from, Vector3 to, Color color, bool vertical)
        {
            Vector3 fromTan, toTan;
            if (vertical)
            {
                float tangent = Mathf.Clamp(Mathf.Abs(to.y - from.y) * 0.5f, 25f, 60f);
                fromTan = from + Vector3.up * tangent;
                toTan = to + Vector3.down * tangent;
            }
            else
            {
                float tangent = Mathf.Clamp(Mathf.Abs(to.x - from.x) * 0.5f, 30f, 80f);
                fromTan = from + Vector3.right * tangent;
                toTan = to + Vector3.left * tangent;
            }
            Handles.DrawBezier(from, to, fromTan, toTan, color, null, 2.5f);
        }

        private void DrawNodeCard(DialogueRow dlg, NodeRow n, Rect r)
        {
            bool selected = ReferenceEquals(n, SelectedNode());
            bool isStart = n.Id == dlg.StartNode;
            var typeColor = TypeColors.TryGetValue(n.Type, out var c) ? c : Color.gray;

            // 选中/起始外框
            if (selected)
            {
                EditorGUI.DrawRect(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), new Color(0.55f, 0.85f, 1f));
            }
            else if (isStart)
            {
                EditorGUI.DrawRect(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), new Color(0.4f, 0.8f, 0.4f));
            }

            // 卡片本体（整面可点）
            if (GUI.Button(r, GUIContent.none, _cardStyle))
            {
                _selNode = _nodes.IndexOf(n);
                GUI.FocusControl(null);
            }

            // 左侧类型色条
            EditorGUI.DrawRect(new Rect(r.x, r.y, 5f, r.height), typeColor);

            // 标题行：类型 + id + 标记
            string title = $"{n.Type} #{n.Id}";
            if (isStart) title = "▶ " + title;
            if (!string.IsNullOrEmpty(n.Condition)) title += "  ⚑";
            var oldColor = _cardTitleStyle.normal.textColor;
            _cardTitleStyle.normal.textColor = typeColor;
            GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 14f, 16f), title, _cardTitleStyle);
            _cardTitleStyle.normal.textColor = oldColor;

            // 正文摘要：line/choice 显说话人+正文；setflag 显动作
            string body;
            if (n.Type == "setflag")
            {
                body = n.Action;
            }
            else if (n.Type == "end")
            {
                body = "(对话结束)";
            }
            else
            {
                body = string.IsNullOrEmpty(n.Speaker) ? n.Text : $"{n.Speaker}: {n.Text}";
            }
            GUI.Label(new Rect(r.x + 10f, r.y + 22f, r.width - 16f, r.height - 26f), Truncate(body, 60), _cardBodyStyle);
        }

        /// <summary>
        /// 自动布局：从起始节点 BFS，深度 = 列（横向）或行（纵向）；未连通节点排在最后。
        /// </summary>
        private static Dictionary<int, Rect> ComputeLayout(DialogueRow dlg, List<NodeRow> nodes, bool vertical)
        {
            var depth = new Dictionary<int, int>();
            var byId = new Dictionary<int, NodeRow>();
            foreach (var n in nodes) byId[n.Id] = n;

            // BFS
            var queue = new Queue<NodeRow>();
            if (byId.TryGetValue(dlg.StartNode, out var start))
            {
                depth[start.Id] = 0;
                queue.Enqueue(start);
            }
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int d = depth[cur.Id];
                foreach (int target in OutTargets(cur))
                {
                    if (target != 0 && byId.TryGetValue(target, out var tn) && !depth.ContainsKey(target))
                    {
                        depth[target] = d + 1;
                        queue.Enqueue(tn);
                    }
                }
            }
            // 未连通节点放到最后一层之后
            int maxDepth = 0;
            foreach (var kv in depth) maxDepth = Mathf.Max(maxDepth, kv.Value);
            foreach (var n in nodes)
            {
                if (!depth.ContainsKey(n.Id))
                {
                    depth[n.Id] = maxDepth + 1;
                }
            }

            // 同层序号：横向 = 同列第几行；纵向 = 同行第几列
            var orderInLayer = new Dictionary<int, int>();
            var layout = new Dictionary<int, Rect>();
            foreach (var n in nodes)
            {
                int layer = depth[n.Id];
                int order = orderInLayer.TryGetValue(layer, out int cnt) ? cnt : 0;
                orderInLayer[layer] = order + 1;

                float x, y;
                if (vertical)
                {
                    x = 20f + order * (NodeW + 24f);
                    y = 20f + layer * (NodeH + RowGap + 14f);
                }
                else
                {
                    x = 20f + layer * (NodeW + ColGap);
                    y = 20f + order * (NodeH + RowGap);
                }
                layout[n.Id] = new Rect(x, y, NodeW, NodeH);
            }
            return layout;
        }

        /// <summary>节点的所有出边目标（next + 选项）。</summary>
        private static IEnumerable<int> OutTargets(NodeRow n)
        {
            if (n.Type == "choice")
            {
                foreach (var opt in ParseChoiceTargets(n.Choices))
                {
                    yield return opt.Value;
                }
            }
            else if (n.Next != 0)
            {
                yield return n.Next;
            }
        }

        private static List<KeyValuePair<string, int>> ParseChoiceTargets(string raw)
        {
            var result = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrWhiteSpace(raw)) return result;
            foreach (var item in raw.Split('|'))
            {
                var pair = item.Split(new[] { "->" }, StringSplitOptions.None);
                if (pair.Length == 2 && int.TryParse(pair[1].Trim(), out int target))
                {
                    result.Add(new KeyValuePair<string, int>(pair[0].Trim(), target));
                }
            }
            return result;
        }

        // ---------- 节点详情 ----------

        private void DrawNodeDetail()
        {
            var n = SelectedNode();
            if (n == null)
            {
                EditorGUILayout.HelpBox("点击节点卡片进行编辑", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField($"节点 {n.Id} 详情", _headerStyle);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            n.Id = EditorGUILayout.IntField("节点 ID", n.Id);
            int typeIdx = Mathf.Max(0, Array.IndexOf(NodeTypes, n.Type));
            n.Type = NodeTypes[EditorGUILayout.Popup("类型", typeIdx, NodeTypes)];

            if (n.Type == "line" || n.Type == "choice")
            {
                n.Speaker = EditorGUILayout.TextField("说话人", n.Speaker);
                EditorGUILayout.LabelField("正文");
                n.Text = EditorGUILayout.TextArea(n.Text, GUILayout.MinHeight(48));
            }
            if (n.Type == "choice")
            {
                EditorGUILayout.LabelField("选项（文本->节点ID，多条用 | 分隔）", EditorStyles.miniLabel);
                n.Choices = EditorGUILayout.TextField("选项", n.Choices);
            }

            n.Condition = EditorGUILayout.TextField("进入条件", n.Condition);
            n.Action = EditorGUILayout.TextField("到达动作", n.Action);
            if (n.Type != "end")
            {
                n.Next = EditorGUILayout.IntField("下一节点(0=结束)", n.Next);
            }

            EditorGUILayout.Space();
            DrawValidation(n);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawValidation(NodeRow n)
        {
            var sb = new StringBuilder();
            if (n.Id == 0) sb.AppendLine("• 节点 ID 不能为 0");
            if (_nodes.Exists(x => x != n && x.Id == n.Id)) sb.AppendLine($"• 节点 ID {n.Id} 重复");
            if (n.Next != 0 && !_nodes.Exists(x => x.Id == n.Next)) sb.AppendLine($"• next 指向的节点 {n.Next} 不存在");
            if (n.Type == "choice" && !string.IsNullOrEmpty(n.Choices))
            {
                foreach (var item in n.Choices.Split('|'))
                {
                    var pair = item.Split(new[] { "->" }, StringSplitOptions.None);
                    if (pair.Length != 2 || !int.TryParse(pair[1].Trim(), out int target))
                    {
                        sb.AppendLine($"• 选项 \"{item}\" 格式错误（应为 文本->节点ID）");
                    }
                    else if (!_nodes.Exists(x => x.Id == target))
                    {
                        sb.AppendLine($"• 选项 \"{pair[0].Trim()}\" 指向的节点 {target} 不存在");
                    }
                }
            }

            var dlg = _selDialogue >= 0 && _selDialogue < _dialogues.Count ? _dialogues[_selDialogue] : null;
            if (dlg != null && dlg.StartNode != 0 && !_nodes.Exists(x => x.Id == dlg.StartNode))
            {
                sb.AppendLine($"• 对话起始节点 {dlg.StartNode} 不存在");
            }

            if (sb.Length > 0)
            {
                EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.Warning);
            }
        }

        // ---------- 数据操作 ----------

        private NodeRow SelectedNode()
        {
            return _selNode >= 0 && _selNode < _nodes.Count ? _nodes[_selNode] : null;
        }

        private List<NodeRow> NodesOf(int dialogueId)
        {
            var list = _nodes.FindAll(n => n.DialogueId == dialogueId);
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            return list;
        }

        private int NextDialogueId()
        {
            int max = 0;
            foreach (var d in _dialogues) max = Math.Max(max, d.Id);
            return max + 1;
        }

        private int NextNodeId()
        {
            int max = 0;
            foreach (var n in _nodes) max = Math.Max(max, n.Id);
            return max + 1;
        }

        private void LoadAll()
        {
            _dialogues.Clear();
            _nodes.Clear();
            _selDialogue = -1;
            _selNode = -1;

            foreach (var row in ReadCsv(DialogueCsv, 3))
            {
                _dialogues.Add(new DialogueRow
                {
                    Id = ParseInt(row, 1),
                    Name = Get(row, 2),
                    StartNode = ParseInt(row, 3),
                    OnceOnly = Get(row, 4).Equals("true", StringComparison.OrdinalIgnoreCase),
                    Priority = ParseInt(row, 5),
                });
            }

            foreach (var row in ReadCsv(NodeCsv, 3))
            {
                _nodes.Add(new NodeRow
                {
                    Id = ParseInt(row, 1),
                    DialogueId = ParseInt(row, 2),
                    Type = Get(row, 3),
                    Speaker = Get(row, 4),
                    Text = Get(row, 5),
                    Choices = Get(row, 6),
                    Condition = Get(row, 7),
                    Action = Get(row, 8),
                    Next = ParseInt(row, 9),
                });
            }

            _status = $"已加载 {_dialogues.Count} 段对话、{_nodes.Count} 个节点";
        }

        private bool SaveAll()
        {
            try
            {
                var dlgRows = new List<string[]>
                {
                    new[] { "##var", "id", "name", "startNode", "onceOnly", "priority" },
                    new[] { "##type", "int", "string", "int", "bool", "int" },
                    new[] { "##", "对话ID", "注释名(不显示)", "起始节点ID", "只触发一次", "优先级" },
                };
                foreach (var d in _dialogues)
                {
                    dlgRows.Add(new[] { "", d.Id.ToString(), d.Name, d.StartNode.ToString(), d.OnceOnly ? "true" : "false", d.Priority.ToString() });
                }
                WriteCsv(DialogueCsv, dlgRows);

                var nodeRows = new List<string[]>
                {
                    new[] { "##var", "id", "dialogueId", "type", "speaker", "text", "choices", "condition", "action", "next" },
                    new[] { "##type", "int", "int", "string", "string", "string", "string", "string", "string", "int" },
                    new[] { "##", "节点ID", "所属对话", "类型(line/choice/setflag/end)", "说话人", "正文", "选项(文本->节点|...)", "进入条件", "到达动作", "下一节点(0=结束)" },
                };
                _nodes.Sort((a, b) => a.Id.CompareTo(b.Id));
                foreach (var n in _nodes)
                {
                    nodeRows.Add(new[] { "", n.Id.ToString(), n.DialogueId.ToString(), n.Type, n.Speaker, n.Text, n.Choices, n.Condition, n.Action, n.Next.ToString() });
                }
                WriteCsv(NodeCsv, nodeRows);

                _status = $"已保存（{DateTime.Now:HH:mm:ss}）";
                return true;
            }
            catch (Exception e)
            {
                _status = "保存失败：" + e.Message;
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        private void RunGen()
        {
            _status = "导表中…";
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + GenBat)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.StandardInput.Close(); // bat 末尾 pause 需要 stdin
                    string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(120000))
                    {
                        p.Kill();
                        _status = "导表超时";
                        return;
                    }
                    bool ok = output.Contains("[Luban] Generation succeeded");
                    _status = ok ? "保存并导表完成" : "导表失败，详见 Console";
                    if (!ok) UnityEngine.Debug.LogError("[DialogueEditor] 导表输出：\n" + output);
                }
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                _status = "导表失败：" + e.Message;
                UnityEngine.Debug.LogException(e);
            }
        }

        // ---------- CSV 读写（支持引号字段） ----------

        private static List<string[]> ReadCsv(string path, int skipRows)
        {
            var result = new List<string[]>();
            if (!File.Exists(path)) return result;

            var rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
            for (int i = skipRows; i < rows.Count; i++)
            {
                if (rows[i].Count > 1) result.Add(rows[i].ToArray());
            }
            return result;
        }

        private static List<List<string>> ParseCsv(string content)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { row.Add(field.ToString()); field.Length = 0; }
                else if (c == '\n') { row.Add(field.ToString()); field.Length = 0; rows.Add(row); row = new List<string>(); }
                else if (c != '\r') field.Append(c);
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
            return rows;
        }

        private static void WriteCsv(string path, List<string[]> rows)
        {
            var sb = new StringBuilder();
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(EscapeCsvField(row[i] ?? ""));
                }
                sb.Append('\n');
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string EscapeCsvField(string s)
        {
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string Get(string[] row, int i) => i < row.Length ? row[i] : "";
        private static int ParseInt(string[] row, int i) => int.TryParse(Get(row, i), out int v) ? v : 0;
        private static string Truncate(string s, int len) => string.IsNullOrEmpty(s) ? "" : (s.Length <= len ? s : s.Substring(0, len) + "…");
    }
}
