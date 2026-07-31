using UnityEngine;
using System.Collections.Generic;

namespace MCFight
{
    public class DualModeAbility : IAbilityComponent
    {
        private float _switchRange, _meleeDmg, _rangedDmg;
        private StatusEffectType[] _onHit;
        private string _mid;
        public DualModeAbility(MonsterDefSO def) : this(def, 100f) { }
        public DualModeAbility(MonsterDefSO def, float switchRange)
        {
            _mid = def.monsterId;
            _switchRange = MonsterConfigLoader.GetAbilityParam(_mid, "switchRange");
            _rangedDmg = def.attack;
            _meleeDmg = def.attack * 1.4f;
            _onHit = def.onHitEffects;
        }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (dist <= _switchRange)
            {
                float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
                if (dist <= range && TargetingSystem.CanTargetForAttack(ref unit, ref target, false))
                {
                    DamageSystem.DealDamage(ref target, _meleeDmg, DamageCategory.Melee, ref unit, state.Units);
                    if (_onHit != null) StatusEffectSystem.ApplyAll(ref target, _onHit);
                    if (unit.MoveType == MoveType.Fly) unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW;
                    unit.AttackCooldown = 0.85f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
                    return true;
                }
                return false;
            }
            else
            {
                if (dist <= 160f + target.Radius * BattleConstants.TARGET_RADIUS_PAD && TargetingSystem.CanTargetForAttack(ref unit, ref target, true))
                {
                    float dx = target.X - unit.X, dy = target.Y - unit.Y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _rangedDmg, 160f, _onHit);
                    state.Projectiles.Add(proj);
                    unit.AttackCooldown = 1.1f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
                    return true;
                }
                return false;
            }
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class TrollAbility : IAbilityComponent
    {
        private float _heavyCd = 0f;
        private float _heavyDmg, _normalDmg, _heavyCooldown;
        private string _mid;
        public TrollAbility(MonsterDefSO def) { _mid = def.monsterId; _heavyDmg = MonsterConfigLoader.GetAbilityParam(_mid, "heavyDamage"); _normalDmg = MonsterConfigLoader.GetAbilityParam(_mid, "normalDamage"); _heavyCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "heavyCooldown"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_heavyCd > 0) _heavyCd -= dt;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            float dmg = _heavyCd <= 0 ? _heavyDmg : _normalDmg;
            if (_heavyCd <= 0) _heavyCd = _heavyCooldown;
            DamageSystem.DealDamage(ref target, dmg, DamageCategory.Melee, ref unit, state.Units);
            unit.AttackCooldown = 0.85f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => unit.AttackRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class BerserkerAbility : IAbilityComponent
    {
        private bool _useSpin = false;
        private float _spinTimer = 0f;
        private int _spinTicks = 0;
        private float _spinTickTimer = 0f;
        private int _spinMaxTicks;
        private float _spinDamage, _spinRadius, _spinInterval, _meleeDamage;
        private string _mid;
        public BerserkerAbility(MonsterDefSO def) { _mid = def.monsterId; _spinMaxTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "spinTicks"); _spinDamage = MonsterConfigLoader.GetAbilityParam(_mid, "spinDamage"); _spinRadius = MonsterConfigLoader.GetAbilityParam(_mid, "spinRadius"); _spinInterval = MonsterConfigLoader.GetAbilityParam(_mid, "spinInterval"); _meleeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meleeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            if (_useSpin) { _spinTimer = 3f; _spinTicks = 0; _spinTickTimer = 0f; unit.SkillState.SetFloat(SkillKeys.CastTimeLeft, 3f); unit.AttackCooldown = 3f; unit.State = UnitStateEnum.Attack; }
            else { DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); StatusEffectSystem.Apply(ref target, StatusEffectType.Burn); unit.AttackCooldown = 3f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack; }
            _useSpin = !_useSpin;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_spinTimer > 0) { _spinTimer -= dt; _spinTickTimer += dt;
                if (_spinTickTimer >= _spinInterval && _spinTicks < _spinMaxTicks) { _spinTickTimer -= _spinInterval; _spinTicks++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _spinRadius, state.Units, _spinDamage, DamageCategory.Melee, new[] { StatusEffectType.Burn }); }
                if (_spinTimer <= 0) unit.SkillState.SetFloat(SkillKeys.CastTimeLeft, 0); }
        }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => _spinTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class ChargeMeleeAbility : IAbilityComponent
    {
        private float _normalDmg, _chargeDmg, _chargeThreshold;
        private float _lastAttackTime;
        private string _mid;
        public ChargeMeleeAbility(MonsterDefSO def, float normalDmg, float chargeDmg, float threshold)
        { _mid = def.monsterId; _normalDmg = MonsterConfigLoader.GetAbilityParam(_mid, "normalDamage"); _chargeDmg = MonsterConfigLoader.GetAbilityParam(_mid, "chargeDamage"); _chargeThreshold = MonsterConfigLoader.GetAbilityParam(_mid, "chargeThreshold"); _lastAttackTime = _chargeThreshold; }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            _lastAttackTime += dt;
            float dmg = _lastAttackTime >= _chargeThreshold ? _chargeDmg : _normalDmg;
            _lastAttackTime = 0;
            DamageSystem.DealDamage(ref target, dmg, DamageCategory.Melee, ref unit, state.Units);
            unit.AttackCooldown = 0.85f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { _lastAttackTime += dt; }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class GoblinAbility : IAbilityComponent
    {
        private float _aoeRadius, _aoeDamage, _knockbackForce;
        private string _mid;
        public GoblinAbility(MonsterDefSO def) { _mid = def.monsterId; _aoeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "aoeRadius"); _aoeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "aoeDamage"); _knockbackForce = MonsterConfigLoader.GetAbilityParam(_mid, "knockbackForce"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _aoeRadius, state.Units, _aoeDamage, DamageCategory.Melee);
            for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y); if (d <= _aoeRadius + u.Radius) DamageSystem.ApplyKnockback(ref u, _knockbackForce, unit.X, unit.Y); }
            state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _aoeRadius));
            unit.AttackCooldown = 0.85f; unit.AttackAnimTimer = BattleConstants.AOE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class ConeBreathAbility : IAbilityComponent
    {
        private readonly StatusEffectType _effect;
        private float _breathTimer = 0f;
        private float _tickTimer = 0f;
        private int _ticks = 0;
        private int _maxTicks;
        private float _tickInterval, _range, _angleDeg, _dmgPerTick;
        private string _mid;
        public ConeBreathAbility(MonsterDefSO def)
        { _mid = def.monsterId; _effect = def.onHitEffects != null && def.onHitEffects.Length > 0 ? def.onHitEffects[0] : StatusEffectType.Slow; _maxTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "breathTicks"); _tickInterval = MonsterConfigLoader.GetAbilityParam(_mid, "breathInterval"); _range = MonsterConfigLoader.GetAbilityParam(_mid, "breathRange"); _angleDeg = MonsterConfigLoader.GetAbilityParam(_mid, "breathAngle"); _dmgPerTick = MonsterConfigLoader.GetAbilityParam(_mid, "breathDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            if (unit.AttackCooldown > 0) return false;
            if (dist > _range + 42f + state.Units[targetIdx].Radius * BattleConstants.TARGET_RADIUS_PAD) return false;
            _breathTimer = 2f; _tickTimer = 0f; _ticks = 0;
            unit.AttackCooldown = 2f; unit.AttackAnimTimer = 2f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_breathTimer <= 0) return;
            _breathTimer -= dt; _tickTimer += dt;
            if (_tickTimer >= _tickInterval && _ticks < _maxTicks)
            {
                _tickTimer -= _tickInterval; _ticks++;
                int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId);
                float aimAngleRad = unit.Facing >= 0 ? 0f : Mathf.PI;
                if (tIdx >= 0)
                {
                    ref var target = ref state.Units[tIdx];
                    aimAngleRad = Mathf.Atan2(target.Y - unit.Y, target.X - unit.X);
                }
                float vfxAngle = aimAngleRad * Mathf.Rad2Deg - 90f;
                float facingDirX = Mathf.Cos(aimAngleRad), facingDirY = Mathf.Sin(aimAngleRad);
                string vfxName = _effect == StatusEffectType.Burn ? "firebreath" : "icemist";
                MCFight.VFXSpriteView.Play(vfxName, unit.X + facingDirX * _range * 0.3f, unit.Y + facingDirY * _range * 0.3f, _range, 0.6f, vfxAngle, true);
                float halfAngleRad = _angleDeg * 0.5f * Mathf.Deg2Rad;
                AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aimAngleRad, halfAngleRad, _range,
                    state.Units, _dmgPerTick, DamageCategory.Ranged, new[] { _effect }, false);
            }
            if (_breathTimer <= 0) unit.State = UnitStateEnum.Idle;
        }
        public float GetEngageRange(ref UnitState unit) => _range;
        public bool IsBusy(ref UnitState unit) => _breathTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class MagnetronAbility : IAbilityComponent
    {
        private float _detectRadius, _baseDamage, _knockbackForce;
        private string _mid;
        public MagnetronAbility(MonsterDefSO def) { _mid = def.monsterId; _detectRadius = MonsterConfigLoader.GetAbilityParam(_mid, "detectRadius"); _baseDamage = MonsterConfigLoader.GetAbilityParam(_mid, "baseDamage"); _knockbackForce = MonsterConfigLoader.GetAbilityParam(_mid, "knockbackForce"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            int enemyCount = 0;
            for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y); if (d <= _detectRadius) enemyCount++; }
            float dmg = _baseDamage + enemyCount;
            DamageSystem.DealDamage(ref target, dmg, DamageCategory.Melee, ref unit, state.Units);
            DamageSystem.ApplyKnockback(ref target, _knockbackForce, unit.X, unit.Y);
            unit.AttackCooldown = 1.5f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class StraddlerAbility : IAbilityComponent
    {
        public StraddlerAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (dist > 160f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            float dx = target.X - unit.X, dy = target.Y - unit.Y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 3f, 160f);
            state.Projectiles.Add(proj);
            unit.AttackCooldown = 1.1f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class StymphalianAbility : IAbilityComponent
    {
        public StymphalianAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (dist > 100f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            FireShot(ref unit, targetIdx, state);
            FireShot(ref unit, targetIdx, state);
            unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        void FireShot(ref UnitState unit, int targetIdx, BattleState state)
        {
            ref var target = ref state.Units[targetIdx];
            float dx = target.X - unit.X, dy = target.Y - unit.Y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 1f, 100f);
            proj.Speed = 350f;
            state.Projectiles.Add(proj);
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 100f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }
}
