using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// GTA5 风格武器轮盘 UI。
    /// </summary>
    [Window(UILayer.Top, location: "WeaponWheelUI", fullScreen: true)]
    public class WeaponWheelUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Transform _wheelRoot;
        private Image[] _slotIcons = new Image[WeaponSystem.MAX_WEAPON_SLOTS];
        private TextMeshProUGUI[] _slotLabels = new TextMeshProUGUI[WeaponSystem.MAX_WEAPON_SLOTS];
        private Image _highlight;

        protected override void ScriptGenerator()
        {
            _wheelRoot = FindChild("m_rect_WheelRoot");
            for (int i = 0; i < WeaponSystem.MAX_WEAPON_SLOTS; i++)
            {
                _slotIcons[i] = FindChildComponent<Image>($"m_rect_WheelRoot/m_img_Slot_{i}");
                _slotLabels[i] = FindChildComponent<TextMeshProUGUI>($"m_rect_WheelRoot/m_img_Slot_{i}/m_text_Label");
            }
            _highlight = FindChildComponent<Image>("m_rect_WheelRoot/m_img_Highlight");
        }
        #endregion

        private int _selectedSlot = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            RefreshAllSlots();
            Log.Debug($"[WeaponWheelUI] 节点绑定: Root={_wheelRoot != null}, Highlight={_highlight != null}");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdateSelection();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < WeaponSystem.MAX_WEAPON_SLOTS; i++)
            {
                RefreshSlot(i);
            }
        }

        private void RefreshSlot(int slot)
        {
            if (slot < 0 || slot >= WeaponSystem.MAX_WEAPON_SLOTS) return;
            var weapon = WeaponSystem.Instance?._slots[slot];
            if (_slotLabels[slot] != null)
            {
                _slotLabels[slot].text = weapon != null ? weapon.Config.name : "Empty";
            }
            if (_slotIcons[slot] != null)
            {
                _slotIcons[slot].color = weapon != null ? Color.white : Color.gray;
            }
        }

        private void UpdateSelection()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (mousePos - center).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;

            if (_highlight != null)
            {
                _highlight.rectTransform.rotation = Quaternion.Euler(0, 0, angle - 60f);
            }

            if (angle < 120f) _selectedSlot = 0;
            else if (angle < 240f) _selectedSlot = 1;
            else _selectedSlot = 2;
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        public int GetSelectedSlot() => _selectedSlot;
    }
}
