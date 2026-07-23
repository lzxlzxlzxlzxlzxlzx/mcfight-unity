using System.Collections.Generic;

namespace MCFight
{
    /// <summary> 技能组件接口 —— 纯逻辑，不依赖 MonoBehaviour </summary>
    public interface IAbilityComponent
    {
        /// <summary> 初始化单位技能状态（在单位创建时调用一次） </summary>
        void OnInit(ref UnitState unit);

        /// <summary> 每帧决策：尝试释放技能。返回 true 表示已释放（跳过普攻） </summary>
        bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt);

        /// <summary> 施法中每帧调用 </summary>
        void TickCast(ref UnitState unit, BattleState state, float dt);

        /// <summary> 该单位的交战半径 </summary>
        float GetEngageRange(ref UnitState unit);

        /// <summary> 是否正在施法/引导/蓄力（阻止移动和普攻） </summary>
        bool IsBusy(ref UnitState unit);

        /// <summary> 是否允许对空（影响目标选择） </summary>
        bool AllowAntiAir(ref UnitState unit);
    }

    /// <summary> 无技能的空实现 </summary>
    public class NullAbility : IAbilityComponent
    {
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt) => false;
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => unit.AttackRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }
}
