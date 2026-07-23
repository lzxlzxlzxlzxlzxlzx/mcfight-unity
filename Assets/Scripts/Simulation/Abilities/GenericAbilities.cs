using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 标准近战攻击 </summary>
    public class MeleeAbility : IAbilityComponent
    {
        public void OnInit(ref UnitState unit) { }

        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];

            float range = Mathf.Max(unit.AttackRange, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            bool inMelee = dist <= range;
            bool canAttack = TargetingSystem.CanTargetForAttack(ref unit, ref target, false);

            if (inMelee && canAttack && unit.AttackCooldown <= 0)
            {
                // 执行近战
                DamageSystem.DealDamage(ref target, unit.Attack, DamageCategory.Melee, ref unit, state.Units);

                // 命中附带状态
                if (unit.HasTag("poison_on_hit"))
                    StatusEffectSystem.Apply(ref target, StatusEffectType.Poison);
                if (unit.HasTag("burn_on_hit"))
                    StatusEffectSystem.Apply(ref target, StatusEffectType.Burn);
                if (unit.HasTag("wither_on_hit"))
                    StatusEffectSystem.Apply(ref target, StatusEffectType.Wither);
                if (unit.HasTag("slow_on_hit"))
                    StatusEffectSystem.Apply(ref target, StatusEffectType.Slow);

                // 飞行近战设置脆弱窗口
                if (unit.MoveType == MoveType.Fly)
                    unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW;

                unit.AttackCooldown = unit.AttackInterval;
                unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME;
                unit.State = UnitStateEnum.Attack;
                return true;
            }
            return false;
        }

        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => unit.AttackRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    /// <summary> AOE 近战攻击 </summary>
    public class AoeMeleeAbility : IAbilityComponent
    {
        public void OnInit(ref UnitState unit) { }

        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];

            float range = Mathf.Max(unit.AttackRange, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            bool inMelee = dist <= range;
            bool canAttack = TargetingSystem.CanTargetForAttack(ref unit, ref target, false);

            if (inMelee && canAttack && unit.AttackCooldown <= 0)
            {
                // AOE 伤害
                float aoeRadius = unit.HasTag("giant") ? 92f : BattleConstants.DEFAULT_AOE_RADIUS;
                AreaEffectSystem.DealInstantAoe(
                    ref unit, target.X, target.Y, aoeRadius, state.Units,
                    unit.Attack, DamageCategory.Melee);

                // 冲击波视觉效果
                state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(
                    state.NextId(), unit.Team, target.X, target.Y, aoeRadius));

                unit.AttackCooldown = unit.AttackInterval;
                unit.AttackAnimTimer = BattleConstants.AOE_ANIM_TIME;
                unit.State = UnitStateEnum.Attack;
                return true;
            }
            return false;
        }

        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => unit.AttackRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    /// <summary> 远程投射攻击 </summary>
    public class RangedAbility : IAbilityComponent
    {
        public void OnInit(ref UnitState unit) { }

        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];

            bool inRange = dist <= unit.AttackRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            bool canAttack = TargetingSystem.CanTargetForAttack(ref unit, ref target, true);

            if (inRange && canAttack && unit.AttackCooldown <= 0)
            {
                // 发射投射物
                float dx = target.X - unit.X;
                float dy = target.Y - unit.Y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float dirX = d > 0.01f ? dx / d : 1f;
                float dirY = d > 0.01f ? dy / d : 0f;

                var proj = ProjectileSystem.CreateDefault(
                    state.NextId(), unit.Team, unit.X, unit.Y, dirX, dirY,
                    unit.Id, unit.MonsterId, unit.Attack, unit.AttackRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD);

                state.Projectiles.Add(proj);

                unit.AttackCooldown = unit.AttackInterval;
                unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME;
                unit.State = UnitStateEnum.Attack;
                return true;
            }
            return false;
        }

        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => unit.AttackRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true; // 远程可对空
    }

    /// <summary> 自爆攻击（苦力怕/核能苦力怕） </summary>
    public class ExplosiveAbility : IAbilityComponent
    {
        private float _explodeRadius;
        private float _fuseDuration;
        private float _centerDamage;
        private float _edgeDamage;
        private bool _triggered;

        public ExplosiveAbility(float explodeRadius, float fuseDuration, float centerDamage, float edgeDamage = 0)
        {
            _explodeRadius = explodeRadius;
            _fuseDuration = fuseDuration;
            _centerDamage = centerDamage;
            _edgeDamage = edgeDamage > 0 ? edgeDamage : centerDamage;
        }

        public void OnInit(ref UnitState unit)
        {
            _triggered = false;
            unit.SkillState.SetFloat(SkillKeys.CastTimeLeft, 0);
        }

        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            // 检查是否有敌人在触发范围内
            bool hasNearbyEnemy = false;
            for (int i = 0; i < state.Units.Count; i++)
            {
                ref var u = ref state.Units[i];
                if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue;
                float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y);
                if (d <= _explodeRadius * 0.8f) { hasNearbyEnemy = true; break; }
            }

            float fuse = unit.SkillState.GetFloat(SkillKeys.CastTimeLeft, 0);

            if (hasNearbyEnemy && fuse <= 0 && !_triggered)
            {
                _triggered = true;
                unit.SkillState.SetFloat(SkillKeys.CastTimeLeft, _fuseDuration);
                unit.State = UnitStateEnum.Attack;
                return true;
            }

            return false;
        }

        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            float fuse = unit.SkillState.GetFloat(SkillKeys.CastTimeLeft, 0);
            if (fuse <= 0) return;

            fuse -= dt;
            unit.SkillState.SetFloat(SkillKeys.CastTimeLeft, fuse);

            if (fuse <= 0)
            {
                // 自爆！伤害所有单位（不分敌我）
                for (int i = 0; i < state.Units.Count; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.State == UnitStateEnum.Dead) continue;

                    float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y);
                    if (d <= _explodeRadius)
                    {
                        float t = d / _explodeRadius; // 0=中心, 1=边缘
                        float dmg = Mathf.Lerp(_centerDamage, _edgeDamage, t);
                        DamageSystem.DealDamage(ref u, dmg, DamageCategory.Explosion, ref unit, state.Units);
                    }
                }

                // 自身死亡
                MCFight.VFXSpriteView.Play("bigexplosion", unit.X, unit.Y, _explodeRadius * 2f, 2f);
                unit.Hp = 0;
                unit.State = UnitStateEnum.Dead;
            }
        }

        public float GetEngageRange(ref UnitState unit) => _explodeRadius * 0.8f;
        public bool IsBusy(ref UnitState unit) => unit.SkillState.GetFloat(SkillKeys.CastTimeLeft, 0) > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }
}
