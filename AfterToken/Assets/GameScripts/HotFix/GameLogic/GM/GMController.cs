#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic.GM
{
    /// <summary>
    /// GM 调试控制器：提供控制台和 GM 面板。
    /// 仅在编辑器或 Development Build 中编译。
    /// </summary>
    public class GMController : MonoBehaviour
    {
        public static GMController Instance { get; private set; }

        [Header("快捷键")]
        [SerializeField] private KeyCode _consoleKey = KeyCode.BackQuote; // ` 键
        [SerializeField] private KeyCode _panelKey = KeyCode.F1;

        private bool _showConsole;
        private bool _showPanel;
        private string _inputText = "";
        private Vector2 _logScrollPosition;
        private readonly List<string> _logs = new List<string>();
        private readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();

        private Rect _consoleRect = new Rect(10, Screen.height - 220, 600, 200);
        private Rect _panelRect = new Rect(Screen.width - 310, 10, 300, 400);

        private void Awake()
        {
            Instance = this;
            RegisterCommands();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_consoleKey))
            {
                _showConsole = !_showConsole;
            }

            if (Input.GetKeyDown(_panelKey))
            {
                _showPanel = !_showPanel;
            }

            // 控制台打开时按回车执行
            if (_showConsole && Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteInput();
            }

            // 按 ESC 关闭窗口
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _showConsole = false;
                _showPanel = false;
            }
        }

        private void OnGUI()
        {
            if (_showConsole) DrawConsole();
            if (_showPanel) DrawPanel();
        }

        #region 控制台

        private void DrawConsole()
        {
            _consoleRect = GUILayout.Window(0, _consoleRect, ConsoleWindow, "GM Console (~)");
        }

        private void ConsoleWindow(int id)
        {
            // 日志区域
            _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition, GUILayout.Height(140));
            foreach (var log in _logs)
            {
                GUILayout.Label(log);
            }
            GUILayout.EndScrollView();

            // 输入区域
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("GMConsoleInput");
            _inputText = GUILayout.TextField(_inputText, GUILayout.Height(25));
            if (GUILayout.Button("执行", GUILayout.Width(60), GUILayout.Height(25)))
            {
                ExecuteInput();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void ExecuteInput()
        {
            if (string.IsNullOrWhiteSpace(_inputText)) return;

            LogToConsole("> " + _inputText);
            var parts = _inputText.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var cmd = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(cmd, out var action))
            {
                try
                {
                    action(args);
                }
                catch (Exception e)
                {
                    LogToConsole($"[Error] 命令执行失败: {e.Message}");
                }
            }
            else
            {
                LogToConsole($"[Error] 未知命令: {cmd}，输入 help 查看列表");
            }

            _inputText = "";
            GUI.FocusControl("GMConsoleInput");
        }

        #endregion

        #region GM 面板

        private void DrawPanel()
        {
            _panelRect = GUILayout.Window(1, _panelRect, PanelWindow, "GM Panel (F1)");
        }

        private void PanelWindow(int id)
        {
            GUILayout.Label("玩家", GUI.skin.box);
            if (GUILayout.Button("满血")) RunCommand("hp", "1000");
            if (GUILayout.Button("无敌（高血量）")) RunCommand("maxhp", "99999");

            GUILayout.Label("武器", GUI.skin.box);
            if (GUILayout.Button("手枪")) RunCommand("weapon", "1001");
            if (GUILayout.Button("冲锋枪")) RunCommand("weapon", "1002");
            if (GUILayout.Button("步枪")) RunCommand("weapon", "1003");
            if (GUILayout.Button("狙击枪")) RunCommand("weapon", "1004");
            if (GUILayout.Button("火箭筒")) RunCommand("weapon", "1005");
            if (GUILayout.Button("满弹药")) RunCommand("ammo", "999");

            GUILayout.Label("战斗", GUI.skin.box);
            if (GUILayout.Button("杀死所有敌人")) RunCommand("killall");
            if (GUILayout.Button("生成敌人 9001")) RunCommand("spawn", "9001");
            if (GUILayout.Button("生成敌人 9002")) RunCommand("spawn", "9002");

            GUILayout.Label("时间 / 关卡", GUI.skin.box);
            if (GUILayout.Button("慢动作 0.2x")) RunCommand("time", "0.2");
            if (GUILayout.Button("正常速度")) RunCommand("time", "1");
            if (GUILayout.Button("关卡 1")) RunCommand("level", "1");
            if (GUILayout.Button("关卡 2")) RunCommand("level", "2");
            if (GUILayout.Button("重新开始")) RunCommand("restart");

            GUILayout.Label("配置", GUI.skin.box);
            if (GUILayout.Button("重载配置")) RunCommand("reload");

            GUI.DragWindow();
        }

        #endregion

        #region 命令注册

        private void RegisterCommands()
        {
            _commands["help"] = args =>
            {
                LogToConsole("可用命令:");
                LogToConsole("  help              显示帮助");
                LogToConsole("  weapon <id>       装备武器到当前槽位");
                LogToConsole("  hp <value>        设置当前血量");
                LogToConsole("  maxhp <value>     设置最大血量并回满");
                LogToConsole("  ammo <value>      设置当前弹药");
                LogToConsole("  god               无敌模式");
                LogToConsole("  killall           杀死所有敌人");
                LogToConsole("  spawn <enemyId>   在玩家附近生成敌人");
                LogToConsole("  time <scale>      设置时间缩放");
                LogToConsole("  level <id>        切换关卡");
                LogToConsole("  reload            重新加载配置表");
                LogToConsole("  clear             清空控制台");
            };

            _commands["weapon"] = args =>
            {
                if (!TryParseInt(args, 0, out var id)) return;
                var weaponSystem = WeaponSystem.Instance;
                if (weaponSystem == null)
                {
                    LogToConsole("[Error] WeaponSystem 未初始化");
                    return;
                }
                weaponSystem.GM_EquipAndSwitch(id);
                LogToConsole($"装备武器 {id}");
            };

            _commands["hp"] = args =>
            {
                if (!TryParseInt(args, 0, out var hp)) return;
                PlayerSystem.Instance?.GM_SetHp(hp);
                LogToConsole($"设置血量 {hp}");
            };

            _commands["maxhp"] = args =>
            {
                if (!TryParseInt(args, 0, out var maxHp)) return;
                PlayerSystem.Instance?.GM_SetMaxHp(maxHp);
                LogToConsole($"设置最大血量 {maxHp}");
            };

            _commands["ammo"] = args =>
            {
                if (!TryParseInt(args, 0, out var ammo)) return;
                var weapon = WeaponSystem.Instance?.CurrentWeapon;
                if (weapon == null)
                {
                    LogToConsole("[Error] 当前没有武器");
                    return;
                }
                weapon.GM_SetAmmo(ammo);
                LogToConsole($"设置弹药 {ammo}");
            };

            _commands["god"] = args =>
            {
                PlayerSystem.Instance?.GM_SetMaxHp(99999);
                LogToConsole("已开启无敌模式");
            };

            _commands["killall"] = args =>
            {
                var enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    enemy.TakeDamage(999999, Vector2.zero);
                }
                LogToConsole($"杀死 {enemies.Length} 个敌人");
            };

            _commands["spawn"] = args =>
            {
                if (!TryParseInt(args, 0, out var enemyId)) return;
                var playerPos = PlayerSystem.Instance?.GetPlayerPosition() ?? Vector3.zero;
                var offset = new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), 0);
                SpawnEnemyAsync(enemyId, playerPos + offset).Forget();
            };

            _commands["time"] = args =>
            {
                if (!TryParseFloat(args, 0, out var scale)) return;
                Time.timeScale = Mathf.Clamp(scale, 0f, 2f);
                LogToConsole($"设置时间缩放 {Time.timeScale}");
            };

            _commands["level"] = args =>
            {
                if (!TryParseInt(args, 0, out var levelId)) return;
                BattleContext.CurrentLevelId = levelId;
                GameApp.ChangeProcedure<ProcedureBattle>();
                LogToConsole($"切换关卡 {levelId}");
            };

            _commands["restart"] = args =>
            {
                GameApp.ChangeProcedure<ProcedureBattle>();
                LogToConsole("重新开始当前关卡");
            };

            _commands["reload"] = args =>
            {
                ConfigSystem.Instance.Reload();
                WeaponConfigMgr.Instance.Reload();
                LevelConfigMgr.Instance.Reload();
                LogToConsole("配置已重载");
            };

            _commands["clear"] = args =>
            {
                _logs.Clear();
            };
        }

        #endregion

        #region 辅助方法

        private void RunCommand(string cmd, params string[] args)
        {
            if (_commands.TryGetValue(cmd, out var action))
            {
                try
                {
                    action(args);
                }
                catch (Exception e)
                {
                    LogToConsole($"[Error] {e.Message}");
                }
            }
        }

        private async UniTask SpawnEnemyAsync(int enemyId, Vector3 position)
        {
            var go = await GameModule.Resource.LoadGameObjectAsync("Enemy", null);
            if (go == null)
            {
                LogToConsole("[Error] 加载 Enemy Prefab 失败");
                return;
            }

            go.transform.position = position;
            var enemy = go.GetComponent<EnemyEntity>();
            if (enemy == null) enemy = go.AddComponent<EnemyEntity>();

            var enemyCfg = ConfigSystem.Instance.Tables.TbEnemy.GetOrDefault(enemyId);
            enemy.Initialize(
                enemyId,
                enemyCfg?.MaxHp ?? 50,
                enemyCfg?.MoveSpeed ?? 2f,
                enemyCfg?.AttackDamage ?? 5,
                enemyCfg?.AttackRange ?? 1.2f,
                enemyCfg?.AttackInterval ?? 0.5f);

            GameEvent.Get<IEnemyEvent>().OnEnemySpawned(enemy.GetInstanceID(), enemyId);
            LogToConsole($"生成敌人 {enemyId} 在 {position}");
        }

        private void LogToConsole(string message)
        {
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_logs.Count > 100) _logs.RemoveAt(0);
            _logScrollPosition.y = float.MaxValue;
        }

        private bool TryParseInt(string[] args, int index, out int value)
        {
            value = 0;
            if (args == null || args.Length <= index || !int.TryParse(args[index], out value))
            {
                LogToConsole($"[Error] 参数错误，需要整数: {string.Join(" ", args ?? Array.Empty<string>())}");
                return false;
            }
            return true;
        }

        private bool TryParseFloat(string[] args, int index, out float value)
        {
            value = 0;
            if (args == null || args.Length <= index || !float.TryParse(args[index], out value))
            {
                LogToConsole($"[Error] 参数错误，需要浮点数: {string.Join(" ", args ?? Array.Empty<string>())}");
                return false;
            }
            return true;
        }

        #endregion
    }
}

#endif
