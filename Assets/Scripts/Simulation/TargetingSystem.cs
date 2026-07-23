using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 纯逻辑目标选择系统 </summary>
    public static class TargetingSystem
    {
        /// <summary> 交战半径（有技能组件时由组件决定，否则用 attackRange） </summary>
        public static float GetEngageRange(ref UnitState unit, IAbilityComponent ability)
        {
            if (ability != null)
                return ability.GetEngageRange(ref unit);
            return unit.AttackRange;
        }

        /// <summary> 对空判定 </summary>
        public static bool CanTargetForAttack(ref UnitState attacker, ref UnitState target, bool allowAntiAir)
        {
            if (allowAntiAir) return true;
            // 飞行单位所有攻击均可对空
            if (attacker.MoveType == MoveType.Fly) return true;
            // 地面远程单位大多数可对空
            if (attacker.AttackType == AttackType.Ranged) return true;
            // 目标是地面单位 → 可攻击
            if (target.MoveType == MoveType.Ground) return true;
            // 地面近战 → 只能打有脆弱窗口的飞行单位
            return target.VulnerableWindow > 0;
        }

        /// <summary> 选择目标 </summary>
        public static int PickTarget(
            ref UnitState unit,
            UnitList units,
            bool forceRetarget,
            IAbilityComponent ability)
        {
            // 收集敌人
            List<int> enemyIds = new();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Team != unit.Team && units[i].State != UnitStateEnum.Dead)
                    enemyIds.Add(units[i].Id);
            }
            if (enemyIds.Count == 0) return -1;

            float range = GetEngageRange(ref unit, ability);

            // 允许对空？
            bool allowAntiAir = ability != null && ability.AllowAntiAir(ref unit);

            // Sticky 目标保持
            if (!forceRetarget && unit.TargetId >= 0)
            {
                int currentIdx = FindUnitById(units, unit.TargetId);
                if (currentIdx >= 0)
                {
                    ref var current = ref units[currentIdx];
                    if (CanTargetForAttack(ref unit, ref current, allowAntiAir))
                    {
                        float d = DamageSystem.Dist(unit.X, unit.Y, current.X, current.Y);
                        if (d <= range + BattleConstants.STICKY_RANGE_BONUS)
                            return current.Id;
                    }
                }
            }

            // 选择最近目标
            return PickNearestTarget(ref unit, units, enemyIds, allowAntiAir, range);
        }

        static int PickNearestTarget(
            ref UnitState unit,
            UnitList units,
            List<int> enemyIds,
            bool allowAntiAir,
            float range)
        {
            int bestId = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < enemyIds.Count; i++)
            {
                int idx = FindUnitById(units, enemyIds[i]);
                if (idx < 0) continue;
                ref var enemy = ref units[idx];

                if (!CanTargetForAttack(ref unit, ref enemy, allowAntiAir)) continue;

                float d = DamageSystem.Dist(unit.X, unit.Y, enemy.X, enemy.Y);

                // anti_arthropod：对飞行节肢生物优先（距离 ×0.75）
                if (unit.HasTag("anti_arthropod") && enemy.MoveType == MoveType.Fly && enemy.HasTag("arthropod"))
                    d *= BattleConstants.ANTI_ARTHROPOD_BIAS;

                if (d < bestScore)
                {
                    bestScore = d;
                    bestId = enemy.Id;
                }
            }

            return bestId;
        }

        static int FindUnitById(UnitList units, int id)
        {
            for (int i = 0; i < units.Count; i++)
                if (units[i].Id == id) return i;
            return -1;
        }

        /// <summary> 获取目标单位在列表中的索引 </summary>
        public static int GetTargetIndex(UnitList units, int targetId)
        {
            if (targetId < 0) return -1;
            return FindUnitById(units, targetId);
        }
    }
}
