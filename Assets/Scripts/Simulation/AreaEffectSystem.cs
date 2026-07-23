using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 纯逻辑区域效果系统（统一管理所有 AoE） </summary>
    public static class AreaEffectSystem
    {
        /// <summary> 创建冲击波 </summary>
        public static AreaEffectData CreateShockwave(int id, int team, float x, float y, float radius, float duration = 0.45f)
        {
            return new AreaEffectData
            {
                Id = id,
                Type = AreaEffectType.Shockwave,
                Team = team,
                X = x, Y = y,
                Radius = radius,
                Duration = duration,
                Remaining = duration,
            };
        }

        /// <summary> 创建熔岩区域 </summary>
        public static AreaEffectData CreateLava(int id, int team, float x, float y, float radius, float duration, float dps)
        {
            return new AreaEffectData
            {
                Id = id,
                Type = AreaEffectType.LavaPatch,
                Team = team,
                X = x, Y = y,
                Radius = radius,
                Duration = duration,
                Remaining = duration,
                Damage = dps,
                DamageCategory = DamageCategory.Ranged,
                StatusOnTick = new[] { StatusEffectType.Burn },
            };
        }

        /// <summary> 创建冰冻区域 </summary>
        public static AreaEffectData CreateFrostZone(int id, int team, float x, float y, float radius, float duration, float dps)
        {
            return new AreaEffectData
            {
                Id = id,
                Type = AreaEffectType.FrostZone,
                Team = team,
                X = x, Y = y,
                Radius = radius,
                Duration = duration,
                Remaining = duration,
                Damage = dps,
                DamageCategory = DamageCategory.Ranged,
                StatusOnTick = new[] { StatusEffectType.Slow },
            };
        }

        /// <summary> 创建污染区域 </summary>
        public static AreaEffectData CreatePollutionZone(int id, int team, float x, float y, float radius, float duration, float dps)
        {
            return new AreaEffectData
            {
                Id = id,
                Type = AreaEffectType.PollutionZone,
                Team = team,
                X = x, Y = y,
                Radius = radius,
                Duration = duration,
                Remaining = duration,
                Damage = dps,
                DamageCategory = DamageCategory.Ranged,
                StatusOnTick = new[] { StatusEffectType.Poison, StatusEffectType.Slow },
            };
        }

        /// <summary> 每帧 tick 所有区域效果 </summary>
        public static void Tick(List<AreaEffectData> effects, UnitList units, float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var eff = effects[i];
                eff.Remaining -= dt;
                if (eff.Remaining <= 0)
                {
                    effects.RemoveAt(i);
                    continue;
                }

                // DoT 类区域（熔岩/冰冻/污染）
                if (eff.Type == AreaEffectType.LavaPatch ||
                    eff.Type == AreaEffectType.FrostZone ||
                    eff.Type == AreaEffectType.PollutionZone)
                {
                    eff.DotTimer += dt;
                    if (eff.DotTimer >= 1f)
                    {
                        eff.DotTimer -= 1f;
                        ApplyAreaDamage(ref eff, units);
                        effects[i] = eff;
                    }
                }
                // 冲击波（扩散伤害，只命中一次）
                else if (eff.Type == AreaEffectType.Shockwave)
                {
                    // 冲击波在创建时即时结算，这里只负责存活计时
                }
            }
        }

        /// <summary> 区域内伤害结算 </summary>
        public static void ApplyAreaDamage(ref AreaEffectData eff, UnitList units)
        {
            int sourceIdx = FindUnitById(units, eff.SourceId);
            ref var source = ref units[sourceIdx >= 0 ? sourceIdx : 0];

            for (int i = 0; i < units.Count; i++)
            {
                ref var u = ref units[i];
                if (u.State == UnitStateEnum.Dead || u.Team == eff.Team) continue;

                // 地面区域只影响地面单位（fire_immune 免疫熔岩）
                if (eff.Type == AreaEffectType.LavaPatch)
                {
                    if (u.MoveType == MoveType.Fly) continue;
                    if (u.HasTag("fire_immune")) continue;
                }
                if (eff.Type == AreaEffectType.FrostZone || eff.Type == AreaEffectType.PollutionZone)
                {
                    if (u.MoveType == MoveType.Fly) continue;
                }

                float d = DamageSystem.Dist(eff.X, eff.Y, u.X, u.Y);
                float hitRange = eff.Radius + u.Radius * 0.5f;
                if (d <= hitRange)
                {
                    DamageSystem.DealDamage(ref u, eff.Damage, eff.DamageCategory, ref source, units);
                    if (eff.StatusOnTick != null)
                        StatusEffectSystem.ApplyAll(ref u, eff.StatusOnTick);
                }
            }
        }

        /// <summary> 即时 AOE 伤害（一次性） </summary>
        public static void DealInstantAoe(
            ref UnitState attacker,
            float centerX, float centerY,
            float radius,
            UnitList units,
            float damage,
            DamageCategory category = DamageCategory.Melee,
            StatusEffectType[] statusOnHit = null,
            bool groundOnly = false)
        {
            for (int i = 0; i < units.Count; i++)
            {
                ref var u = ref units[i];
                if (u.State == UnitStateEnum.Dead || u.Team == attacker.Team) continue;
                if (groundOnly && u.MoveType == MoveType.Fly) continue;

                float d = DamageSystem.Dist(centerX, centerY, u.X, u.Y);
                float hitRange = radius + u.Radius * 0.5f;
                if (d <= hitRange)
                {
                    DamageSystem.DealDamage(ref u, damage, category, ref attacker, units);
                    if (statusOnHit != null)
                        StatusEffectSystem.ApplyAll(ref u, statusOnHit);
                }
            }
        }

        static int FindUnitById(UnitList units, int id)
        {
            for (int i = 0; i < units.Count; i++)
                if (units[i].Id == id) return i;
            return -1;
        }
    }
}
