using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 对话事件接口。
    /// DialogueSystem 发送，DialogueUI 等呈现层订阅。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IDialogueEvent
    {
        /// <summary>对话开始。</summary>
        void OnDialogueStarted(int dialogueId, int npcId);

        /// <summary>推进到一行台词（UI 做打字机呈现）。</summary>
        void OnDialogueLine(int nodeId, string speaker, string text);

        /// <summary>出现选项组（options 与内部选项索引一一对应）。</summary>
        void OnDialogueChoices(int nodeId, string[] options);

        /// <summary>对话结束（含被打断）。</summary>
        void OnDialogueEnded(int dialogueId);
    }
}
