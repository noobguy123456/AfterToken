using UnityEngine;
using TMPro;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗主 UI（HUD）。
    /// 显示玩家 HP、弹药、当前武器信息。
    /// </summary>
    [Window(UILayer.UI, location: "BattleMainUI")]
    public class BattleMainUI : UIWindow
    {
        #region 脚本工具生成的代码
        private TextMeshProUGUI _textHp;
        private TextMeshProUGUI _textAmmo;
        private TextMeshProUGUI _textWeapon;
        private RectTransform _rectCrosshair;

        protected override void ScriptGenerator()
        {
            _textHp = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Hp");
            _textAmmo = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Ammo");
            _textWeapon = FindChildComponent<TextMeshProUGUI>("m_rect_HudRoot/m_text_Weapon");
            _rectCrosshair = FindChildComponent<RectTransform>("m_rect_Crosshair");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            RefreshAll();
            Log.Debug($"[BattleMainUI] 节点绑定: Hp={_textHp != null}, Ammo={_textAmmo != null}, Weapon={_textWeapon != null}, Crosshair={_rectCrosshair != null}");
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int, int>(IPlayerEvent_Event.OnHpChanged, OnHpChanged);
            AddUIEvent<int, int>(IPlayerEvent_Event.OnAmmoChanged, OnAmmoChanged);
            AddUIEvent<int, int>(IWeaponEvent_Event.OnWeaponSwitched, OnWeaponSwitched);
        }

        private void OnHpChanged(int currentHp, int maxHp)
        {
            if (_textHp != null) _textHp.text = $"HP: {currentHp}/{maxHp}";
        }

        private void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (_textAmmo != null) _textAmmo.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        }

        private void OnWeaponSwitched(int ownerId, int slot)
        {
            RefreshWeaponText();
        }

        private void RefreshAll()
        {
            if (_textHp != null) _textHp.text = "HP: -/-";
            if (_textAmmo != null) _textAmmo.text = "Ammo: -/-";
            RefreshWeaponText();
        }

        private void RefreshWeaponText()
        {
            if (_textWeapon == null) return;
            var weapon = WeaponSystem.Instance?.CurrentWeapon;
            _textWeapon.text = weapon != null ? $"Weapon: {weapon.Config.name}" : "Weapon: -";
        }
    }
}
