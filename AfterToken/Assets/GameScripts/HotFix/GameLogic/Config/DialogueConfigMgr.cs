using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 对话配置管理器（TbDialogue / TbDialogueNode 包装）。
    /// </summary>
    public class DialogueConfigMgr
    {
        private static DialogueConfigMgr _instance;
        public static DialogueConfigMgr Instance => _instance ??= new DialogueConfigMgr();

        /// <summary>
        /// 获取对话头，不存在时返回 null。
        /// </summary>
        public Dialogue Get(int dialogueId)
        {
            return ConfigSystem.Instance.Tables.TbDialogue.GetOrDefault(dialogueId);
        }

        /// <summary>
        /// 获取节点，不存在时返回 null。
        /// </summary>
        public DialogueNode GetNode(int nodeId)
        {
            return ConfigSystem.Instance.Tables.TbDialogueNode.GetOrDefault(nodeId);
        }

        /// <summary>
        /// 获取对话的起始节点：从 startNode 起返回第一个满足条件的节点，都不满足返回 null。
        /// </summary>
        public DialogueNode GetStartNode(int dialogueId)
        {
            var cfg = Get(dialogueId);
            if (cfg == null) return null;

            var node = GetNode(cfg.StartNode);
            int guard = 0;
            while (node != null && guard++ < 1000)
            {
                if (NarrativeCondition.Evaluate(node.Condition))
                {
                    return node;
                }
                node = node.Next != 0 ? GetNode(node.Next) : null;
            }
            return null;
        }
    }
}
