using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 用于在程序集重载（Domain Reload / Hotfix Reload）后恢复之前的流程。
    /// 数据保存在 PlayerPrefs 中，跨 Domain Reload 不会丢失。
    /// </summary>
    public static class ProcedureStateRecorder
    {
        private const string LastProcedureKey = "ProcedureStateRecorder.LastProcedure";

        /// <summary>
        /// 记录当前流程名称。
        /// </summary>
        public static void Record(string procedureName)
        {
            PlayerPrefs.SetString(LastProcedureKey, procedureName);
        }

        /// <summary>
        /// 读取上一次记录的流程名称。
        /// </summary>
        public static string GetLastProcedure()
        {
            return PlayerPrefs.GetString(LastProcedureKey, null);
        }

        /// <summary>
        /// 清除记录。
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(LastProcedureKey);
        }
    }
}
