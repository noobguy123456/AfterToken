using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 任务配置管理器（TbQuest / TbQuestObjective 包装）。
    /// 设计文档：docs/Proposal/narrative/quest-system.md。
    /// </summary>
    public class QuestConfigMgr
    {
        private static QuestConfigMgr _instance;
        public static QuestConfigMgr Instance => _instance ??= new QuestConfigMgr();

        private readonly Dictionary<int, List<QuestObjective>> _objectivesByQuest = new Dictionary<int, List<QuestObjective>>();
        private readonly Dictionary<int, List<Quest>> _questsByGiver = new Dictionary<int, List<Quest>>();
        private bool _indexBuilt;

        /// <summary>
        /// 获取任务定义，不存在时返回 null。
        /// </summary>
        public Quest Get(int questId)
        {
            return ConfigSystem.Instance.Tables.TbQuest.GetOrDefault(questId);
        }

        /// <summary>
        /// 获取任务的目标列表（按表内顺序），无目标返回空列表。
        /// </summary>
        public List<QuestObjective> GetObjectives(int questId)
        {
            EnsureIndex();
            return _objectivesByQuest.TryGetValue(questId, out var list) ? list : _empty;
        }

        /// <summary>
        /// 获取指定 NPC 发布的任务列表（头顶任务标记用），无则返回空列表。
        /// </summary>
        public List<Quest> GetQuestsByGiver(int npcId)
        {
            EnsureIndex();
            return _questsByGiver.TryGetValue(npcId, out var list) ? list : _emptyQuests;
        }

        /// <summary>
        /// 全部任务定义（任务日志列表用）。
        /// </summary>
        public IReadOnlyList<Quest> GetAll()
        {
            return ConfigSystem.Instance.Tables.TbQuest.DataList;
        }

        private static readonly List<QuestObjective> _empty = new List<QuestObjective>();
        private static readonly List<Quest> _emptyQuests = new List<Quest>();

        private void EnsureIndex()
        {
            if (_indexBuilt) return;
            _indexBuilt = true;
            _objectivesByQuest.Clear();
            _questsByGiver.Clear();
            foreach (var obj in ConfigSystem.Instance.Tables.TbQuestObjective.DataList)
            {
                if (!_objectivesByQuest.TryGetValue(obj.QuestId, out var list))
                {
                    list = new List<QuestObjective>();
                    _objectivesByQuest[obj.QuestId] = list;
                }
                list.Add(obj);
            }
            foreach (var quest in ConfigSystem.Instance.Tables.TbQuest.DataList)
            {
                if (quest.GiverNpc <= 0) continue;
                if (!_questsByGiver.TryGetValue(quest.GiverNpc, out var list))
                {
                    list = new List<Quest>();
                    _questsByGiver[quest.GiverNpc] = list;
                }
                list.Add(quest);
            }
        }
    }
}
