using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 伤害事件（用于统计和伤害数字） </summary>
    public struct DamageEvent
    {
        public int AttackerId;
        public int TargetId;
        public float Damage;
        public DamageCategory Category;
        public bool IsDot;
        public float X, Y;
    }

    /// <summary> 伤害系统事件总线 </summary>
    public static class DamageEvents
    {
        public static System.Action<DamageEvent> OnDamage;
    }

    /// <summary> 纯逻辑伤害系统：MC 护甲公式 + 完整伤害管道 </summary>
    public static class DamageSystem
    {
        public static float GetDamageAfterArmor(float damage, float armor, float toughness = 0)
        {
            if (damage <= 0) return 0;
            if (armor <= 0) return damage;
            float g = Mathf.Min(20f, Mathf.Max(armor / 5f, armor - (4f * damage) / (toughness + 8f)));
            return damage * (1f - g / 25f);
        }

        public static float DealDamage(
            ref UnitState target,
            float rawDamage,
            DamageCategory category,
            ref UnitState attacker,
            UnitList allUnits)
        {
            if (target.State == UnitStateEnum.Dead) return 0;
            if (rawDamage <= 0) return 0;

            if (target.SkillState.GetBool(SkillKeys.CrabBurrowed)) return 0;

            float dmg = rawDamage;

            if (target.HasTag("revenant_special"))
            {
                bool defending = target.SkillState.GetBool(SkillKeys.RevenantDefending, true);
                if (defending)
                {
                    if (category == DamageCategory.Melee) dmg *= 0.1f;
                    else return 0;
                }
            }

            if (category == DamageCategory.Ranged && target.HasTag("kobo_block_ranged"))
                dmg *= 0.5f;

            if (target.HasTag("troll_immune_ranged"))
            {
                if (category == DamageCategory.Ranged || category == DamageCategory.Beam) return 0;
            }

            if (target.SkillState.GetBool(SkillKeys.MurmurHeadActive))
                dmg *= 0.5f;

            if (category != DamageCategory.True)
            {
                if (!attacker.HasTag("armor_piercing"))
                    dmg = GetDamageAfterArmor(dmg, target.Armor, target.ArmorToughness);
                else
                    dmg = GetDamageAfterArmor(dmg, 0, 0);
            }

            target.Hp -= dmg;
            if (target.Hp <= 0) { target.Hp = 0; target.State = UnitStateEnum.Dead; }

            // 触发伤害事件（用于统计和伤害数字）
            if (DamageEvents.OnDamage != null)
            {
                DamageEvents.OnDamage(new DamageEvent
                {
                    AttackerId = attacker.Id,
                    TargetId = target.Id,
                    Damage = dmg,
                    Category = category,
                    IsDot = category == DamageCategory.True,
                    X = target.X,
                    Y = target.Y,
                });
            }

            return dmg;
        }

        public static void ApplyKnockback(ref UnitState target, float knockbackDist, float fromX, float fromY)
        {
            if (target.HasTag("knockback_immune")) return;
            if (target.StatusEffects.Has(StatusEffectType.Stun)) return;
            float dx = target.X - fromX, dy = target.Y - fromY;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 0.01f)
            {
                target.X += (dx / d) * knockbackDist;
                target.Y += (dy / d) * knockbackDist;
            }
            ClampToField(ref target);
        }

        public static void ClampToField(ref UnitState unit)
        {
            float half = unit.FieldHalfExtent;
            unit.X = Mathf.Clamp(unit.X, half, BattleConstants.FIELD_WIDTH - half);
            unit.Y = Mathf.Clamp(unit.Y, half, BattleConstants.FIELD_HEIGHT - half);
        }

        public static float DistSq(float ax, float ay, float bx, float by)
        { float dx = ax - bx, dy = ay - by; return dx * dx + dy * dy; }

        public static float Dist(float ax, float ay, float bx, float by)
            => Mathf.Sqrt(DistSq(ax, ay, bx, by));
    }

    public static class SkillKeys
    {
        public static readonly int CastTimeLeft = nameof(CastTimeLeft).GetHashCode();
        public static readonly int PendingSkill = nameof(PendingSkill).GetHashCode();
        public static readonly int CrabBurrowed = "crab_burrowed".GetHashCode();
        public static readonly int CrabCastTimeLeft = "crab_cast_time".GetHashCode();
        public static readonly int CrabPendingSkill = "crab_pending_skill".GetHashCode();
        public static readonly int RevenantDefending = "revenant_defending".GetHashCode();
        public static readonly int RevenantCastTimeLeft = "revenant_cast_time".GetHashCode();
        public static readonly int RevenantPendingSkill = "revenant_pending_skill".GetHashCode();
        public static readonly int MurmurHeadActive = "murmur_head_active".GetHashCode();
        public static readonly int NagaContactCd = "naga_contact_cd".GetHashCode();
        public static readonly int NagaSegmentCount = "naga_seg_count".GetHashCode();
        public static int NagaSegX(int i) => ("naga_seg_x_" + i).GetHashCode();
        public static int NagaSegY(int i) => ("naga_seg_y_" + i).GetHashCode();
        public static readonly int RemnantCastTimeLeft = "remnant_cast_time".GetHashCode();
        public static readonly int RemnantPendingSkill = "remnant_pending_skill".GetHashCode();
        public static readonly int RemnantObeliskCd = "remnant_obelisk_cd".GetHashCode();
        public static readonly int HarbAttackMode = "harb_attack_mode".GetHashCode();
        public static readonly int HarbModeTimer = "harb_mode_timer".GetHashCode();
        public static readonly int HarbChargeTimeLeft = "harb_charge_time".GetHashCode();
        public static readonly int KoboCastTimeLeft = "kobo_cast_time".GetHashCode();
        public static readonly int KoboPendingSkill = "kobo_pending_skill".GetHashCode();
    }
}
