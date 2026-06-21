using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GameLogic.Editor
{
    /// <summary>
    /// 战斗场景与资源初始化工具。
    /// 以“非破坏性”方式创建/补齐缺少的资源：已存在的 Prefab 会被增量更新，不会被删除重建。
    /// </summary>
    public static class BattleSceneSetup
    {
        [MenuItem("Battle/Setup Battle Scene & Resources")]
        public static void Setup()
        {
            CreateDirectories();

            // 移除旧 Resources 路径下的 UI Prefab，避免与 AssetRaw 热更资源地址冲突。
            AssetDatabase.DeleteAsset("Assets/Resources/WeaponWheelUI.prefab");
            AssetDatabase.DeleteAsset("Assets/Resources/SniperScopeUI.prefab");

            CreatePlayerPrefab();
            CreateProjectilePrefab();
            CreateEnemyPrefab();
            CreateBattleMainUIPrefab();
            CreateDamageNumberUIPrefab();
            CreateHitFeedbackUIPrefab();
            CreateSniperScopePrefab();
            CreateWeaponWheelPrefab();
            CreateMainMenuUIPrefab();
            CreateLobbyUIPrefab();
            CreateLoadingUIPrefab();
            CreateMainMenuScene();
            CreateLobbyScene();
            CreateBattleScene();
            CreateBattleSceneL01();
            CreateRenderTextureAsset();
            AssetDatabase.Refresh();

            // 执行 YooAsset 模拟构建，确保编辑器下可加载新资源。
            try
            {
                var result = YooAsset.EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                if (result != null)
                {
                    Debug.Log($"[BattleSceneSetup] SimulateBuild 完成: {result.PackageRootDirectory}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BattleSceneSetup] SimulateBuild 失败: {e.Message}");
            }

            Debug.Log("[BattleSceneSetup] 战斗场景与资源初始化完成");
        }

        #region 目录

        private static void CreateDirectories()
        {
            EnsureDirectory("Assets/AssetRaw/Prefabs");
            EnsureDirectory("Assets/AssetRaw/UI");
            EnsureDirectory("Assets/AssetRaw/UI/MainMenuUI");
            EnsureDirectory("Assets/AssetRaw/UI/LobbyUI");
            EnsureDirectory("Assets/AssetRaw/UI/LoadingUI");
            EnsureDirectory("Assets/AssetRaw/UI/DamageNumberUI");
            EnsureDirectory("Assets/AssetRaw/UI/HitFeedbackUI");
            EnsureDirectory("Assets/AssetRaw/UI/WeaponWheelUI");
            EnsureDirectory("Assets/AssetRaw/UI/SniperScopeUI");
            EnsureDirectory("Assets/AssetRaw/Textures");
            EnsureDirectory("Assets/AssetRaw/Scenes");
            EnsureDirectory("Assets/Resources");
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                var folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        #endregion

        #region Prefab 通用工具

        /// <summary>
        /// 创建或增量更新 Prefab。已存在的 Prefab 会被实例化修改后保存，不会被删除重建。
        /// </summary>
        private static void EnsureOrUpdatePrefab(string assetPath, Action<GameObject> buildAction)
        {
            var folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            EnsureDirectory(folder);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject go;
            if (source != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            }
            else
            {
                go = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            }

            buildAction(go);
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static GameObject EnsureChild(GameObject parent, string name)
        {
            var child = parent.transform.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void DestroyAllChildren(GameObject parent)
        {
            while (parent.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
            }
        }

        private static T EnsureComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private const string TMPFontAssetPath = "Assets/AssetRaw/Fonts/MainUIFont.asset";

        private static TMP_FontAsset GetUIFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMPFontAssetPath);
            if (fontAsset == null)
            {
                try
                {
                    fontAsset = TMP_Settings.defaultFontAsset;
                }
                catch
                {
                    fontAsset = null;
                }
            }
            return fontAsset;
        }

        private static void SetupFullScreenRect(RectTransform rt)
        {
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
        }

        private static void SetupText(TextMeshProUGUI text, string content, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            text.text = content;
            text.font = GetUIFontAsset();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
        }

        #endregion

        #region 角色/子弹/敌人 Prefab

        private static void CreatePlayerPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/Prefabs/Player.prefab", go =>
            {
                go.tag = "Player";
                go.layer = LayerMask.NameToLayer("Player");

                var rb = go.EnsureComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.freezeRotation = true;

                var col = go.EnsureComponent<CircleCollider2D>();
                col.radius = 0.3f;

                var sr = go.EnsureComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = Color.cyan;
                sr.sortingOrder = 5;

                go.EnsureComponent<PlayerEntity>();
            });
        }

        private static void CreateProjectilePrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/Prefabs/Projectile_Normal.prefab", go =>
            {
                go.layer = LayerMask.NameToLayer("Projectile");

                var sr = go.EnsureComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = Color.yellow;
                sr.sortingOrder = 10;

                go.EnsureComponent<ProjectileEntity>();

                var col = go.EnsureComponent<CircleCollider2D>();
                col.radius = 0.1f;
                col.isTrigger = true;

                var rb = go.EnsureComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            });
        }

        private static void CreateEnemyPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/Prefabs/Enemy.prefab", go =>
            {
                go.tag = "Enemy";
                go.layer = LayerMask.NameToLayer("Enemy");

                var rb = go.EnsureComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.bodyType = RigidbodyType2D.Kinematic;

                var col = go.EnsureComponent<CircleCollider2D>();
                col.radius = 0.3f;

                var sr = go.EnsureComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = Color.red;
                sr.sortingOrder = 5;

                go.EnsureComponent<EnemyEntity>();
            });
        }

        #endregion

        #region 战斗/UI Prefab

        private static void CreateBattleMainUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/BattleMainUI/BattleMainUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                go.EnsureComponent<GraphicRaycaster>();

                // 清除旧版 Demo UI 遗留节点，只保留当前 HUD 需要的结构。
                DestroyAllChildren(go);
                var scaler = go.GetComponent<CanvasScaler>();
                if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);

                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // HUD：武器信息（HP / Ammo / Weapon）
                var hudRoot = EnsureChild(go, "m_rect_HudRoot");
                var hudRect = hudRoot.EnsureComponent<RectTransform>();
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.offsetMin = Vector2.zero;
                hudRect.offsetMax = Vector2.zero;

                var hp = EnsureChild(hudRoot, "m_text_Hp");
                var hpRect = hp.EnsureComponent<RectTransform>();
                hpRect.anchorMin = new Vector2(0, 1);
                hpRect.anchorMax = new Vector2(0, 1);
                hpRect.pivot = new Vector2(0, 1);
                hpRect.anchoredPosition = new Vector2(20, -20);
                hpRect.sizeDelta = new Vector2(300, 40);
                SetupText(hp.EnsureComponent<TextMeshProUGUI>(), "HP: -/-", 24, Color.white, TextAlignmentOptions.TopLeft);

                var ammo = EnsureChild(hudRoot, "m_text_Ammo");
                var ammoRect = ammo.EnsureComponent<RectTransform>();
                ammoRect.anchorMin = new Vector2(0, 1);
                ammoRect.anchorMax = new Vector2(0, 1);
                ammoRect.pivot = new Vector2(0, 1);
                ammoRect.anchoredPosition = new Vector2(20, -70);
                ammoRect.sizeDelta = new Vector2(300, 40);
                SetupText(ammo.EnsureComponent<TextMeshProUGUI>(), "Ammo: -/-", 24, Color.white, TextAlignmentOptions.TopLeft);

                var weapon = EnsureChild(hudRoot, "m_text_Weapon");
                var weaponRect = weapon.EnsureComponent<RectTransform>();
                weaponRect.anchorMin = new Vector2(0, 1);
                weaponRect.anchorMax = new Vector2(0, 1);
                weaponRect.pivot = new Vector2(0, 1);
                weaponRect.anchoredPosition = new Vector2(20, -120);
                weaponRect.sizeDelta = new Vector2(300, 40);
                SetupText(weapon.EnsureComponent<TextMeshProUGUI>(), "Weapon: -", 24, Color.white, TextAlignmentOptions.TopLeft);

                // 准心（静态，命中反馈由 HitFeedbackSystem 动态处理）
                var crosshair = EnsureChild(go, "m_rect_Crosshair");
                var crossRect = crosshair.EnsureComponent<RectTransform>();
                crossRect.anchorMin = new Vector2(0.5f, 0.5f);
                crossRect.anchorMax = new Vector2(0.5f, 0.5f);
                crossRect.pivot = new Vector2(0.5f, 0.5f);
                crossRect.anchoredPosition = Vector2.zero;
                crossRect.sizeDelta = new Vector2(16, 16);
                var crossImg = crosshair.EnsureComponent<Image>();
                crossImg.color = Color.white;
                crossImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            });
        }

        private static void CreateDamageNumberUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/DamageNumberUI/DamageNumberUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 15;
                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // 飘字模板
                var template = EnsureChild(go, "m_text_Template");
                var templateRect = template.EnsureComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0.5f, 0.5f);
                templateRect.anchorMax = new Vector2(0.5f, 0.5f);
                templateRect.pivot = new Vector2(0.5f, 0.5f);
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = new Vector2(120, 40);
                SetupText(template.EnsureComponent<TextMeshProUGUI>(), "0", 24, Color.white, TextAlignmentOptions.Center);
            });
        }

        private static void CreateHitFeedbackUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/HitFeedbackUI/HitFeedbackUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 110;
                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                var root = EnsureChild(go, "m_rect_IndicatorRoot");
                var rootRect = root.EnsureComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                // 8 方向指示器：N, NE, E, SE, S, SW, W, NW
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    var indicator = EnsureChild(root, $"m_img_Indicator_{i}");
                    var rt = indicator.EnsureComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(48, 48);
                    rt.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * 120f;
                    rt.rotation = Quaternion.Euler(0, 0, -angle);

                    var img = indicator.EnsureComponent<Image>();
                    img.color = new Color(1, 0, 0, 0);
                }

                // 命中标记
                var marker = EnsureChild(root, "m_img_HitMarker");
                var markerRect = marker.EnsureComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.anchoredPosition = Vector2.zero;
                markerRect.sizeDelta = new Vector2(32, 32);
                var markerImg = marker.EnsureComponent<Image>();
                markerImg.color = new Color(1, 0, 0, 0);
            });
        }

        private static void CreateSniperScopePrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/SniperScopeUI/SniperScopeUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = go.EnsureComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // 黑色遮罩（运行时生成圆形 mask sprite）
                var vignette = EnsureChild(go, "m_img_Vignette");
                var vignetteRect = vignette.EnsureComponent<RectTransform>();
                vignetteRect.anchorMin = Vector2.zero;
                vignetteRect.anchorMax = Vector2.one;
                vignetteRect.offsetMin = Vector2.zero;
                vignetteRect.offsetMax = Vector2.zero;
                var vignetteImg = vignette.EnsureComponent<Image>();
                vignetteImg.color = Color.white;

                // 狙击镜画面
                var scope = EnsureChild(go, "m_raw_Scope");
                var scopeRect = scope.EnsureComponent<RectTransform>();
                scopeRect.anchorMin = new Vector2(0.5f, 0.5f);
                scopeRect.anchorMax = new Vector2(0.5f, 0.5f);
                scopeRect.pivot = new Vector2(0.5f, 0.5f);
                scopeRect.anchoredPosition = Vector2.zero;
                scopeRect.sizeDelta = new Vector2(512, 512);
                scope.EnsureComponent<RawImage>();
            });
        }

        private static void CreateWeaponWheelPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/WeaponWheelUI/WeaponWheelUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;

                var scaler = go.EnsureComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // 轮盘根节点
                var wheelRoot = EnsureChild(go, "m_rect_WheelRoot");
                var wheelRect = wheelRoot.EnsureComponent<RectTransform>();
                wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
                wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
                wheelRect.pivot = new Vector2(0.5f, 0.5f);
                wheelRect.anchoredPosition = Vector2.zero;
                wheelRect.sizeDelta = Vector2.zero;

                // 背景圆
                var bg = EnsureChild(wheelRoot, "m_img_Background");
                var bgRect = bg.EnsureComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0.5f, 0.5f);
                bgRect.anchorMax = new Vector2(0.5f, 0.5f);
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.anchoredPosition = Vector2.zero;
                bgRect.sizeDelta = new Vector2(360, 360);
                var bgImg = bg.EnsureComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.7f);

                // 3 个武器槽位
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 100f;

                    var slot = EnsureChild(wheelRoot, $"m_img_Slot_{i}");
                    var slotRect = slot.EnsureComponent<RectTransform>();
                    slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                    slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                    slotRect.pivot = new Vector2(0.5f, 0.5f);
                    slotRect.anchoredPosition = pos;
                    slotRect.sizeDelta = new Vector2(80, 80);
                    var slotImg = slot.EnsureComponent<Image>();
                    slotImg.color = Color.gray;

                    var label = EnsureChild(slot, "m_text_Label");
                    var labelRect = label.EnsureComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                    SetupText(label.EnsureComponent<TextMeshProUGUI>(), "Empty", 14, Color.white, TextAlignmentOptions.Center);
                }

                // 高亮扇区
                var highlight = EnsureChild(wheelRoot, "m_img_Highlight");
                var highlightRect = highlight.EnsureComponent<RectTransform>();
                highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
                highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
                highlightRect.pivot = new Vector2(0.5f, 0.5f);
                highlightRect.anchoredPosition = Vector2.zero;
                highlightRect.sizeDelta = new Vector2(120, 120);
                var highlightImg = highlight.EnsureComponent<Image>();
                highlightImg.color = new Color(1, 1, 1, 0.3f);
            });
        }

        private static void CreateMainMenuUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/MainMenuUI/MainMenuUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                // 嵌套在 UIRoot/UICanvas 下，不添加 CanvasScaler，避免驱动 RectTransform 缩放到 0。
                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // 标题
                var titleGo = EnsureChild(go, "m_text_Title");
                var titleRect = titleGo.EnsureComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0, -120);
                titleRect.sizeDelta = new Vector2(800, 120);
                SetupText(titleGo.EnsureComponent<TextMeshProUGUI>(), "AfterToken", 80, Color.white, TextAlignmentOptions.Center);

                // 按钮根节点
                var btnRoot = EnsureChild(go, "m_rect_ButtonRoot");
                var btnRootRect = btnRoot.EnsureComponent<RectTransform>();
                btnRootRect.anchorMin = new Vector2(0.5f, 0.5f);
                btnRootRect.anchorMax = new Vector2(0.5f, 0.5f);
                btnRootRect.anchoredPosition = Vector2.zero;
                btnRootRect.sizeDelta = new Vector2(400, 300);

                var layout = btnRoot.EnsureComponent<VerticalLayoutGroup>();
                layout.spacing = 20;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                EnsureButton(btnRoot, "m_btn_Start", "Start Game", new Color(0.2f, 0.6f, 1f));
                EnsureButton(btnRoot, "m_btn_Exit", "Exit Game", new Color(0.2f, 0.6f, 1f));
            });
        }

        private static void CreateLobbyUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/LobbyUI/LobbyUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // 标题
                var titleGo = EnsureChild(go, "m_text_Title");
                var titleRect = titleGo.EnsureComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0, -80);
                titleRect.sizeDelta = new Vector2(800, 80);
                SetupText(titleGo.EnsureComponent<TextMeshProUGUI>(), "Select Level", 56, Color.white, TextAlignmentOptions.Center);

                // 关卡按钮列表
                var listRoot = EnsureChild(go, "m_rect_LevelList");
                var listRect = listRoot.EnsureComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0.5f, 0.5f);
                listRect.anchorMax = new Vector2(0.5f, 0.5f);
                listRect.anchoredPosition = new Vector2(0, 20);
                listRect.sizeDelta = new Vector2(600, 400);

                var listLayout = listRoot.EnsureComponent<VerticalLayoutGroup>();
                listLayout.spacing = 20;
                listLayout.childAlignment = TextAnchor.MiddleCenter;
                listLayout.childControlWidth = false;
                listLayout.childControlHeight = false;
                listLayout.childForceExpandWidth = false;
                listLayout.childForceExpandHeight = false;

                // 关卡按钮模板（运行时克隆使用）
                var template = EnsureButton(listRoot, "m_btn_LevelTemplate", "Level Name", new Color(0.2f, 0.7f, 0.4f));
                template.gameObject.SetActive(false);

                // 返回主菜单
                var back = EnsureButton(go, "m_btn_Back", "Back to Menu", new Color(0.3f, 0.3f, 0.3f));
                var backRect = back.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(0f, 0f);
                backRect.pivot = new Vector2(0f, 0f);
                backRect.anchoredPosition = new Vector2(40, 40);
                backRect.sizeDelta = new Vector2(200, 60);
            });
        }

        private static void CreateLoadingUIPrefab()
        {
            EnsureOrUpdatePrefab("Assets/AssetRaw/UI/LoadingUI/LoadingUI.prefab", go =>
            {
                var canvas = go.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                go.EnsureComponent<GraphicRaycaster>();
                SetupFullScreenRect(go.EnsureComponent<RectTransform>());

                // LoadingUI 在代码中动态构建内容，这里只保证根节点结构正确。
                // 后续如需美术替换，可在此 Prefab 中预置节点并在 LoadingUI.cs 中绑定。
            });
        }

        /// <summary>
        /// 在 parent 下创建或更新一个命名按钮。返回 Button 组件。
        /// </summary>
        private static Button EnsureButton(GameObject parent, string name, string labelText, Color color)
        {
            var go = EnsureChild(parent, name);
            var rt = go.EnsureComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 60);

            var img = go.EnsureComponent<Image>();
            img.color = color;

            var btn = go.EnsureComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = EnsureChild(go, "Text");
            var labelRect = labelGo.EnsureComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            SetupText(labelGo.EnsureComponent<TextMeshProUGUI>(), labelText, 28, Color.white, TextAlignmentOptions.Center);

            return btn;
        }

        #endregion

        #region 场景

        private static void CreateMainMenuScene()
        {
            string path = "Assets/AssetRaw/Scenes/MainMenuScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateBasicSceneContent("MainMenuScene");
                EditorSceneManager.SaveScene(scene, path);
            }
            else
            {
                EnsureSceneHasMainCamera(path);
            }
        }

        private static void CreateLobbyScene()
        {
            string path = "Assets/AssetRaw/Scenes/LobbyScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateBasicSceneContent("LobbyScene");
                EditorSceneManager.SaveScene(scene, path);
            }
            else
            {
                EnsureSceneHasMainCamera(path);
            }
        }

        private static void EnsureSceneHasMainCamera(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var camGo = GameObject.FindWithTag("MainCamera");
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 8;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.transform.position = new Vector3(0, 0, -10);
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateBattleScene()
        {
            string path = "Assets/AssetRaw/Scenes/BattleScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0, 0, -10);

            // Light
            var lightGo = new GameObject("Global Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;

            // Ground
            var groundGo = new GameObject("Ground");
            groundGo.layer = LayerMask.NameToLayer("Obstacle");
            var groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            groundSr.color = new Color(0.2f, 0.25f, 0.2f);
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.size = new Vector2(40, 40);
            groundGo.transform.localScale = Vector3.one;
            var groundCol = groundGo.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(40, 40);

            // Spawn point
            var spawnGo = new GameObject("PlayerSpawnPoint");
            spawnGo.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateBattleSceneL01()
        {
            string sourcePath = "Assets/AssetRaw/Scenes/BattleScene.unity";
            string targetPath = "Assets/AssetRaw/Scenes/BattleScene_L01.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath) != null)
            {
                AssetDatabase.CopyAsset(sourcePath, targetPath);
            }
            else
            {
                // 如果 BattleScene 也不存在，先创建一个最小化的关卡 2 场景
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateBasicSceneContent("BattleScene_L01");
                EditorSceneManager.SaveScene(scene, targetPath);
            }
        }

        private static void CreateBasicSceneContent(string sceneName)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0, 0, -10);

            var lightGo = new GameObject("Global Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;

            var spawnGo = new GameObject("PlayerSpawnPoint");
            spawnGo.transform.position = Vector3.zero;

            Debug.Log($"[BattleSceneSetup] 创建基础场景内容: {sceneName}");
        }

        #endregion

        #region 其他资源

        private static void CreateRenderTextureAsset()
        {
            string path = "Assets/AssetRaw/Textures/SniperScopeRT.renderTexture";
            if (AssetDatabase.LoadAssetAtPath<RenderTexture>(path) != null) return;

            var rt = new RenderTexture(512, 512, 16);
            rt.Create();
            AssetDatabase.CreateAsset(rt, path);
        }

        #endregion
    }
}
