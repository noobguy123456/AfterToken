using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 道具格子点击事件转发器。
    /// 挂载在 ItemSlot 根节点上，把点击转发给所属的 ItemSlotWidget。
    /// 仅响应左键（右键保留给后续的 使用/丢弃 等操作）。
    /// </summary>
    public class ItemSlotClickHandler : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// 点击回调（由 ItemSlotWidget 注册）。
        /// </summary>
        public System.Action OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnClicked?.Invoke();
            }
        }
    }
}
