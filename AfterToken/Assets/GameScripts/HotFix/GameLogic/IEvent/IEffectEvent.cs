using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 特效相关事件接口。玩法系统只发事件，由 EffectSystem 订阅执行。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IEffectEvent
    {
        /// <summary>
        /// 请求在指定位置播放一次性特效。address 为 YooAsset 资源定位名，见 EffectIds。
        /// </summary>
        void OnPlayEffect(string address, Vector3 pos, Quaternion rot, EffectContext ctx);
    }
}
