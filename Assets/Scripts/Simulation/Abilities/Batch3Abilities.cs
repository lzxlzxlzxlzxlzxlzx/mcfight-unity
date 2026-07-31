using UnityEngine;
using System.Collections.Generic;

namespace MCFight
{
    public class WardenAbility : IAbilityComponent
    {
        private float _rangedCd = 0f;
        private string _mid;
        private float _sonicDamage, _sonicRange, _sonicCooldown, _sonicSpeed, _meleeDamage;
        public WardenAbility(MonsterDefSO def) { _mid = def.monsterId; _sonicDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sonicDamage"); _sonicRange = MonsterConfigLoader.GetAbilityParam(_mid, "sonicRange"); _sonicCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "sonicCooldown"); _sonicSpeed = MonsterConfigLoader.GetAbilityParam(_mid, "sonicSpeed"); _meleeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meleeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_rangedCd > 0) _rangedCd -= dt;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (_rangedCd <= 0 && dist <= _sonicRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD)
            { float sdx = target.X - unit.X, sdy = target.Y - unit.Y; float sd = Mathf.Sqrt(sdx * sdx + sdy * sdy); float dirX = sd > 0.01f ? sdx / sd : 1f, dirY = sd > 0.01f ? sdy / sd : 0f; var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, dirX, dirY, unit.Id, unit.MonsterId, _sonicDamage, _sonicRange); proj.Kind = ProjectileKind.ForsakenSonic; proj.PierceHalfWidth = 26f; proj.HitEnemyIds = new List<int>(); proj.Speed = _sonicSpeed; proj.MaxTravel = 0; state.Projectiles.Add(proj); _rangedCd = _sonicCooldown; unit.AttackCooldown = 1.5f; unit.State = UnitStateEnum.Attack; return true; }
            float range = Mathf.Max(48f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist <= range && TargetingSystem.CanTargetForAttack(ref unit, ref target, false))
            { DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); unit.AttackCooldown = 1.5f; unit.AttackAnimTimer = 0.3f; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => _sonicRange;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class TremorsaurusAbility : IAbilityComponent
    {
        private float _roarCd = 0f; private float _roarTimer = 0f;
        private string _mid;
        private float _roarCooldown, _roarRadius, _meleeDamage;
        public TremorsaurusAbility(MonsterDefSO def) { _mid = def.monsterId; _roarCd = 0f; _roarCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "roarCooldown"); _roarRadius = MonsterConfigLoader.GetAbilityParam(_mid, "roarRadius"); _meleeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meleeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_roarCd > 0) _roarCd -= dt;
            if (_roarTimer > 0) return true;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (_roarCd <= 0) { _roarTimer = 2f; _roarCd = _roarCooldown + (float)state.RNG.NextDouble() * 5f; for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; if (!u.HasTag("boss")) StatusEffectSystem.Apply(ref u, StatusEffectType.Fear); } state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _roarRadius, 0.55f)); unit.State = UnitStateEnum.Attack; return true; }
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist <= range && TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) { DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); unit.AttackCooldown = 0.7f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { if (_roarTimer > 0) { _roarTimer -= dt; unit.State = UnitStateEnum.Attack; } }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => _roarTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class CoralLeapAbility : IAbilityComponent
    {
        private readonly float _leapMaxRange, _leapRadius, _leapDmg, _leapDuration;
        private float _leapTimer = 0f; private float _fromX, _fromY, _toX, _toY;
        private string _mid;
        public CoralLeapAbility(MonsterDefSO def) { _mid = def.monsterId; _leapMaxRange = MonsterConfigLoader.GetAbilityParam(_mid, "leapMaxRange"); _leapRadius = MonsterConfigLoader.GetAbilityParam(_mid, "leapRadius"); _leapDmg = MonsterConfigLoader.GetAbilityParam(_mid, "leapDamage"); _leapDuration = MonsterConfigLoader.GetAbilityParam(_mid, "leapDuration"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            if (unit.AttackCooldown > 0) return false;
            if (_leapTimer > 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            if (dist > _leapMaxRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD) return false;
            _leapTimer = _leapDuration; _fromX = unit.X; _fromY = unit.Y; _toX = target.X; _toY = target.Y;
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_leapTimer <= 0) return;
            _leapTimer -= dt; float t = 1f - _leapTimer / _leapDuration;
            MovementSystem.SetLeapArcPosition(ref unit, _fromX, _fromY, _toX, _toY, t, 42f);
            if (_leapTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _leapRadius, state.Units, _leapDmg, DamageCategory.Melee); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _leapRadius, 0.42f)); VFXSpriteView.Play("closeaoe", unit.X, unit.Y, _leapRadius * 2f, 0.5f); unit.AttackCooldown = 2f; unit.State = UnitStateEnum.Idle; }
        }
        public float GetEngageRange(ref UnitState unit) => _leapMaxRange;
        public bool IsBusy(ref UnitState unit) => _leapTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class CyclopsAbility : IAbilityComponent
    {
        private float _devourRecovery = 0f;
        private string _mid;
        private float _devourThreshold, _aoeDamage, _aoeRadius;
        public CyclopsAbility(MonsterDefSO def) { _mid = def.monsterId; _devourThreshold = MonsterConfigLoader.GetAbilityParam(_mid, "devourThreshold"); _aoeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "aoeDamage"); _aoeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "aoeRadius"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (_devourRecovery > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range) return false;
            if (target.MaxHp <= _devourThreshold && TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) { target.Hp = 0; target.State = UnitStateEnum.Dead; _devourRecovery = 3f; unit.State = UnitStateEnum.Attack; return true; }
            else if (target.MoveType == MoveType.Ground) { AreaEffectSystem.DealInstantAoe(ref unit, target.X, target.Y, _aoeRadius, state.Units, _aoeDamage, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, target.X, target.Y, _aoeRadius, 0.38f)); unit.AttackCooldown = 2f; unit.AttackAnimTimer = BattleConstants.AOE_ANIM_TIME; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { if (_devourRecovery > 0) _devourRecovery -= dt; }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => _devourRecovery > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class EnderGolemAbility : IAbilityComponent
    {
        private int _skillIdx = 0; private float _lockTimer = 0f;
        private string _mid;
        private float _baseDamage, _aoeRadius, _rangedRadius;
        public EnderGolemAbility(MonsterDefSO def) { _mid = def.monsterId; _baseDamage = MonsterConfigLoader.GetAbilityParam(_mid, "baseDamage"); _aoeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "aoeRadius"); _rangedRadius = MonsterConfigLoader.GetAbilityParam(_mid, "rangedRadius"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0 || _lockTimer > 0) return false;
            int choice = _skillIdx % 3; float dmg = _baseDamage + (float)state.RNG.NextDouble() * 6f;
            if (choice == 0 && dist <= Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false; DamageSystem.DealDamage(ref target, dmg, DamageCategory.Melee, ref unit, state.Units); }
            else if (choice == 1 && dist <= _aoeRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _aoeRadius, state.Units, dmg, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _aoeRadius, 0.42f)); }
            else if (dist <= 240f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { float aimAngle = Mathf.Atan2(target.Y - unit.Y, target.X - unit.X); float dirX = Mathf.Cos(aimAngle), dirY = Mathf.Sin(aimAngle); AreaEffectSystem.DealCompositeAoe(ref unit, unit.X, unit.Y, dirX, dirY, 240f, 12f, 48f, state.Units, dmg, DamageCategory.Ranged, null, false); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, target.X, target.Y, _rangedRadius, 0.35f)); }
            else return false;
            _skillIdx++; unit.AttackCooldown = 2f; _lockTimer = 1f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { if (_lockTimer > 0) _lockTimer -= dt; }
        public float GetEngageRange(ref UnitState unit) => 240f;
        public bool IsBusy(ref UnitState unit) => _lockTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class AmethystCrabAbility : IAbilityComponent
    {
        private enum Phase { Idle, Burrow, EmergeCast, SweepCast }
        private Phase _phase = Phase.Idle; private float _phaseTimer = 0f;
        private string _mid;
        private float _burrowDuration, _emergeDamage, _sweepDamage;
        public AmethystCrabAbility(MonsterDefSO def) { _mid = def.monsterId; _burrowDuration = MonsterConfigLoader.GetAbilityParam(_mid, "burrowDuration"); _emergeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "emergeDamage"); _sweepDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sweepDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_phase != Phase.Idle || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (dist > 48f + target.Radius) return false;
            _phase = Phase.Burrow; _phaseTimer = _burrowDuration; unit.SkillState.SetBool(SkillKeys.CrabBurrowed, true); unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_phase == Phase.Idle) return;
            _phaseTimer -= dt;
            switch (_phase) {
                case Phase.Burrow: if (_phaseTimer <= 0) { _phase = Phase.EmergeCast; _phaseTimer = 2f; unit.SkillState.SetBool(SkillKeys.CrabBurrowed, false); } break;
                case Phase.EmergeCast: if (_phaseTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, 48f, state.Units, _emergeDamage, DamageCategory.Melee, null, false); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, 48f, 0.4f)); _phase = Phase.SweepCast; _phaseTimer = 3f; } break;
                case Phase.SweepCast: if (_phaseTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, 20f, state.Units, _sweepDamage, DamageCategory.Melee, null, true); _phase = Phase.Idle; unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Idle; } break;
            }
        }
        public float GetEngageRange(ref UnitState unit) => 48f;
        public bool IsBusy(ref UnitState unit) => _phase != Phase.Idle;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class RevenantAbility : IAbilityComponent
    {
        private float _castTimer = 0f; private float _tickTimer = 0f; private int _ticksDone = 0; private int _pendingSkill = -1;
        private string _mid;
        private int _spinTicks, _sonicTicks;
        private float _spinDamage, _spinRadius, _spinTickInterval, _sonicDamage, _sonicRadius, _sonicTickInterval, _projDamage, _projRange;
        public RevenantAbility(MonsterDefSO def) { _mid = def.monsterId; _spinTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "spinTicks"); _spinDamage = MonsterConfigLoader.GetAbilityParam(_mid, "spinDamage"); _spinRadius = MonsterConfigLoader.GetAbilityParam(_mid, "spinRadius"); _spinTickInterval = MonsterConfigLoader.GetAbilityParam(_mid, "spinTickInterval"); _sonicTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "sonicTicks"); _sonicDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sonicDamage"); _sonicRadius = MonsterConfigLoader.GetAbilityParam(_mid, "sonicRadius"); _sonicTickInterval = MonsterConfigLoader.GetAbilityParam(_mid, "sonicTickInterval"); _projDamage = MonsterConfigLoader.GetAbilityParam(_mid, "projDamage"); _projRange = MonsterConfigLoader.GetAbilityParam(_mid, "projRange"); }
        public void OnInit(ref UnitState unit) { unit.SkillState.SetBool(SkillKeys.RevenantDefending, true); }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_castTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (target.MoveType == MoveType.Fly) _pendingSkill = state.RNG.Next(2) == 0 ? 1 : 2;
            else _pendingSkill = state.RNG.Next(3);
            _castTimer = 2f; _tickTimer = 0f; _ticksDone = 0;
            if (_pendingSkill == 2) unit.SkillState.SetBool(SkillKeys.RevenantDefending, false);
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return;
            _castTimer -= dt; _tickTimer += dt;
            switch (_pendingSkill) {
                case 0: if (_tickTimer >= _spinTickInterval && _ticksDone < _spinTicks) { _tickTimer -= _spinTickInterval; _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _spinRadius, state.Units, _spinDamage, DamageCategory.Melee, null, true); } break;
                case 1: if (_tickTimer >= _sonicTickInterval && _ticksDone < _sonicTicks) { _tickTimer -= _sonicTickInterval; _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _sonicRadius, state.Units, _sonicDamage, DamageCategory.Ranged, new[] { StatusEffectType.Burn }, false); MCFight.VFXSpriteView.Play("soundwave", unit.X, unit.Y, _sonicRadius * 2f, 0.5f); } break;
                case 2: if (_tickTimer >= _sonicTickInterval && _ticksDone < _sonicTicks) { _tickTimer -= _sonicTickInterval; _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _projDamage, _projRange); proj.Kind = ProjectileKind.RevenantBone; proj.Speed = 300f; state.Projectiles.Add(proj); } } break;
            }
            if (_castTimer <= 0) { unit.SkillState.SetBool(SkillKeys.RevenantDefending, true); unit.AttackCooldown = 1f; _pendingSkill = -1; }
        }
        public float GetEngageRange(ref UnitState unit) => _projRange;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class WarpedMoscoAbility : IAbilityComponent
    {
        private bool _transformed = false;
        private string _mid;
        private float _transformThreshold, _meleeDamage, _healAmount;
        public WarpedMoscoAbility(MonsterDefSO def) { _mid = def.monsterId; _transformThreshold = MonsterConfigLoader.GetAbilityParam(_mid, "transformThreshold"); _meleeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meleeDamage"); _healAmount = MonsterConfigLoader.GetAbilityParam(_mid, "healAmount"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!_transformed && unit.Hp <= unit.MaxHp * _transformThreshold) { _transformed = true; unit.MoveType = MoveType.Fly; unit.AttackType = AttackType.Ranged; unit.MoveSpeed = 128; unit.BaseMoveSpeed = 128; unit.Attack = 7; unit.AttackRange = 180; unit.AttackInterval = 1f; unit.BaseAttackInterval = 1f; }
            if (unit.AttackCooldown > 0) return false;
            if (_transformed) { if (dist > 180f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false; float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 7f, 180f); state.Projectiles.Add(proj); unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Attack; return true; }
            else { float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD; if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false; int choice = state.RNG.Next(3); if (choice == 0) DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); else if (choice == 1) AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, 56f, state.Units, _meleeDamage + (float)state.RNG.NextDouble() * 11f, DamageCategory.Melee, null, true); else { DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + _healAmount); } unit.AttackCooldown = 3f; unit.AttackAnimTimer = 0.35f; unit.State = UnitStateEnum.Attack; return true; }
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => _transformed ? 180f : 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => _transformed;
    }

    public class FarseerAbility : IAbilityComponent
    {
        private bool _useRay = true; private int _modeCount = 0;
        private string _mid;
        private float _rayDamagePct, _rayRange, _meleeDamage;
        public FarseerAbility(MonsterDefSO def) { _mid = def.monsterId; _rayDamagePct = MonsterConfigLoader.GetAbilityParam(_mid, "rayDamagePct"); _rayRange = MonsterConfigLoader.GetAbilityParam(_mid, "rayRange"); _meleeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meleeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (_useRay) { if (dist > _rayRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false; float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, target.MaxHp * _rayDamagePct, _rayRange); state.Projectiles.Add(proj); unit.AttackCooldown = 1.5f; _modeCount++; if (_modeCount >= 3) { _useRay = false; _modeCount = 0; } }
            else { float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD; if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false; DamageSystem.DealDamage(ref target, _meleeDamage, DamageCategory.Melee, ref unit, state.Units); unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW; unit.AttackCooldown = 0.7f; _modeCount++; if (_modeCount >= 5) { _useRay = true; _modeCount = 0; } }
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => _useRay ? _rayRange : 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class DeepOneMageAbility : IAbilityComponent
    {
        private string _mid;
        private float _closeAoeDamage, _closeAoeRadius, _waterDamage, _waveDamage, _waveRange, _waveAngle;
        public DeepOneMageAbility(MonsterDefSO def) { _mid = def.monsterId; _closeAoeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "closeAoeDamage"); _closeAoeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "closeAoeRadius"); _waterDamage = MonsterConfigLoader.GetAbilityParam(_mid, "waterDamage"); _waveDamage = MonsterConfigLoader.GetAbilityParam(_mid, "waveDamage"); _waveRange = MonsterConfigLoader.GetAbilityParam(_mid, "waveRange"); _waveAngle = MonsterConfigLoader.GetAbilityParam(_mid, "waveAngle"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            if (dist <= 70f) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _closeAoeRadius, state.Units, _closeAoeDamage, DamageCategory.Melee, null, true); VFXSpriteView.Play("closeaoe", unit.X, unit.Y, 60f, 0.5f); unit.AttackCooldown = 1.2f; }
            else { int choice = state.RNG.Next(2); if (choice == 0 && dist <= 220f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _waterDamage, 220f); proj.Speed = 260f; state.Projectiles.Add(proj); } else if (dist <= _waveRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { DamageSystem.ApplyKnockback(ref target, 80f, unit.X, unit.Y); float wdx = target.X - unit.X, wdy = target.Y - unit.Y; float wd = Mathf.Sqrt(wdx * wdx + wdy * wdy); float baseAngle = Mathf.Atan2(wd > 0.01f ? wdy / wd : 0f, wd > 0.01f ? wdx / wd : 1f); for (int sw = -1; sw <= 1; sw++) { float a = baseAngle + sw * _waveAngle; var waveProj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, Mathf.Cos(a), Mathf.Sin(a), unit.Id, unit.MonsterId, _waveDamage, _waveRange); waveProj.Kind = ProjectileKind.ForsakenSonic; waveProj.PierceHalfWidth = 16f; waveProj.HitEnemyIds = new List<int>(); waveProj.Speed = 300f; waveProj.MaxTravel = 0; state.Projectiles.Add(waveProj); } } else return false; unit.AttackCooldown = 1.2f; }
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 220f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class NucleeperAbility : IAbilityComponent
    {
        private float _fuseTimer = -1f; private bool _triggered = false;
        private string _mid;
        private float _explodeRadius, _fuseDuration, _centerDamage, _edgeDamage;
        public NucleeperAbility(MonsterDefSO def) { _mid = def.monsterId; _explodeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "explodeRadius"); _fuseDuration = MonsterConfigLoader.GetAbilityParam(_mid, "fuseDuration"); _centerDamage = MonsterConfigLoader.GetAbilityParam(_mid, "centerDamage"); _edgeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "edgeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_triggered) return false;
            bool hasEnemy = false;
            for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; if (DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y) <= _explodeRadius) { hasEnemy = true; break; } }
            if (hasEnemy && _fuseTimer < 0) { _triggered = true; _fuseTimer = _fuseDuration; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_fuseTimer <= 0) return;
            _fuseTimer -= dt;
            if (_fuseTimer <= 0) { for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.State == UnitStateEnum.Dead) continue; float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y); if (d <= _explodeRadius) { float t = d / _explodeRadius; float dmg = Mathf.Lerp(_centerDamage, _edgeDamage, t); DamageSystem.DealDamage(ref u, dmg, DamageCategory.Explosion, ref unit, state.Units); } } MCFight.VFXSpriteView.Play("bigexplosion", unit.X, unit.Y, 400f, 2f); unit.Hp = 0; unit.State = UnitStateEnum.Dead; }
        }
        public float GetEngageRange(ref UnitState unit) => _explodeRadius;
        public bool IsBusy(ref UnitState unit) => _fuseTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class DreadLichAbility : IAbilityComponent
    {
        private float _summonCd = 0f;
        private string _mid;
        private float _summonCooldown, _rangedDamage, _mmDamage;
        public DreadLichAbility(MonsterDefSO def) { _mid = def.monsterId; _summonCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "summonCooldown"); _rangedDamage = MonsterConfigLoader.GetAbilityParam(_mid, "rangedDamage"); _mmDamage = MonsterConfigLoader.GetAbilityParam(_mid, "mmDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_summonCd > 0) _summonCd -= dt;
            if (_summonCd <= 0) { SummonMinion(ref unit, state, unit.X + (float)(state.RNG.NextDouble() - 0.5) * 60f, unit.Y + (float)(state.RNG.NextDouble() - 0.5) * 60f); _summonCd = _summonCooldown; return false; }
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (dist > 200f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            float dmg = target.MoveType == MoveType.Fly ? _rangedDamage : _mmDamage;
            float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy);
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, dmg, 200f); proj.Speed = 240f; state.Projectiles.Add(proj);
            unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        void SummonMinion(ref UnitState unit, BattleState state, float x, float y) { string[] ids = { "dread_thrall", "dread_beast", "dread_ghoul", "dread_spider" }; string id = ids[state.RNG.Next(4)]; var minion = new UnitState { Id = state.NextId(), Team = unit.Team, MonsterId = id, X = x, Y = y, Facing = unit.Facing, Hp = 30, MaxHp = 30, Attack = 5, Armor = 2, MoveSpeed = 60, AttackRange = 42, AttackInterval = 1f, Radius = 14, MoveType = MoveType.Ground, AttackType = AttackType.Melee, State = UnitStateEnum.Idle, BaseMoveSpeed = 60, BaseAttackInterval = 1f, TargetId = -1, RiderUnitId = -1, MountUnitId = -1, RetargetTimer = BattleConstants.TARGET_RETARGET_INTERVAL, Tags = new[] { "summoned" } }; minion.StatusEffects = new StatusEffectList(); minion.SkillState = new SkillStateMap(); state.Units.Add(minion); }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 200f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class WadjetAbility : IAbilityComponent
    {
        private float _obeliskCd = 0f; private bool _useTornado = false; private float _castTimer = 0f;
        private int _pendingSkill = -1; private float _sweepHitTimer = 0f;
        private string _mid;
        private float _sweepDamage, _sweepRadius, _tornadoDamage, _obeliskDamage, _obeliskCooldown;
        public WadjetAbility(MonsterDefSO def) { _mid = def.monsterId; _sweepDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sweepDamage"); _sweepRadius = MonsterConfigLoader.GetAbilityParam(_mid, "sweepRadius"); _tornadoDamage = MonsterConfigLoader.GetAbilityParam(_mid, "tornadoDamage"); _obeliskDamage = MonsterConfigLoader.GetAbilityParam(_mid, "obeliskDamage"); _obeliskCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "obeliskCooldown"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_obeliskCd > 0) _obeliskCd -= dt;
            if (_castTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;

            float pad = target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            float obeliskRange = 290f + pad; // maxR(240) + hitRadius(50)
            float meleeRange = _sweepRadius + pad;
            bool isFly = target.MoveType == MoveType.Fly;

            // 优先：石碑（冷却好且敌人在命中范围内，对地对空均可）
            if (_obeliskCd <= 0 && dist <= obeliskRange)
            {
                _pendingSkill = 2; _castTimer = 3f; _obeliskCd = _obeliskCooldown;
                _sweepHitTimer = -1;
                unit.State = UnitStateEnum.Attack;
                return true;
            }

            // 石碑冷却中：地面敌人交替二连斩/龙卷，空军只用龙卷
            if (isFly)
            {
                // 空军：龙卷（投射物，射程 240）
                if (dist <= unit.AttackRange + pad)
                {
                    _pendingSkill = 1; _castTimer = 3f; _sweepHitTimer = -1;
                    unit.State = UnitStateEnum.Attack;
                    return true;
                }
            }
            else
            {
                // 地面：近战范围交替二连斩/龙卷
                if (dist <= meleeRange)
                {
                    _pendingSkill = _useTornado ? 1 : 0;
                    _useTornado = !_useTornado;
                    _castTimer = 3f; _sweepHitTimer = -1;
                    unit.State = UnitStateEnum.Attack;
                    return true;
                }
            }

            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return;
            float elapsed = 3f - _castTimer; _castTimer -= dt;
            if (_pendingSkill == 0) { if (elapsed < 0.1f && _sweepHitTimer < 0) { _sweepHitTimer = 0.1f; int tIdx0 = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx0 >= 0) { ref var t0 = ref state.Units[tIdx0]; float aim0 = Mathf.Atan2(t0.Y - unit.Y, t0.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim0, 45f * Mathf.Deg2Rad, _sweepRadius, state.Units, _sweepDamage, DamageCategory.Melee, null, true); float dx0 = t0.X - unit.X, dy0 = t0.Y - unit.Y; float d0 = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0); MCFight.SlashView.Play(unit.X, unit.Y, d0 > 0.01f ? dx0 / d0 : 1f, d0 > 0.01f ? dy0 / d0 : 0f, 72f, 0.4f); } } if (elapsed >= 1.5f && _sweepHitTimer > 0) { _sweepHitTimer = -1; int tIdx1 = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx1 >= 0) { ref var t1 = ref state.Units[tIdx1]; float aim1 = Mathf.Atan2(t1.Y - unit.Y, t1.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim1, 45f * Mathf.Deg2Rad, _sweepRadius, state.Units, _sweepDamage, DamageCategory.Melee, null, true); float dx1 = t1.X - unit.X, dy1 = t1.Y - unit.Y; float d1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1); MCFight.SlashView.Play(unit.X, unit.Y, d1 > 0.01f ? dx1 / d1 : 1f, d1 > 0.01f ? dy1 / d1 : 0f, 72f, 0.4f); } } }
            if (_pendingSkill == 1 && elapsed >= 1.5f && _sweepHitTimer < 0) { _sweepHitTimer = 0; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _tornadoDamage, 9999f); proj.Kind = ProjectileKind.ForsakenSonic; proj.PierceHalfWidth = 34f; proj.HitEnemyIds = new List<int>(); proj.Speed = 210f; proj.MaxTravel = 0; state.Projectiles.Add(proj); } }
            if (_pendingSkill == 2 && elapsed >= 1.5f && _sweepHitTimer < 0) { _sweepHitTimer = 0; int rings = 5; float maxR = 240f; var hitIds = new List<int>(); for (int r = 0; r < rings; r++) { float radius = r * (maxR / (rings - 1)); int count = Mathf.Max(1, r * 2 + 1); for (int a = 0; a < count; a++) { float angle = (a * Mathf.PI * 2f / count) + r * 0.3f; float ox = unit.X + Mathf.Cos(angle) * radius; float oy = unit.Y + Mathf.Sin(angle) * radius; MCFight.VFXSpriteView.Play("obelisk", ox, oy, 100f, 0.8f); for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; if (hitIds.Contains(u.Id)) continue; float d = DamageSystem.Dist(ox, oy, u.X, u.Y); if (d <= 50f + u.Radius) { DamageSystem.DealDamage(ref u, _obeliskDamage, DamageCategory.Melee, ref unit, state.Units); hitIds.Add(u.Id); } } } } }
            if (_castTimer <= 0) { _pendingSkill = -1; _sweepHitTimer = 0; unit.AttackCooldown = 0.5f; }
        }
        public float GetEngageRange(ref UnitState unit) => 290f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class FrostmawAbility : IAbilityComponent
    {
        private float _slamCd = 0f; private float _castTimer = 0f; private float _tickTimer = 0f;
        private int _ticksDone = 0; private int _pendingSkill = -1;
        private string _mid;
        private float _slamDamage, _slamRadius, _slamCooldown, _frostDamage, _frostRadius, _projectileDamage;
        public FrostmawAbility(MonsterDefSO def) { _mid = def.monsterId; _slamDamage = MonsterConfigLoader.GetAbilityParam(_mid, "slamDamage"); _slamRadius = MonsterConfigLoader.GetAbilityParam(_mid, "slamRadius"); _slamCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "slamCooldown"); _frostDamage = MonsterConfigLoader.GetAbilityParam(_mid, "frostDamage"); _frostRadius = MonsterConfigLoader.GetAbilityParam(_mid, "frostRadius"); _projectileDamage = MonsterConfigLoader.GetAbilityParam(_mid, "projectileDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_slamCd > 0) _slamCd -= dt;
            if (_castTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            bool isFly = target.MoveType == MoveType.Fly;
            if (!isFly && _slamCd <= 0 && dist <= _slamRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { _pendingSkill = 3; _castTimer = 2f; _slamCd = _slamCooldown; unit.State = UnitStateEnum.Attack; return true; }
            var options = new List<int>();
            if (dist <= 220f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) options.Add(0);
            if (dist <= _frostRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD && !isFly) options.Add(1);
            if (dist <= Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD) options.Add(2);
            if (isFly) { options.Clear(); if (dist <= 220f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) options.Add(0); if (dist <= _frostRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) options.Add(1); }
            if (options.Count == 0) return false;
            _pendingSkill = options[state.RNG.Next(options.Count)];
            _castTimer = _pendingSkill == 1 ? 2f : (_pendingSkill == 3 ? 2f : 0f); _tickTimer = 0f; _ticksDone = 0;
            if (_pendingSkill == 0) { float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _projectileDamage, 220f, new[] { StatusEffectType.Freeze }); proj.Speed = 260f; state.Projectiles.Add(proj); unit.AttackCooldown = 0.6f * 3f; unit.State = UnitStateEnum.Attack; return true; }
            if (_pendingSkill == 2) { DamageSystem.DealDamage(ref target, 10f, DamageCategory.Melee, ref unit, state.Units); unit.AttackCooldown = 1f * 3f; unit.State = UnitStateEnum.Attack; return true; }
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return; _castTimer -= dt; _tickTimer += dt;
            if (_pendingSkill == 1) { if (_tickTimer >= 0.2f && _ticksDone < 10) { _tickTimer -= 0.2f; _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _frostRadius, state.Units, _frostDamage, DamageCategory.Ranged, new[] { StatusEffectType.Slow }, false); if (_ticksDone == 1) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); float targetAngle = 0f; if (tIdx >= 0) { ref var target = ref state.Units[tIdx]; float dx = target.X - unit.X, dy = target.Y - unit.Y;                     targetAngle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f; } MCFight.VFXSpriteView.Play("icemist", unit.X, unit.Y, 140f, 2f, targetAngle); } } }
            if (_pendingSkill == 3 && _castTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _slamRadius, state.Units, _slamDamage, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _slamRadius, 0.5f)); VFXSpriteView.Play("closeaoe", unit.X, unit.Y, 180f, 1f); }
            if (_castTimer <= 0) { _pendingSkill = -1; unit.AttackCooldown = 0.5f; }
        }
        public float GetEngageRange(ref UnitState unit) => 220f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class AlphaYetiAbility : IAbilityComponent
    {
        private float _frenzyCd = 0f; private float _frenzyTimer = 0f; private float _frenzyTickTimer = 0f; private int _frenzyTicksDone = 0;
        private string _mid;
        private float _frenzyDamage, _frenzyRadius, _frostZoneRadius, _frostZoneDuration, _iceBombDamage, _iceBombRadius;
        private int _frenzyTicks;
        public AlphaYetiAbility(MonsterDefSO def) { _mid = def.monsterId; _frenzyDamage = MonsterConfigLoader.GetAbilityParam(_mid, "frenzyDamage"); _frenzyRadius = MonsterConfigLoader.GetAbilityParam(_mid, "frenzyRadius"); _frenzyTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "frenzyTicks"); _frostZoneRadius = MonsterConfigLoader.GetAbilityParam(_mid, "frostZoneRadius"); _frostZoneDuration = MonsterConfigLoader.GetAbilityParam(_mid, "frostZoneDuration"); _iceBombDamage = MonsterConfigLoader.GetAbilityParam(_mid, "iceBombDamage"); _iceBombRadius = MonsterConfigLoader.GetAbilityParam(_mid, "iceBombRadius"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_frenzyCd > 0) _frenzyCd -= dt;
            if (_frenzyTimer > 0) return true;
            if (unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (dist <= _frenzyRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD && _frenzyCd <= 0 && target.MoveType == MoveType.Ground) { _frenzyTimer = 3f; _frenzyCd = 10f; _frenzyTicksDone = 0; _frenzyTickTimer = 0; unit.State = UnitStateEnum.Attack; return true; }
            if (dist > 220f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy);
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 0f, 220f);
            proj.Speed = 200f; proj.ExplodeRadius = _iceBombRadius; proj.RawDamage = _iceBombDamage; state.Projectiles.Add(proj);
            state.AreaEffects.Add(AreaEffectSystem.CreateFrostZone(state.NextId(), unit.Team, target.X, target.Y, _frostZoneRadius, _frostZoneDuration, 2f));
            unit.AttackCooldown = 2f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_frenzyTimer <= 0) return; _frenzyTimer -= dt; _frenzyTickTimer += dt;
            if (_frenzyTickTimer >= 1f && _frenzyTicksDone < _frenzyTicks) { _frenzyTickTimer -= 1f; _frenzyTicksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _frenzyRadius, state.Units, _frenzyDamage, DamageCategory.Melee, new[] { StatusEffectType.Slow }, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _frenzyRadius, 0.3f)); }
            if (_frenzyTimer <= 0) unit.AttackCooldown = 0.5f;
        }
        public float GetEngageRange(ref UnitState unit) => 220f;
        public bool IsBusy(ref UnitState unit) => _frenzyTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class ProwlerAbility : IAbilityComponent
    {
        private int _skillIdx = 0; private float _castTimer = 0f; private float _tickTimer = 0f;
        private int _ticksDone = 0; private int _pendingSkill = -1;
        private int _beamVisualId = -1;
        private string _mid;
        private float _sweepDamage, _sweepRadius, _spinDamage, _spinRadius, _missileDamage, _beamDamage, _beamDamagePct, _beamLength, _beamHalfWidth;
        public ProwlerAbility(MonsterDefSO def) { _mid = def.monsterId; _sweepDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sweepDamage"); _sweepRadius = MonsterConfigLoader.GetAbilityParam(_mid, "sweepRadius"); _spinDamage = MonsterConfigLoader.GetAbilityParam(_mid, "spinDamage"); _spinRadius = MonsterConfigLoader.GetAbilityParam(_mid, "spinRadius"); _missileDamage = MonsterConfigLoader.GetAbilityParam(_mid, "missileDamage"); _beamDamage = MonsterConfigLoader.GetAbilityParam(_mid, "beamDamage"); _beamDamagePct = MonsterConfigLoader.GetAbilityParam(_mid, "beamDamagePct"); _beamLength = MonsterConfigLoader.GetAbilityParam(_mid, "beamLength"); _beamHalfWidth = MonsterConfigLoader.GetAbilityParam(_mid, "beamHalfWidth"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_castTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            int idx = _skillIdx % 4; bool isFly = target.MoveType == MoveType.Fly;
            if (idx == 3) { if (dist > _beamLength + target.Radius * BattleConstants.TARGET_RADIUS_PAD) return false; _pendingSkill = 3; _castTimer = 1.5f; _tickTimer = 0; _ticksDone = 0; _beamVisualId = state.NextId(); float dx0 = target.X - unit.X, dy0 = target.Y - unit.Y; float d0 = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0); state.ActiveBeams.Add(new ActiveBeamData { Id = _beamVisualId, Team = unit.Team, SourceId = unit.Id, TargetId = target.Id, OriginX = unit.X, OriginY = unit.Y, DirX = d0 > 0.01f ? dx0 / d0 : 1f, DirY = d0 > 0.01f ? dy0 / d0 : 0f, Length = _beamLength, HalfWidth = _beamHalfWidth, Remaining = 1.5f, SourceMonsterId = unit.MonsterId, Kind = BeamKind.ProwlerRay }); }
            else if (isFly) { _pendingSkill = 2; _castTimer = 0.3f; }
            else { var opts = new List<int>(); if (dist <= _sweepRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) opts.Add(0); if (dist <= _spinRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) opts.Add(1); opts.Add(2); _pendingSkill = opts[state.RNG.Next(opts.Count)]; _castTimer = _pendingSkill == 0 ? 0.5f : (_pendingSkill == 1 ? 2f : 0.3f); _tickTimer = 0; _ticksDone = 0; }
            _skillIdx++; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return; _castTimer -= dt; _tickTimer += dt;
            if (_pendingSkill == 0) { if (_ticksDone == 0 && _tickTimer >= 0.5f) { _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _sweepRadius, state.Units, _sweepDamage, DamageCategory.Melee, null, true); } }
            if (_pendingSkill == 1) { if (_tickTimer >= 0.5f && _ticksDone < 4) { _tickTimer -= 0.5f; _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _spinRadius, state.Units, _spinDamage, DamageCategory.Melee, null, true); } }
            if (_pendingSkill == 2) { if (_ticksDone == 0) { _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; for (int i = 0; i < 3; i++) { float angle = Mathf.Atan2(t.Y - unit.Y, t.X - unit.X) + (i - 1) * 0.8f; var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, Mathf.Cos(angle), Mathf.Sin(angle), unit.Id, unit.MonsterId, _missileDamage, 9999f, new[] { StatusEffectType.Wither }); proj.Kind = ProjectileKind.ProwlerMissile; proj.TargetId = t.Id; proj.ExplodeRadius = 24f; proj.Speed = 250f; proj.HomingSteer = 4.5f * dt; proj.MaxTravel = 0; state.Projectiles.Add(proj); } } } }
            if (_pendingSkill == 3) { for (int i = 0; i < state.ActiveBeams.Count; i++) { if (state.ActiveBeams[i].Id == _beamVisualId) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var beam = state.ActiveBeams[i]; beam.OriginX = unit.X; beam.OriginY = unit.Y; beam.DirX = d > 0.01f ? dx / d : 1f; beam.DirY = d > 0.01f ? dy / d : 0f; beam.Length = _beamLength; beam.Remaining = _castTimer; state.ActiveBeams[i] = beam; } break; } } if (_tickTimer >= 0.375f && _ticksDone < 4) { _tickTimer -= 0.375f; _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); float dirX = d > 0.01f ? dx / d : 1f, dirY = d > 0.01f ? dy / d : 0f; float dmg = _beamDamage + t.MaxHp * _beamDamagePct; AreaEffectSystem.DealBeamAoe(ref unit, unit.X, unit.Y, dirX, dirY, d, _beamHalfWidth, state.Units, dmg, DamageCategory.Beam, null, false); } } }
            if (_castTimer <= 0) { if (_pendingSkill == 3) { for (int i = state.ActiveBeams.Count - 1; i >= 0; i--) if (state.ActiveBeams[i].Id == _beamVisualId) { state.ActiveBeams.RemoveAt(i); break; } _beamVisualId = -1; } _pendingSkill = -1; unit.AttackCooldown = 3f; }
        }
        public float GetEngageRange(ref UnitState unit) => 240f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class ForsakenAbility : IAbilityComponent
    {
        private float _castTimer = 0f; private float _tickTimer = 0f; private int _ticksDone = 0;
        private int _pendingSkill = -1; private float _leapCd = 0f; private float _leapTimer = 0f;
        private float _leapFromX, _leapFromY, _leapToX, _leapToY; private float _regenAccum = 0f;
        private string _mid;
        private int _sonicTicks;
        private float _leapCooldown, _regenPerSec, _biteDamage, _hammerDamage, _sonicDamage, _sonicRadius, _arcDamage;
        public ForsakenAbility(MonsterDefSO def) { _mid = def.monsterId; _leapCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "leapCooldown"); _regenPerSec = MonsterConfigLoader.GetAbilityParam(_mid, "regenPerSec"); _biteDamage = MonsterConfigLoader.GetAbilityParam(_mid, "biteDamage"); _hammerDamage = MonsterConfigLoader.GetAbilityParam(_mid, "hammerDamage"); _sonicDamage = MonsterConfigLoader.GetAbilityParam(_mid, "sonicDamage"); _sonicRadius = MonsterConfigLoader.GetAbilityParam(_mid, "sonicRadius"); _sonicTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "sonicTicks"); _arcDamage = MonsterConfigLoader.GetAbilityParam(_mid, "arcDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_leapCd > 0) _leapCd -= dt;
            _regenAccum += dt; if (_regenAccum >= 1f) { _regenAccum -= 1f; unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + _regenPerSec); }
            if (_leapTimer > 0 || _castTimer > 0) return true;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (dist > 100f && _leapCd <= 0) { _leapTimer = 1f; _leapCd = _leapCooldown; _leapFromX = unit.X; _leapFromY = unit.Y; _leapToX = target.X; _leapToY = target.Y; unit.State = UnitStateEnum.Attack; return true; }
            if (unit.AttackCooldown > 0) return false;
            bool isFly = target.MoveType == MoveType.Fly; var opts = new List<int>();
            if (dist <= Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD) opts.Add(0);
            if (!isFly && dist <= Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD) opts.Add(1);
            if (dist <= _sonicRadius + target.Radius * BattleConstants.TARGET_RADIUS_PAD) opts.Add(2);
            if (dist >= 36f) opts.Add(3);
            if (isFly) opts.RemoveAll(o => o == 1);
            if (opts.Count == 0) return false;
            _pendingSkill = opts[state.RNG.Next(opts.Count)];
            _castTimer = _pendingSkill == 0 ? 2f : (_pendingSkill == 2 ? 2f : (_pendingSkill == 3 ? 0.45f : 0.35f)); _tickTimer = 0; _ticksDone = 0;
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_leapTimer > 0) { _leapTimer -= dt; float t = 1f - _leapTimer / 1f; MovementSystem.SetLeapArcPosition(ref unit, _leapFromX, _leapFromY, _leapToX, _leapToY, t, 38f); if (_leapTimer <= 0) unit.AttackCooldown = 0.5f; return; }
            if (_castTimer <= 0) return; _castTimer -= dt; _tickTimer += dt;
            if (_pendingSkill == 0) { if (_tickTimer >= 1f && _ticksDone < 2) { _tickTimer -= 1f; _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; DamageSystem.DealDamage(ref t, _biteDamage, DamageCategory.Melee, ref unit, state.Units); VFXSpriteView.Play("closeaoe", t.X, t.Y, 48f, 0.5f); } } }
            if (_pendingSkill == 1) { if (_ticksDone == 0 && _tickTimer >= 0.35f) { _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; AreaEffectSystem.DealInstantAoe(ref unit, t.X, t.Y, 24f, state.Units, _hammerDamage, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, t.X, t.Y, 24f, 0.38f)); VFXSpriteView.Play("closeaoe", t.X, t.Y, 48f, 0.5f); } } }
            if (_pendingSkill == 2) { if (_tickTimer >= 0.5f && _ticksDone < _sonicTicks) { _tickTimer -= 0.5f; _ticksDone++; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _sonicRadius, state.Units, _sonicDamage, DamageCategory.Ranged, null, false); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _sonicRadius, 0.28f)); VFXSpriteView.Play("soundwave", unit.X, unit.Y, 64f, 0.6f); } }
            if (_pendingSkill == 3) { if (_ticksDone == 0 && _tickTimer >= 0.45f) { _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, _arcDamage, 9999f); proj.Kind = ProjectileKind.ForsakenSonic; proj.ArcRadius = 72f; proj.ArcHalfRad = 48f * Mathf.Deg2Rad; proj.PierceHalfWidth = 22f; proj.HitEnemyIds = new List<int>(); proj.Speed = 360f; proj.MaxTravel = 0; state.Projectiles.Add(proj); } } }
            if (_castTimer <= 0) { _pendingSkill = -1; unit.AttackCooldown = 0.5f; }
        }
        public float GetEngageRange(ref UnitState unit) => 200f;
        public bool IsBusy(ref UnitState unit) => _leapTimer > 0 || _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class KobolediatorAbility : IAbilityComponent
    {
        private bool _useTriple = true; private float _castTimer = 0f; private float _tickTimer = 0f;
        private int _ticksDone = 0; private int _pendingSkill = -1;
        private float _chargeTimer = 0f; private float _chargeFromX, _chargeFromY, _chargeToX, _chargeToY;
        private string _mid;
        private float _chargeDamage, _chargeRadius, _slashDamage, _slashRadius, _finishDamage, _bigSlashDamage, _bigSlashRadius;
        public KobolediatorAbility(MonsterDefSO def) { _mid = def.monsterId; _chargeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "chargeDamage"); _chargeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "chargeRadius"); _slashDamage = MonsterConfigLoader.GetAbilityParam(_mid, "slashDamage"); _slashRadius = MonsterConfigLoader.GetAbilityParam(_mid, "slashRadius"); _finishDamage = MonsterConfigLoader.GetAbilityParam(_mid, "finishDamage"); _bigSlashDamage = MonsterConfigLoader.GetAbilityParam(_mid, "bigSlashDamage"); _bigSlashRadius = MonsterConfigLoader.GetAbilityParam(_mid, "bigSlashRadius"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_castTimer > 0 || _chargeTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (target.MoveType == MoveType.Fly) return false;
            if (dist >= 120f) { _pendingSkill = 0; _chargeTimer = 2f; _chargeFromX = unit.X; _chargeFromY = unit.Y; _chargeToX = target.X; _chargeToY = target.Y; unit.State = UnitStateEnum.Attack; return true; }
            _pendingSkill = _useTriple ? 1 : 2; _useTriple = !_useTriple;
            _castTimer = _pendingSkill == 1 ? 3f : 2f; _tickTimer = 0; _ticksDone = 0;
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_chargeTimer > 0) { _chargeTimer -= dt; float t = 1f - _chargeTimer / 2f; unit.X = Mathf.Lerp(_chargeFromX, _chargeToX, t); unit.Y = Mathf.Lerp(_chargeFromY, _chargeToY, t); DamageSystem.ClampToField(ref unit); if (_chargeTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _chargeRadius, state.Units, _chargeDamage, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _chargeRadius, 0.4f)); VFXSpriteView.Play("shockwave", unit.X, unit.Y, 144f, 0.6f); unit.AttackCooldown = 0.5f; } return; }
            if (_castTimer <= 0) return; _castTimer -= dt; _tickTimer += dt;
            if (_pendingSkill == 1) { if (_ticksDone == 0) { _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float aim = Mathf.Atan2(t.Y - unit.Y, t.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim, 45f * Mathf.Deg2Rad, _slashRadius, state.Units, _slashDamage, DamageCategory.Melee, null, true); float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); MCFight.SlashView.Play(unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, 72f, 0.4f); } } else if (_ticksDone == 1 && _tickTimer >= 1.5f) { _ticksDone++; _tickTimer -= 1.5f; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float aim = Mathf.Atan2(t.Y - unit.Y, t.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim, 45f * Mathf.Deg2Rad, _slashRadius, state.Units, _slashDamage, DamageCategory.Melee, null, true); float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); MCFight.SlashView.Play(unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, 72f, 0.4f); } } else if (_ticksDone == 2 && _castTimer <= 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float aim = Mathf.Atan2(t.Y - unit.Y, t.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim, 45f * Mathf.Deg2Rad, _slashRadius, state.Units, _finishDamage, DamageCategory.Melee, null, true); float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); MCFight.SlashView.Play(unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, 72f, 0.5f); } } }
            if (_pendingSkill == 2 && _castTimer <= 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float aim = Mathf.Atan2(t.Y - unit.Y, t.X - unit.X); AreaEffectSystem.DealSectorAoe(ref unit, unit.X, unit.Y, aim, 60f * Mathf.Deg2Rad, _bigSlashRadius, state.Units, _bigSlashDamage, DamageCategory.Melee, null, true); float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); MCFight.SlashView.Play(unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, 100f, 0.6f); } }
            if (_castTimer <= 0) { _pendingSkill = -1; unit.AttackCooldown = 0.5f; }
        }
        public float GetEngageRange(ref UnitState unit) => 260f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0 || _chargeTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class TremorzillaAbility : IAbilityComponent
    {
        private float _beamCd = 0f; private float _beamTimer = 0f; private float _beamTickTimer = 0f;
        private int _beamTicksDone = 0; private int _beamVisualId = -1;
        private string _mid;
        private int _beamTicks;
        private float _beamCooldown, _beamDuration, _beamDamage, _beamHalfWidth, _beamLength, _stompDamage, _stompRadius, _beamTickInterval;
        public TremorzillaAbility(MonsterDefSO def) { _mid = def.monsterId; _beamCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "beamCooldown"); _beamDuration = MonsterConfigLoader.GetAbilityParam(_mid, "beamDuration"); _beamDamage = MonsterConfigLoader.GetAbilityParam(_mid, "beamDamage"); _beamTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "beamTicks"); _beamHalfWidth = MonsterConfigLoader.GetAbilityParam(_mid, "beamHalfWidth"); _beamLength = MonsterConfigLoader.GetAbilityParam(_mid, "beamLength"); _stompDamage = MonsterConfigLoader.GetAbilityParam(_mid, "stompDamage"); _stompRadius = MonsterConfigLoader.GetAbilityParam(_mid, "stompRadius"); _beamTickInterval = _beamDuration / _beamTicks; }
        public void OnInit(ref UnitState unit) { _beamCd = 0f; }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_beamCd > 0) _beamCd -= dt;
            if (_beamTimer > 0) return true;
            if (unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (_beamCd <= 0 && dist <= 280f + target.Radius * BattleConstants.TARGET_RADIUS_PAD)
            {
                _beamTimer = _beamDuration; _beamCd = _beamCooldown; _beamTicksDone = 0; _beamTickTimer = 0;
                _beamVisualId = state.NextId();
                float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy);
                float dirX = d > 0.01f ? dx / d : 1f, dirY = d > 0.01f ? dy / d : 0f;
                state.ActiveBeams.Add(new ActiveBeamData { Id = _beamVisualId, Team = unit.Team, SourceId = unit.Id, TargetId = target.Id, OriginX = unit.X, OriginY = unit.Y, DirX = dirX, DirY = dirY, Length = _beamLength, HalfWidth = _beamHalfWidth, Remaining = _beamDuration, TicksRemaining = _beamTicks, DamagePerTick = _beamDamage, SourceMonsterId = unit.MonsterId, Kind = BeamKind.Tremor });
                unit.State = UnitStateEnum.Attack;
                return true;
            }
            float range = Mathf.Max(58f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist <= range && TargetingSystem.CanTargetForAttack(ref unit, ref target, false))
            { float aoeR = _stompRadius; AreaEffectSystem.DealInstantAoe(ref unit, target.X, target.Y, aoeR, state.Units, _stompDamage, DamageCategory.Melee, new[] { StatusEffectType.Poison }, false); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, target.X, target.Y, aoeR)); unit.AttackCooldown = 1f; unit.AttackAnimTimer = BattleConstants.AOE_ANIM_TIME; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_beamTimer <= 0) return;
            _beamTimer -= dt; _beamTickTimer += dt;
            for (int i = 0; i < state.ActiveBeams.Count; i++) { if (state.ActiveBeams[i].Id == _beamVisualId) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var beam = state.ActiveBeams[i]; beam.OriginX = unit.X; beam.OriginY = unit.Y; beam.DirX = d > 0.01f ? dx / d : 1f; beam.DirY = d > 0.01f ? dy / d : 0f; beam.Length = _beamLength; beam.Remaining = _beamTimer; state.ActiveBeams[i] = beam; } break; } }
            if (_beamTickTimer >= _beamTickInterval && _beamTicksDone < _beamTicks) { _beamTickTimer -= _beamTickInterval; _beamTicksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); float dirX = d > 0.01f ? dx / d : 1f, dirY = d > 0.01f ? dy / d : 0f; AreaEffectSystem.DealBeamAoe(ref unit, unit.X, unit.Y, dirX, dirY, _beamLength, _beamHalfWidth, state.Units, _beamDamage, DamageCategory.Beam, null, false); } }
            if (_beamTimer <= 0) { unit.AttackCooldown = 0.5f; for (int i = state.ActiveBeams.Count - 1; i >= 0; i--) if (state.ActiveBeams[i].Id == _beamVisualId) { state.ActiveBeams.RemoveAt(i); break; } _beamVisualId = -1; }
        }
        public float GetEngageRange(ref UnitState unit) => 280f;
        public bool IsBusy(ref UnitState unit) => _beamTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class LuxtructosaurusAbility : IAbilityComponent
    {
        private bool _nextStomp = false; private float _leapCd = 0f; private float _meteorTimer = 0f;
        private float _castTimer = 0f; private int _pendingSkill = -1;
        private string _mid;
        private float _leapCooldown, _leapDamage, _leapRadius, _stompDamage, _stompRadius, _meteorInterval, _meteorDamage, _meteorRadius, _lavaDuration, _lavaDPS;
        public LuxtructosaurusAbility(MonsterDefSO def) { _mid = def.monsterId; _leapCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "leapCooldown"); _leapDamage = MonsterConfigLoader.GetAbilityParam(_mid, "leapDamage"); _leapRadius = MonsterConfigLoader.GetAbilityParam(_mid, "leapRadius"); _stompDamage = MonsterConfigLoader.GetAbilityParam(_mid, "stompDamage"); _stompRadius = MonsterConfigLoader.GetAbilityParam(_mid, "stompRadius"); _meteorInterval = MonsterConfigLoader.GetAbilityParam(_mid, "meteorInterval"); _meteorDamage = MonsterConfigLoader.GetAbilityParam(_mid, "meteorDamage"); _meteorRadius = MonsterConfigLoader.GetAbilityParam(_mid, "meteorRadius"); _lavaDuration = MonsterConfigLoader.GetAbilityParam(_mid, "lavaDuration"); _lavaDPS = MonsterConfigLoader.GetAbilityParam(_mid, "lavaDPS"); _meteorTimer = _meteorInterval; }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_leapCd > 0) _leapCd -= dt;
            if (_castTimer > 0) return true;
            if (unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (dist > 100f && dist <= 340f + target.Radius * BattleConstants.TARGET_RADIUS_PAD && _leapCd <= 0) { _pendingSkill = 0; _castTimer = 0.84f; _leapCd = _leapCooldown; unit.SkillState.SetFloat("lux_leap_from_x".GetHashCode(), unit.X); unit.SkillState.SetFloat("lux_leap_from_y".GetHashCode(), unit.Y); unit.SkillState.SetFloat("lux_leap_to_x".GetHashCode(), target.X); unit.SkillState.SetFloat("lux_leap_to_y".GetHashCode(), target.Y); unit.State = UnitStateEnum.Attack; return true; }
            float range = Mathf.Max(55f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist <= range) { _pendingSkill = _nextStomp ? 2 : 1; _nextStomp = !_nextStomp; _castTimer = 0.7f; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            _meteorTimer -= dt; if (_meteorTimer <= 0) { _meteorTimer = _meteorInterval; float angle = (float)(state.RNG.NextDouble() * Mathf.PI * 2); float r = (float)state.RNG.NextDouble() * 200f; float mx = unit.X + Mathf.Cos(angle) * r; float my = unit.Y + Mathf.Sin(angle) * r; AreaEffectSystem.DealInstantAoe(ref unit, mx, my, _meteorRadius, state.Units, _meteorDamage, DamageCategory.Ranged, new[] { StatusEffectType.Burn }, false); state.AreaEffects.Add(AreaEffectSystem.CreateLava(state.NextId(), unit.Team, mx, my, _meteorRadius, _lavaDuration, _lavaDPS)); MCFight.VFXSpriteView.Play("meteor", mx, my, 106f, 1.5f); MCFight.VFXSpriteView.Play("lava_circle", mx, my, _meteorRadius * 2f, _lavaDuration); }
            if (_castTimer <= 0) return; _castTimer -= dt;
            if (_pendingSkill == 0) { float t = 1f - _castTimer / 0.84f; float fx = unit.SkillState.GetFloat("lux_leap_from_x".GetHashCode(), unit.X); float fy = unit.SkillState.GetFloat("lux_leap_from_y".GetHashCode(), unit.Y); float tx = unit.SkillState.GetFloat("lux_leap_to_x".GetHashCode(), unit.X); float ty = unit.SkillState.GetFloat("lux_leap_to_y".GetHashCode(), unit.Y); MovementSystem.SetLeapArcPosition(ref unit, fx, fy, tx, ty, t, 55f); if (_castTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _leapRadius, state.Units, _leapDamage, DamageCategory.Melee, new[] { StatusEffectType.Burn }, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _leapRadius, 0.42f)); unit.AttackCooldown = 0.5f; } }
            if (_pendingSkill == 1 && _castTimer <= 0) { AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _stompRadius, state.Units, _stompDamage, DamageCategory.Melee, new[] { StatusEffectType.Burn }, true); unit.AttackCooldown = 2.2f; }
            if (_pendingSkill == 2 && _castTimer <= 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var tg = ref state.Units[tIdx]; AreaEffectSystem.DealInstantAoe(ref unit, tg.X, tg.Y, 96f, state.Units, _stompDamage, DamageCategory.Melee, new[] { StatusEffectType.Burn }, true); } unit.AttackCooldown = 2.2f; }
            if (_castTimer <= 0) _pendingSkill = -1;
        }
        public float GetEngageRange(ref UnitState unit) => 340f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }

    public class RemnantAbility : IAbilityComponent
    {
        private float _castTimer = 0f; private float _obeliskCd = 0f; private int _pendingSkill = -1; private bool _hasSandstorm = false;
        private string _mid;
        private int _tornadoCount, _obeliskRings;
        private float _biteDamage, _biteDamagePct, _stompDamage, _stompRadius, _tornadoDamage, _tornadoRadius, _obeliskDamage, _obeliskDamagePct, _obeliskCooldown;
        public RemnantAbility(MonsterDefSO def) { _mid = def.monsterId; _biteDamage = MonsterConfigLoader.GetAbilityParam(_mid, "biteDamage"); _biteDamagePct = MonsterConfigLoader.GetAbilityParam(_mid, "biteDamagePct"); _stompDamage = MonsterConfigLoader.GetAbilityParam(_mid, "stompDamage"); _stompRadius = MonsterConfigLoader.GetAbilityParam(_mid, "stompRadius"); _tornadoCount = MonsterConfigLoader.GetAbilityParamInt(_mid, "tornadoCount"); _tornadoDamage = MonsterConfigLoader.GetAbilityParam(_mid, "tornadoDamage"); _tornadoRadius = MonsterConfigLoader.GetAbilityParam(_mid, "tornadoRadius"); _obeliskDamage = MonsterConfigLoader.GetAbilityParam(_mid, "obeliskDamage"); _obeliskDamagePct = MonsterConfigLoader.GetAbilityParam(_mid, "obeliskDamagePct"); _obeliskRings = MonsterConfigLoader.GetAbilityParamInt(_mid, "obeliskRings"); _obeliskCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "obeliskCooldown"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_obeliskCd > 0) _obeliskCd -= dt;
            if (_castTimer > 0 || unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            float pad = target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            float meleeDist = Mathf.Max(55f, unit.Radius + target.Radius) + pad;
            float stompDist = _stompRadius + pad;
            float sandstormReach = 96f + _tornadoRadius + pad; // orbitRadius + tornadoRadius
            float obeliskReach = 290f + pad;

            var opts = new List<int>();
            if (target.MoveType == MoveType.Ground)
            {
                if (dist <= meleeDist) opts.Add(0); // 撕咬
                if (dist <= stompDist) opts.Add(1); // 甩尾(践踏)
                opts.Add(3); // 践踏(目标位置AOE，射程远)
            }
            if (dist <= sandstormReach) opts.Add(2); // 沙暴(仅在龙卷风能接触到的距离)
            if (_obeliskCd <= 0 && dist <= obeliskReach) opts.Add(4); // 石碑弹幕

            if (opts.Count == 0) return false;
            _pendingSkill = opts[state.RNG.Next(opts.Count)]; _castTimer = 2f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return; float elapsed = 2f - _castTimer; _castTimer -= dt;
            if (_pendingSkill == 0 && elapsed >= 1.0f && !_hasSandstorm) { _hasSandstorm = true; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dmg = _biteDamage + t.MaxHp * _biteDamagePct; DamageSystem.DealDamage(ref t, dmg, DamageCategory.Melee, ref unit, state.Units); } }
            if (_pendingSkill == 1 && elapsed >= 1.0f && !_hasSandstorm) { _hasSandstorm = true; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _stompRadius, state.Units, _stompDamage, DamageCategory.Melee, null, true); state.AreaEffects.Add(AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _stompRadius, 0.45f)); VFXSpriteView.Play("shockwave", unit.X, unit.Y, _stompRadius * 2f, 0.6f); }
            if (_pendingSkill == 2 && elapsed >= 1.0f && !_hasSandstorm) { _hasSandstorm = true; for (int i = 0; i < _tornadoCount; i++) { var eff = AreaEffectSystem.CreateShockwave(state.NextId(), unit.Team, unit.X, unit.Y, _tornadoRadius, 15f); eff.Type = AreaEffectType.SandTornado; eff.OrbitRadius = 96f; eff.OrbitAngle = i * (Mathf.PI * 2f / _tornadoCount); eff.AngularSpeed = 2.2f; eff.Damage = _tornadoDamage; eff.X = unit.X + Mathf.Cos(eff.OrbitAngle) * eff.OrbitRadius; eff.Y = unit.Y + Mathf.Sin(eff.OrbitAngle) * eff.OrbitRadius; state.AreaEffects.Add(eff); } }
            if (_pendingSkill == 3 && elapsed >= 1.0f && !_hasSandstorm) { _hasSandstorm = true; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dmg = 23f + t.MaxHp * 0.035f; AreaEffectSystem.DealInstantAoe(ref unit, t.X, t.Y, 100f, state.Units, dmg, DamageCategory.Melee, null, true); } VFXSpriteView.Play("shockwave", unit.X, unit.Y, 200f, 1f); }
            if (_pendingSkill == 4 && elapsed >= 1.0f && !_hasSandstorm) { _hasSandstorm = true; _obeliskCd = _obeliskCooldown; var hitIds = new List<int>(); for (int r = 0; r < _obeliskRings; r++) { float radius = r * (240f / (_obeliskRings - 1)); int count = Mathf.Max(1, r + 1); for (int a = 0; a < count; a++) { float angle = (a * Mathf.PI * 2f / count) + r * 0.3f; float ox = unit.X + Mathf.Cos(angle) * radius; float oy = unit.Y + Mathf.Sin(angle) * radius; VFXSpriteView.Play("obelisk", ox, oy, 100f, 1.5f); for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; if (hitIds.Contains(u.Id)) continue; float d = DamageSystem.Dist(ox, oy, u.X, u.Y); if (d <= 50f + u.Radius) { float dmg = _obeliskDamage + u.MaxHp * _obeliskDamagePct; DamageSystem.DealDamage(ref u, dmg, DamageCategory.Melee, ref unit, state.Units); hitIds.Add(u.Id); } } } } }
            if (_castTimer <= 0) { _pendingSkill = -1; _hasSandstorm = false; }
        }
        public float GetEngageRange(ref UnitState unit) => 290f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class HarbingerAbility : IAbilityComponent
    {
        private int _attackMode = 0; private float _modeTimer = 0f; private int _skillIdx = 0;
        private float _skillTimer = 0f; private float _castTimer = 0f; private float _tickTimer = 0f;
        private int _ticksDone = 0; private int _pendingSkill = -1; private float _regenAccum = 0f;
        private int _beamVisualId = -1;
        private string _mid;
        private float _modeTimerCfg, _skillTimerCfg, _regenPerSec, _homingDamage, _rainRadius, _rainDamage, _beamDuration, _beamDamage, _beamDamagePct, _beamHalfWidth, _beamLength;
        private int _homingCount, _rainCount;
        public HarbingerAbility(MonsterDefSO def) { _mid = def.monsterId; _modeTimerCfg = MonsterConfigLoader.GetAbilityParam(_mid, "modeTimer"); _skillTimerCfg = MonsterConfigLoader.GetAbilityParam(_mid, "skillTimer"); _regenPerSec = MonsterConfigLoader.GetAbilityParam(_mid, "regenPerSec"); _homingCount = MonsterConfigLoader.GetAbilityParamInt(_mid, "homingCount"); _homingDamage = MonsterConfigLoader.GetAbilityParam(_mid, "homingDamage"); _rainCount = MonsterConfigLoader.GetAbilityParamInt(_mid, "rainCount"); _rainRadius = MonsterConfigLoader.GetAbilityParam(_mid, "rainRadius"); _rainDamage = MonsterConfigLoader.GetAbilityParam(_mid, "rainDamage"); _beamDuration = MonsterConfigLoader.GetAbilityParam(_mid, "beamDuration"); _beamDamage = MonsterConfigLoader.GetAbilityParam(_mid, "beamDamage"); _beamDamagePct = MonsterConfigLoader.GetAbilityParam(_mid, "beamDamagePct"); _beamHalfWidth = MonsterConfigLoader.GetAbilityParam(_mid, "beamHalfWidth"); _beamLength = MonsterConfigLoader.GetAbilityParam(_mid, "beamLength"); _modeTimer = _modeTimerCfg; _skillTimer = _skillTimerCfg; }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            _regenAccum += dt; if (_regenAccum >= 1f) { _regenAccum -= 1f; unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + _regenPerSec); }
            if (_castTimer > 0) return true;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            if (_skillTimer <= 0 && dist <= 240f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) { int idx = _skillIdx % 4; _pendingSkill = idx; _skillIdx++; _skillTimer = _skillTimerCfg; if (idx == 0) { for (int i = 0; i < _homingCount; i++) { float angle = (i / (float)_homingCount) * Mathf.PI * 0.9f - Mathf.PI * 0.45f; float baseAngle = Mathf.Atan2(target.Y - unit.Y, target.X - unit.X); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, Mathf.Cos(baseAngle + angle), Mathf.Sin(baseAngle + angle), unit.Id, unit.MonsterId, _homingDamage, 9999f, new[] { StatusEffectType.Wither }); proj.Kind = ProjectileKind.HarbHoming; proj.TargetId = target.Id; proj.ExplodeRadius = 42f; proj.Speed = 320f; proj.HomingSteer = 4.5f * dt; proj.MaxTravel = 0; state.Projectiles.Add(proj); } unit.State = UnitStateEnum.Attack; return true; } if (idx == 1) { for (int i = 0; i < _rainCount; i++) { float a = (float)(state.RNG.NextDouble() * Mathf.PI * 2f); float r = (float)state.RNG.NextDouble() * 160f; float gx = unit.X + Mathf.Cos(a) * r; float gy = unit.Y + Mathf.Sin(a) * r; AreaEffectSystem.DealInstantAoe(ref unit, gx, gy, _rainRadius, state.Units, _rainDamage, DamageCategory.Ranged, new[] { StatusEffectType.Wither }, false); } unit.State = UnitStateEnum.Attack; return true; } if (idx == 2) { _castTimer = 0.55f; unit.State = UnitStateEnum.Attack; return true; } _castTimer = _beamDuration; _tickTimer = 0; _ticksDone = 0; _beamVisualId = state.NextId(); float dx0 = target.X - unit.X, dy0 = target.Y - unit.Y; float d0 = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0); state.ActiveBeams.Add(new ActiveBeamData { Id = _beamVisualId, Team = unit.Team, SourceId = unit.Id, TargetId = target.Id, OriginX = unit.X, OriginY = unit.Y, DirX = d0 > 0.01f ? dx0 / d0 : 1f, DirY = d0 > 0.01f ? dy0 / d0 : 0f, Length = _beamLength, HalfWidth = _beamHalfWidth, Remaining = _beamDuration, SourceMonsterId = unit.MonsterId, Kind = BeamKind.HarbingerDeath }); unit.State = UnitStateEnum.Attack; return true; }
            if (unit.AttackCooldown > 0) return false;
            if (dist > 240f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) return false;
            if (_attackMode == 0) { float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 8f, 240f, new[] { StatusEffectType.Wither }); proj.ExplodeRadius = 48f; proj.Speed = 300f; state.Projectiles.Add(proj); unit.AttackCooldown = 2f; }
            else { float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 5f, 240f, new[] { StatusEffectType.Burn }); proj.Speed = 420f; state.Projectiles.Add(proj); unit.AttackCooldown = 1f; }
            unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            _modeTimer -= dt; if (_modeTimer <= 0) { _attackMode = 1 - _attackMode; _modeTimer = _modeTimerCfg; }
            if (_skillTimer > 0) _skillTimer -= dt;
            if (_castTimer <= 0) return; _castTimer -= dt;
            if (_pendingSkill == 2 && _castTimer <= 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dmg = 11f + t.MaxHp * 0.06f; DamageSystem.DealDamage(ref t, dmg, DamageCategory.Melee, ref unit, state.Units); } }
            if (_pendingSkill == 3)
            {
                for (int i = 0; i < state.ActiveBeams.Count; i++) { if (state.ActiveBeams[i].Id == _beamVisualId) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dx = t.X - unit.X, dy = t.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var beam = state.ActiveBeams[i]; beam.OriginX = unit.X; beam.OriginY = unit.Y; beam.DirX = d > 0.01f ? dx / d : 1f; beam.DirY = d > 0.01f ? dy / d : 0f; beam.Length = _beamLength; beam.Remaining = _castTimer; state.ActiveBeams[i] = beam; } break; } }
                _tickTimer += dt; if (_tickTimer >= 1f && _ticksDone < 5) { _tickTimer -= 1f; _ticksDone++; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float dmg = _beamDamage + t.MaxHp * _beamDamagePct; DamageSystem.DealDamage(ref t, dmg, DamageCategory.Beam, ref unit, state.Units); StatusEffectSystem.Apply(ref t, StatusEffectType.Burn); } }
            }
            if (_castTimer <= 0) { if (_pendingSkill == 3) { for (int i = state.ActiveBeams.Count - 1; i >= 0; i--) if (state.ActiveBeams[i].Id == _beamVisualId) { state.ActiveBeams.RemoveAt(i); break; } _beamVisualId = -1; } _pendingSkill = -1; }
        }
        public float GetEngageRange(ref UnitState unit) => 240f;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class NagaAbility : IAbilityComponent
    {
        private float _contactCd = 0f; private float _moveTimer = 0f; private float _moveTargetX, _moveTargetY;
        private string _mid;
        private float _contactDamage, _contactCooldown;
        private int _maxSegments, _minSegments;
        public NagaAbility(MonsterDefSO def) { _mid = def.monsterId; _contactDamage = MonsterConfigLoader.GetAbilityParam(_mid, "contactDamage"); _contactCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "contactCooldown"); _maxSegments = MonsterConfigLoader.GetAbilityParamInt(_mid, "maxSegments"); _minSegments = MonsterConfigLoader.GetAbilityParamInt(_mid, "minSegments"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_contactCd > 0) _contactCd -= dt;
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            _moveTimer -= dt;
            if (_moveTimer <= 0) { _moveTimer = 1.2f * (0.7f + (float)state.RNG.NextDouble() * 0.6f); int nearestIdx = -1; float nearestDist = float.MaxValue; for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; float d = DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y); if (d < nearestDist) { nearestDist = d; nearestIdx = i; } } if (nearestIdx >= 0) { ref var target = ref state.Units[nearestIdx]; float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); if (d > 0.01f) { float perp = (float)(state.RNG.NextDouble() - 0.5) * 120f; _moveTargetX = target.X + (dx / d) * 100f + (-dy / d) * perp; _moveTargetY = target.Y + (dy / d) * 100f + (dx / d) * perp; } } else { _moveTargetX = (float)state.RNG.NextDouble() * BattleConstants.FIELD_WIDTH; _moveTargetY = (float)state.RNG.NextDouble() * BattleConstants.FIELD_HEIGHT; } }
            float hpRatio = unit.Hp / unit.MaxHp; float speedMult = 1f + 0.5f * (1f - hpRatio); float speed = unit.BaseMoveSpeed * speedMult;
            float mdx = _moveTargetX - unit.X, mdy = _moveTargetY - unit.Y; float md = Mathf.Sqrt(mdx * mdx + mdy * mdy);
            if (md > 0.01f) { unit.X += (mdx / md) * speed * dt; unit.Y += (mdy / md) * speed * dt; unit.Facing = mdx >= 0 ? 1f : -1f; }
            DamageSystem.ClampToField(ref unit);
            int segCount = Mathf.Clamp(Mathf.RoundToInt(_minSegments + (_maxSegments - _minSegments) * hpRatio), _minSegments, _maxSegments);
            unit.SkillState.SetInt(SkillKeys.NagaSegmentCount, segCount);
            for (int i = 0; i < segCount; i++) { float prevX = i == 0 ? unit.X : unit.SkillState.GetFloat(SkillKeys.NagaSegX(i - 1), unit.X); float prevY = i == 0 ? unit.Y : unit.SkillState.GetFloat(SkillKeys.NagaSegY(i - 1), unit.Y); float curX = unit.SkillState.GetFloat(SkillKeys.NagaSegX(i), prevX); float curY = unit.SkillState.GetFloat(SkillKeys.NagaSegY(i), prevY); float dx = curX - prevX, dy = curY - prevY; float d = Mathf.Sqrt(dx * dx + dy * dy); if (d > 16f) { float lerp = (d - 16f) / d; curX = prevX + dx * lerp; curY = prevY + dy * lerp; } unit.SkillState.SetFloat(SkillKeys.NagaSegX(i), curX); unit.SkillState.SetFloat(SkillKeys.NagaSegY(i), curY); }
            if (_contactCd <= 0) { for (int i = 0; i < state.Units.Count; i++) { ref var enemy = ref state.Units[i]; if (enemy.Team == unit.Team || enemy.State == UnitStateEnum.Dead) continue; bool hit = false; if (DamageSystem.DistSq(unit.X, unit.Y, enemy.X, enemy.Y) <= Mathf.Pow(12 + enemy.Radius + 4, 2)) hit = true; if (!hit) { for (int s = 0; s < segCount; s++) { float sx = unit.SkillState.GetFloat(SkillKeys.NagaSegX(s), unit.X); float sy = unit.SkillState.GetFloat(SkillKeys.NagaSegY(s), unit.Y); if (DamageSystem.DistSq(sx, sy, enemy.X, enemy.Y) <= Mathf.Pow(12 + enemy.Radius + 4, 2)) { hit = true; break; } } } if (hit) { DamageSystem.DealDamage(ref enemy, _contactDamage, DamageCategory.Melee, ref unit, state.Units); _contactCd = _contactCooldown; break; } } }
        }
        public float GetEngageRange(ref UnitState unit) => 200f;
        public bool IsBusy(ref UnitState unit) => true;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    /// <summary> 渊灵术士：标记目标 → 延迟 → 激光雨（7tick×14=98总伤害） </summary>
    public class WarlockAbility : IAbilityComponent
    {
        private float _cd = 0f;
        private float _castTimer = 0f;
        private float _tickTimer = 0f;
        private int _ticksDone = 0;
        private float _markX, _markY;
        private int _beamVisualId = -1;
        private string _mid;
        private float _markRange, _markDelay, _laserTickDamage, _laserTickInterval, _laserRadius, _abilityCooldown;
        private int _laserTicks;

        public WarlockAbility(MonsterDefSO def)
        {
            _mid = def.monsterId;
            _markRange = MonsterConfigLoader.GetAbilityParam(_mid, "markRange");
            _markDelay = MonsterConfigLoader.GetAbilityParam(_mid, "markDelay");
            _laserTickDamage = MonsterConfigLoader.GetAbilityParam(_mid, "laserTickDamage");
            _laserTicks = MonsterConfigLoader.GetAbilityParamInt(_mid, "laserTicks");
            _laserTickInterval = MonsterConfigLoader.GetAbilityParam(_mid, "laserTickInterval");
            _laserRadius = MonsterConfigLoader.GetAbilityParam(_mid, "laserRadius");
            _abilityCooldown = MonsterConfigLoader.GetAbilityParam(_mid, "abilityCooldown");
        }

        public void OnInit(ref UnitState unit) { _cd = 0f; }

        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_cd > 0) _cd -= dt;
            if (_castTimer > 0) return true;
            if (unit.AttackCooldown > 0) return false;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            if (_cd <= 0 && dist <= _markRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD)
            {
                _markX = target.X;
                _markY = target.Y;
                _castTimer = _markDelay + _laserTicks * _laserTickInterval;
                _cd = _abilityCooldown;
                _ticksDone = 0;
                _tickTimer = 0f;
                _beamVisualId = state.NextId();
                unit.State = UnitStateEnum.Attack;
                return true;
            }
            return false;
        }

        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_castTimer <= 0) return;
            _castTimer -= dt;

            // Update beam visual to track current target position
            if (_beamVisualId >= 0)
            {
                int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId);
                if (tIdx >= 0)
                {
                    ref var t = ref state.Units[tIdx];
                    _markX = t.X;
                    _markY = t.Y;
                }
            }

            // Phase 1: Delay (markDelay) - show crosshair marking on target
            float elapsed = (_markDelay + _laserTicks * _laserTickInterval) - _castTimer;
            if (elapsed < _markDelay)
            {
                // Play crosshair marker once at start, keep it visible during delay
                if (elapsed < 0.1f)
                    VFXSpriteView.Play("crosshair", _markX, _markY, 50f, _markDelay);
                return;
            }

            // Phase 2: Fire rain
            _tickTimer += dt;
            if (_tickTimer >= _laserTickInterval && _ticksDone < _laserTicks)
            {
                _tickTimer -= _laserTickInterval;
                _ticksDone++;

                // Deal beam damage in radius around marked position
                AreaEffectSystem.DealInstantAoe(ref unit, _markX, _markY, _laserRadius,
                    state.Units, _laserTickDamage, DamageCategory.Beam, null, false);

                // Play fire rain VFX at marked position
                VFXSpriteView.Play("firerain", _markX, _markY, 80f, 0.5f);
            }

            if (_castTimer <= 0)
            {
                _beamVisualId = -1;
                unit.AttackCooldown = 0.5f;
            }
        }

        public float GetEngageRange(ref UnitState unit) => _markRange;
        public bool IsBusy(ref UnitState unit) => _castTimer > 0;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }
}
