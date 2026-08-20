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
        private TextMeshProUGUI _statsText;
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
            _statsText = FindChildComponent<TextMeshProUGUI>("m_text_WeaponStats");
        }
        #endregion

        private int _selectedSlot = -1;
        private int _lastStatsSlot = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            RefreshAllSlots();
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
            var weapon = WeaponSystem.Instance?.GetWeaponInSlot(slot);
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

            // 悬停槽位变化时刷新武器属性面板
            if (_selectedSlot != _lastStatsSlot)
            {
                _lastStatsSlot = _selectedSlot;
                RefreshStatsText(_selectedSlot);
            }
        }

        /// <summary>
        /// 刷新悬停武器的属性面板（空槽位清空显示）。
        /// </summary>
        private void RefreshStatsText(int slot)
        {
            if (_statsText == null) return;

            var weapon = WeaponSystem.Instance?.GetWeaponInSlot(slot);
            if (weapon?.Config == null)
            {
                _statsText.text = string.Empty;
                return;
            }

            var cfg = weapon.Config;
            _statsText.text =
                $"{cfg.name}\n" +
                $"Damage: {cfg.damage:0.#}   Fire Rate: {cfg.fireRate:0.#}/s   Clip: {cfg.clipSize}\n" +
                $"Reload: {cfg.reloadTime:0.#}s   Range: {cfg.maxRange:0.#}m";
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        public int GetSelectedSlot() => _selectedSlot;
    }
}
