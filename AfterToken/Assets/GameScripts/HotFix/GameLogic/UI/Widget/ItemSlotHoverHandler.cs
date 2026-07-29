using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 道具格子悬浮事件转发器。
    /// 挂载在 ItemSlot 根节点上，把鼠标进入/离开转发给 ItemTooltipUI。
    /// </summary>
    public class ItemSlotHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 当前格子绑定的道具 ID（由 ItemSlotWidget.SetItem 写入）。
        /// </summary>
        public int ItemId;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemId > 0)
            {
                // 传格子自身的 RectTransform，提示窗锚定在格子左侧/右侧而非跟随鼠标
                ItemTooltipUI.ShowTooltip(ItemId, transform as RectTransform);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.HideTooltip();
        }
    }
}
