using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 可受伤对象接口。
    /// 统一玩家、敌人等战斗实体的受伤入口，降低 BattleSystem 与具体实体类型的耦合。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 是否已死亡。
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// 受到伤害。
        /// </summary>
        /// <param name="damage">伤害值。</param>
        /// <param name="hitDirection">受击方向。</param>
        /// <returns>是否实际受到伤害。</returns>
        bool TakeDamage(int damage, Vector2 hitDirection);
    }
}
