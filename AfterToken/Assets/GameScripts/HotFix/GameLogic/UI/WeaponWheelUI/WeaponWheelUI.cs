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

        // 轮盘选择的增量累积：轮盘期间系统光标保持锁定隐藏（不显示系统鼠标），
        // 用打开轮盘以来的鼠标位移矢量决定选中方向，准星位置不受任何影响。
        private Vector2 _wheelAccum;
        private const float WheelDeadZone = 20f;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            _selectedSlot = -1;
            _lastStatsSlot = -1;
            _wheelAccum = Vector2.zero;
            // 隐藏准星（冻结其位置），轮盘不借用准星也不显示系统鼠标
            CrosshairUpdater.Instance?.SetVisible(false);
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
            // 光标锁定下 Input.mousePosition 恒为中心点，不能用；改为累积鼠标位移。
            _wheelAccum.x += Input.GetAxis("Mouse X") * SensitivitySetting.Value;
            _wheelAccum.y += Input.GetAxis("Mouse Y") * SensitivitySetting.Value;

            // 死区内保持上次选择（未推满死区 = 不换武器，松开保持原武器）
            if (_wheelAccum.magnitude < WheelDeadZone)
            {
                return;
            }

            Vector2 dir = _wheelAccum.normalized;
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
            CrosshairUpdater.Instance?.SetVisible(true);
            base.OnDestroy();
        }

        public int GetSelectedSlot() => _selectedSlot;
    }
}
