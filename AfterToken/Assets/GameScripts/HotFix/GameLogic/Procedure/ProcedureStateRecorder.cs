using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 用于在程序集重载（Domain Reload / Hotfix Reload）后恢复之前的流程。
    /// 数据保存在 PlayerPrefs 中，跨 Domain Reload 不会丢失。
    /// </summary>
    public static class ProcedureStateRecorder
    {
        private const string LAST_PROCEDURE_KEY = "ProcedureStateRecorder.LastProcedure";

        /// <summary>
        /// 记录当前流程名称。
        /// </summary>
        public static void Record(string procedureName)
        {
            PlayerPrefs.SetString(LAST_PROCEDURE_KEY, procedureName);
        }

        /// <summary>
        /// 读取上一次记录的流程名称。
        /// </summary>
        public static string GetLastProcedure()
        {
            return PlayerPrefs.GetString(LAST_PROCEDURE_KEY, null);
        }

        /// <summary>
        /// 清除记录。
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(LAST_PROCEDURE_KEY);
        }
    }
}
