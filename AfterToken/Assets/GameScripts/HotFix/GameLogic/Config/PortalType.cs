namespace GameLogic
{
    /// <summary>
    /// 传送门类型常量。
    /// </summary>
    public static class PortalType
    {
        /// <summary>
        /// 撤离回基地（模拟经营场景即据点，游戏无"大厅"概念）。
        /// </summary>
        public const string RETURN_BASE = "portal_return_base";
        public const string NEXT_LEVEL = "portal_next_level";
        public const string CUSTOM_SCENE = "portal_custom_scene";
        /// <summary>
        /// 选关传送门（基地内）：不切场景，交互时打开选关窗口（LobbyUI）。
        /// </summary>
        public const string SELECT_LEVEL = "portal_select_level";
    }
}
