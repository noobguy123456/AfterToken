using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 存档选择界面：3 个竖矩形槽位，显示人物预览 + 等级（左上）+ 货币（人物下方）。
    /// 空槽位显示 Empty/New Game。点击槽位即切换存档（SaveSystem.SwitchSlot）。
    /// 人物预览：隐藏 PreviewRig（Player prefab + 专用相机）渲染到共享 RenderTexture，三个槽位共用。
    /// </summary>
    [Window(UILayer.Top, location: "SaveSlotSelectUI", fullScreen: false)]
    public class SaveSlotSelectUI : UIWindow
    {
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private Button _closeButton;
        private readonly Button[] _slotButtons = new Button[SaveSystem.SlotCount];
        private readonly Image[] _slotBgs = new Image[SaveSystem.SlotCount];
        private readonly RawImage[] _characterImages = new RawImage[SaveSystem.SlotCount];
        private readonly TextMeshProUGUI[] _levelTexts = new TextMeshProUGUI[SaveSystem.SlotCount];
        private readonly TextMeshProUGUI[] _currencyTexts = new TextMeshProUGUI[SaveSystem.SlotCount];
        private readonly TextMeshProUGUI[] _emptyTexts = new TextMeshProUGUI[SaveSystem.SlotCount];
        private readonly TextMeshProUGUI[] _slotNameTexts = new TextMeshProUGUI[SaveSystem.SlotCount];

        private static readonly Color SlotBgNormal = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        private static readonly Color SlotBgCurrent = new Color(0.15f, 0.35f, 0.2f, 0.95f);

        // ---- 人物预览 ----
        private GameObject _previewRig;
        private RenderTexture _previewRt;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _closeButton = FindChildComponent<Button>("m_btn_Close");
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                string root = $"m_rect_SlotRoot/m_rect_Slot{i + 1}";
                _slotButtons[i] = FindChildComponent<Button>($"{root}/m_img_SlotBg");
                _slotBgs[i] = FindChildComponent<Image>($"{root}/m_img_SlotBg");
                _characterImages[i] = FindChildComponent<RawImage>($"{root}/m_img_SlotBg/m_raw_Character");
                _levelTexts[i] = FindChildComponent<TextMeshProUGUI>($"{root}/m_img_SlotBg/m_text_Level");
                _currencyTexts[i] = FindChildComponent<TextMeshProUGUI>($"{root}/m_img_SlotBg/m_text_Currency");
                _emptyTexts[i] = FindChildComponent<TextMeshProUGUI>($"{root}/m_img_SlotBg/m_text_Empty");
                _slotNameTexts[i] = FindChildComponent<TextMeshProUGUI>($"{root}/m_img_SlotBg/m_text_SlotName");
            }
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_text_Title"), "ui.saveslot.title");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_btn_Close/m_text_Close"), "ui.common.close");

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<SaveSlotSelectUI>());
            }
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                if (_slotButtons[i] == null) continue;
                int slot = i + 1;
                _slotButtons[i].onClick.RemoveAllListeners();
                _slotButtons[i].onClick.AddListener(() => OnSlotSelected(slot));
            }

            SetupPreview();
            RefreshSlots();
            LocalizationSystem.Instance.OnLanguageChanged += RefreshSlots;
        }

        protected override void OnDestroy()
        {
            LocalizationSystem.Instance.OnLanguageChanged -= RefreshSlots;
            CleanupPreview();
            base.OnDestroy();
        }

        /// <summary>
        /// 点击槽位：选中存档并直接进入游戏（符合"点存档即开局"的操作直觉）。
        /// </summary>
        private void OnSlotSelected(int slot)
        {
            SaveSystem.SwitchSlot(slot);
            GameModule.UI.CloseUI<SaveSlotSelectUI>();
            // 与主菜单 Start 一致：直接进入基地（模拟经营场景）
            GameApp.ChangeProcedure<ProcedureSimulation>();
        }

        /// <summary>
        /// 全量刷新三个槽位的显示。
        /// </summary>
        private void RefreshSlots()
        {
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                int slot = i + 1;
                var summary = SaveSystem.GetSlotSummary(slot);
                bool isCurrent = slot == SaveSystem.CurrentSlot;

                if (_slotBgs[i] != null)
                {
                    _slotBgs[i].color = isCurrent ? SlotBgCurrent : SlotBgNormal;
                }
                if (_slotNameTexts[i] != null)
                {
                    _slotNameTexts[i].text = (isCurrent ? "▶ " : "") + Loc.Get("ui.saveslot.slot_n", slot);
                }

                bool hasData = summary.exists;
                if (_characterImages[i] != null)
                {
                    // 空槽位还没有人物，不显示预览模型
                    _characterImages[i].gameObject.SetActive(hasData);
                }
                if (_levelTexts[i] != null)
                {
                    _levelTexts[i].gameObject.SetActive(hasData);
                    if (hasData)
                    {
                        _levelTexts[i].text = Loc.Get("ui.saveslot.level", summary.level);
                    }
                }
                if (_currencyTexts[i] != null)
                {
                    _currencyTexts[i].gameObject.SetActive(hasData);
                    if (hasData)
                    {
                        _currencyTexts[i].text =
                            $"{Loc.Get("ui.saveslot.gold")}: {summary.gold}\n{Loc.Get("ui.saveslot.diamond")}: {summary.diamond}";
                    }
                }
                if (_emptyTexts[i] != null)
                {
                    _emptyTexts[i].gameObject.SetActive(!hasData);
                    if (!hasData)
                    {
                        _emptyTexts[i].text = $"{Loc.Get("ui.saveslot.empty")}\n\n{Loc.Get("ui.saveslot.new_game")}";
                    }
                }
            }
        }

        // ---- 人物预览（共享 RenderTexture） ----

        private void SetupPreview()
        {
            _previewRt = new RenderTexture(256, 360, 16);
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                if (_characterImages[i] != null)
                {
                    _characterImages[i].texture = _previewRt;
                }
            }

            _previewRig = new GameObject("SaveSlotPreviewRig");
            _previewRig.transform.position = new Vector3(0f, -2000f, 0f);

            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(_previewRig.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(_previewRig.transform);
            camGo.transform.localPosition = new Vector3(0f, 0.9f, -3.2f);
            camGo.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.11f, 0.14f, 1f);
            cam.fieldOfView = 40f;
            cam.targetTexture = _previewRt;
            cam.cullingMask = ~0;

            LoadPreviewModel(gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid LoadPreviewModel(CancellationToken cancellationToken)
        {
            var model = await GameModule.Resource.LoadGameObjectAsync("Player", _previewRig.transform, cancellationToken);
            if (model == null)
            {
                Log.Warning("[SaveSlotSelectUI] Player prefab 加载失败，预览区留空");
                return;
            }
            // 本窗口可能已关闭（资源异步返回晚于销毁）
            if (_previewRig == null)
            {
                Object.Destroy(model);
                return;
            }
            model.transform.localPosition = Vector3.zero;
            // 面向相机
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void CleanupPreview()
        {
            if (_previewRig != null)
            {
                Object.Destroy(_previewRig);
                _previewRig = null;
            }
            if (_previewRt != null)
            {
                // 先释放 GPU 显存，再销毁托管对象，确保彻底释放
                _previewRt.Release();
                Object.Destroy(_previewRt);
                _previewRt = null;
            }
        }
    }
}
