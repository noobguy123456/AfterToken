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
        public static CursorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CursorManager();
                    Application.focusChanged += _instance.OnApplicationFocusChanged;
                }

                return _instance;
            }
        }

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
        /// 释放光标管理器占用的资源。应在游戏退出时调用。
        /// </summary>
        public static void Release()
        {
            if (_instance != null)
            {
                Application.focusChanged -= _instance.OnApplicationFocusChanged;
                _instance._applyCts?.Cancel();
                _instance._applyCts?.Dispose();
            }

            _instance = null;
        }

        /// <summary>
        /// 设置默认光标纹理与热点（左上角为原点）。
        /// </summary>
        public void SetDefaultCursor(Texture2D texture, Vector2 hotSpot)
        {
            _defaultCursorTexture = texture;
            _defaultHotSpot = hotSpot;
            ApplyCursorState().Forget();
        }

        /// <summary>
        /// 设置当前光标纹理与热点。
        /// 传入 null 则恢复默认光标。
        /// </summary>
        public void SetCursor(Texture2D texture, Vector2 hotSpot)
        {
            _currentCursorTexture = texture;
            _currentHotSpot = hotSpot;
            ApplyCursorState().Forget();
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
                ApplyCursorState().Forget();
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
                ApplyCursorState().Forget();
            }
        }

        /// <summary>
        /// 强制隐藏光标并重置引用计数。
        /// 适用于流程切换等需要强制恢复战斗状态的场景。
        /// </summary>
        public void ForceHideCursor()
        {
            _showRefCount = 0;
            ApplyCursorState().Forget();
        }

        /// <summary>
        /// 强制显示光标并重置引用计数为 1。
        /// 适用于流程切换等需要强制恢复菜单状态的场景。
        /// </summary>
        public void ForceShowCursor()
        {
            _showRefCount = 1;
            ApplyCursorState().Forget();
        }

        /// <summary>
        /// 设置锁定模式。
        /// </summary>
        public void SetLockMode(GameCursorLockMode mode)
        {
            _currentMode = mode;
            ApplyCursorState().Forget();
        }

        /// <summary>
        /// 当游戏窗口重新获得焦点时，若当前应处于战斗状态（隐藏 + 锁定），
        /// 重新应用光标状态，解决从 UI 返回战斗后光标未立即隐藏的问题。
        /// </summary>
        private void OnApplicationFocusChanged(bool focused)
        {
            if (focused)
            {
                ApplyCursorState().Forget();
            }
            else
            {
                // 切出游戏窗口时先解锁并显示光标，避免系统光标被锁在屏幕中心导致 Alt+Tab 操作困难。
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private async UniTaskVoid ApplyCursorState()
        {
            try
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

                    // Windows 下隐藏/锁定可能不会立即生效（窗口未捕获鼠标时设置会被忽略），
                    // 仅延迟一帧重试不足以覆盖该场景（已知问题：从设置/菜单返回战斗后光标仍可见）。
                    // Cursor.visible / lockState 读回的是 Unity 侧标志而非 OS 真实状态，无法据此判断是否生效，
                    // 因此在接下来约 0.5 秒内逐帧无条件重设，期间任何新的光标请求都会取消本次重试。
                    try
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            await UniTask.Yield(ct);

                            if (_showRefCount != 0)
                            {
                                // 已有新的显示请求，交由最新一次 ApplyCursorState 处理。
                                return;
                            }

                            Cursor.visible = false;
                            if (_currentMode == GameCursorLockMode.Locked)
                            {
                                Cursor.lockState = CursorLockMode.Locked;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }

            }
            catch (Exception e)
            {
                Log.Error($"[CursorManager] ApplyCursorState failed: {e}");
            }
        }
    }
}
