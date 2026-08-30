using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 任务事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IQuestEvent
    {
        /// <summary>任务被接取。</summary>
        void OnQuestAccepted(int questId);

        /// <summary>目标进度变化。</summary>
        void OnObjectiveProgress(int questId, int objectiveId, int current, int need);

        /// <summary>全部目标达成，任务可交付。</summary>
        void OnQuestReadyToTurnIn(int questId);

        /// <summary>任务交付完成（奖励已发放）。</summary>
        void OnQuestCompleted(int questId);
    }
}
