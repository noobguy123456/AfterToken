using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 光标管理模式。
    /// </summary>
    public enum GameCursorLockMode
    {
        /// <summary>
        /// 自由光标：可见、不锁定。
        /// </summary>
        Free,

        /// <summary>
        /// 隐藏并锁定在屏幕中心（FPS/射击模式）。
        /// </summary>
        Locked,
    }

    /// <summary>
    /// 光标管理器。
    /// 负责在游戏界面隐藏系统光标，在打开操作 UI 时显示自定义光标。
    /// </summary>
    public class CursorManager
    {
        private static CursorManager _instance;
        public static CursorManager Instance => _instance ??= new CursorManager();

        private int _showRefCount;
        private GameCursorLockMode _currentMode = GameCursorLockMode.Free;

        private Texture2D _defaultCursorTexture;
        private Vector2 _defaultHotSpot;

        private Texture2D _currentCursorTexture;
        private Vector2 _currentHotSpot;

        private CancellationTokenSource _applyCts;

        public GameCursorLockMode CurrentMode => _currentMode;
        public bool IsCursorVisible => _showRefCount > 0;

        /// <summary>
        /// 设置默认光标纹理与热点（左上角为原点）。
        /// </summary>
        public void SetDefaultCursor(Texture2D texture, Vector2 hotSpot)
        {
            _defaultCursorTexture = texture;
            _defaultHotSpot = hotSpot;
            ApplyCursorState();
        }

        /// <summary>
        /// 设置当前光标纹理与热点。
        /// 传入 null 则恢复默认光标。
        /// </summary>
        public void SetCursor(Texture2D texture, Vector2 hotSpot)
        {
            _currentCursorTexture = texture;
            _currentHotSpot = hotSpot;
            ApplyCursorState();
        }

        /// <summary>
        /// 显示光标（引用计数 +1）。
        /// 当第一个调用者请求显示时，才真正显示系统光标。
        /// </summary>
        public void ShowCursor()
        {
            _showRefCount++;
            if (_showRefCount == 1)
            {
                ApplyCursorState();
                Log.Debug("[CursorManager] 显示光标");
            }
        }

        /// <summary>
        /// 隐藏光标（引用计数 -1）。
        /// 当所有请求者都释放后，才隐藏系统光标。
        /// </summary>
        public void HideCursor()
        {
            if (_showRefCount > 0)
            {
                _showRefCount--;
            }

            if (_showRefCount == 0)
            {
                ApplyCursorState();
                Log.Debug("[CursorManager] 隐藏光标");
            }
        }

        /// <summary>
        /// 强制隐藏光标并重置引用计数。
        /// 适用于流程切换等需要强制恢复战斗状态的场景。
        /// </summary>
        public void ForceHideCursor()
        {
            _showRefCount = 0;
            ApplyCursorState();
            Log.Debug("[CursorManager] 强制隐藏光标");
        }

        /// <summary>
        /// 强制显示光标并重置引用计数为 1。
        /// 适用于流程切换等需要强制恢复菜单状态的场景。
        /// </summary>
        public void ForceShowCursor()
        {
            _showRefCount = 1;
            ApplyCursorState();
            Log.Debug("[CursorManager] 强制显示光标");
        }

        /// <summary>
        /// 设置锁定模式。
        /// </summary>
        public void SetLockMode(GameCursorLockMode mode)
        {
            _currentMode = mode;
            ApplyCursorState();
        }

        private async void ApplyCursorState()
        {
            // 取消上一次的延迟应用，避免连续调用导致状态竞争。
            _applyCts?.Cancel();
            _applyCts?.Dispose();
            _applyCts = new CancellationTokenSource();
            var ct = _applyCts.Token;

            bool visible = _showRefCount > 0;

            if (visible)
            {
                // 从 Locked 切换到 None 时，Windows 需要一帧才能真正释放鼠标。
                // 先解锁并等待一帧，再显示光标，避免光标短暂锁死在屏幕中心。
                bool needYield = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = CursorLockMode.None;
                if (needYield)
                {
                    try
                    {
                        await UniTask.Yield(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }

                Cursor.visible = true;

                var texture = _currentCursorTexture ?? _defaultCursorTexture;
                var hotSpot = _currentCursorTexture != null ? _currentHotSpot : _defaultHotSpot;
                Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.visible = false;
                if (_currentMode == GameCursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                // Free 模式下保持当前 lockState（可能是 Locked），等显示时再由 visible 分支解锁并延迟一帧，
                // 避免 Windows 在不可见时提前解锁导致显示光标时仍锁在中心。
            }
        }
    }
}
