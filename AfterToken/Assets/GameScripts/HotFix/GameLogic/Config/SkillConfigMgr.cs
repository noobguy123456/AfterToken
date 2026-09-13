using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 技能树配置管理器（TbSkill 包装）。分支：1=战斗 2=生存 3=经营。
    /// </summary>
    public class SkillConfigMgr
    {
        private static SkillConfigMgr _instance;
        public static SkillConfigMgr Instance => _instance ??= new SkillConfigMgr();

        /// <summary>按技能 ID 取节点，不存在返回 null。</summary>
        public Skill Get(int skillId)
        {
            return ConfigSystem.Instance.Tables.TbSkill.GetOrDefault(skillId);
        }

        /// <summary>取指定分支的全部节点（按 row/col 排序，供树 UI 摆位）。</summary>
        public List<Skill> GetByBranch(int branch)
        {
            var result = new List<Skill>();
            var list = ConfigSystem.Instance.Tables.TbSkill.DataList;
            if (list == null)
            {
                return result;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s != null && s.Branch == branch)
                {
                    result.Add(s);
                }
            }
            result.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Col.CompareTo(b.Col));
            return result;
        }

        /// <summary>全部节点。</summary>
        public IReadOnlyList<Skill> All => ConfigSystem.Instance.Tables.TbSkill.DataList;
    }
}
