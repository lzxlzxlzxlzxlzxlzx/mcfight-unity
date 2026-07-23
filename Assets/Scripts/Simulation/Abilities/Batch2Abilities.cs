using UnityEngine;
using System.Collections.Generic;

namespace MCFight
{
    public class WitchAbility : IAbilityComponent
    {
        private float _healAmount, _aoeDamage, _aoeRadius;
        private string _mid;
        public WitchAbility(MonsterDefSO def) { _mid = def.monsterId; _healAmount = MonsterConfigLoader.GetAbilityParam(_mid, "healAmount"); _aoeDamage = MonsterConfigLoader.GetAbilityParam(_mid, "aoeDamage"); _aoeRadius = MonsterConfigLoader.GetAbilityParam(_mid, "aoeRadius"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            if (unit.AttackCooldown > 0) return false;
            ref var target = ref state.Units[targetIdx];
            bool hasEnemies = dist <= 200f + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            bool isHurt = unit.Hp < unit.MaxHp;
            if (isHurt && !hasEnemies) { unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + _healAmount); unit.AttackCooldown = 2f; unit.State = UnitStateEnum.Attack; return true; }
            if (!hasEnemies) return false;
            int choice = state.RNG.Next(3);
            float dmg = _aoeDamage; StatusEffectType[] effects = null;
            if (choice == 1) effects = new[] { StatusEffectType.Poison };
            else if (choice == 2) effects = new[] { StatusEffectType.Slow };
            AreaEffectSystem.DealInstantAoe(ref unit, target.X, target.Y, _aoeRadius, state.Units, dmg, DamageCategory.Ranged, effects, false);
            unit.AttackCooldown = 2f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class PriestAbility : IAbilityComponent
    {
        private float _castTimer = 0f; private float _dotTimer = 0f; private bool _casting = false;
        private float _castDuration, _cooldownDuration, _radius, _dmg;
        private string _mid;
        public PriestAbility(MonsterDefSO def) { _mid = def.monsterId; _castDuration = MonsterConfigLoader.GetAbilityParam(_mid, "castDuration"); _cooldownDuration = MonsterConfigLoader.GetAbilityParam(_mid, "cooldownDuration"); _radius = MonsterConfigLoader.GetAbilityParam(_mid, "aoeRadius"); _dmg = MonsterConfigLoader.GetAbilityParam(_mid, "aoeDamage"); }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (unit.AttackCooldown > 0) return false;
            bool hasEnemy = false;
            for (int i = 0; i < state.Units.Count; i++) { ref var u = ref state.Units[i]; if (u.Team == unit.Team || u.State == UnitStateEnum.Dead) continue; if (DamageSystem.Dist(unit.X, unit.Y, u.X, u.Y) <= _radius) { hasEnemy = true; break; } }
            if (!hasEnemy) return false;
            _casting = true; _castTimer = _castDuration; _dotTimer = 0f;
            unit.AttackCooldown = _castDuration + _cooldownDuration; unit.State = UnitStateEnum.Attack;
            MCFight.VFXSpriteView.Play("holyaoe", unit.X, unit.Y, _radius * 2f, _castDuration);
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (!_casting) return;
            _castTimer -= dt; _dotTimer += dt;
            if (_dotTimer >= 1f) { _dotTimer -= 1f; AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, _radius, state.Units, _dmg, DamageCategory.Ranged, null, false); }
            if (_castTimer <= 0) { _casting = false; unit.State = UnitStateEnum.Idle; }
        }
        public float GetEngageRange(ref UnitState unit) => _radius;
        public bool IsBusy(ref UnitState unit) => _casting;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class TarantulaHawkAbility : IAbilityComponent
    {
        public TarantulaHawkAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            DamageSystem.DealDamage(ref target, 5f, DamageCategory.Melee, ref unit, state.Units);
            if (target.HasTag("arthropod")) StatusEffectSystem.Apply(ref target, StatusEffectType.Stun);
            if (unit.MoveType == MoveType.Fly) unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW;
            unit.AttackCooldown = 2f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class BlazeAbility : IAbilityComponent
    {
        private float _volleyTimer = 0f; private int _shotsFired = 0;
        private int _shots; private float _shotInterval, _volleyInterval, _shotDamage;
        private string _mid;
        public BlazeAbility(MonsterDefSO def) { _mid = def.monsterId; _shots = MonsterConfigLoader.GetAbilityParamInt(_mid, "shotCount"); _shotInterval = MonsterConfigLoader.GetAbilityParam(_mid, "shotInterval"); _volleyInterval = MonsterConfigLoader.GetAbilityParam(_mid, "volleyInterval"); _shotDamage = MonsterConfigLoader.GetAbilityParam(_mid, "shotDamage"); }
        public void OnInit(ref UnitState unit) { _volleyTimer = _volleyInterval; }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            if (unit.AttackCooldown > 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            _shotsFired = 0; _volleyTimer = 0;
            unit.AttackCooldown = _volleyInterval; unit.State = UnitStateEnum.Attack;
            FireShot(ref unit, targetIdx, state); _shotsFired++;
            return true;
        }
        void FireShot(ref UnitState unit, int targetIdx, BattleState state)
        {
            ref var target = ref state.Units[targetIdx];
            float baseAngle = Mathf.Atan2(target.Y - unit.Y, target.X - unit.X);
            float offset = (float)(state.RNG.NextDouble() - 0.5) * 0.35f;
            float angle = baseAngle + offset;
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, Mathf.Cos(angle), Mathf.Sin(angle), unit.Id, unit.MonsterId, _shotDamage, 9999f, new[] { StatusEffectType.Burn });
            proj.MaxTravel = 0;
            state.Projectiles.Add(proj);
            MCFight.VFXSpriteView.Play("fireball", unit.X, unit.Y, 30f, 0.5f, angle * Mathf.Rad2Deg);
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_shotsFired >= _shots) return;
            _volleyTimer += dt;
            if (_volleyTimer >= _shotInterval) { _volleyTimer = 0; int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { FireShot(ref unit, tIdx, state); _shotsFired++; } }
        }
        public float GetEngageRange(ref UnitState unit) => 200f;
        public bool IsBusy(ref UnitState unit) => _shotsFired < _shots;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class FlyNagaAbility : IAbilityComponent
    {
        private bool _useDive = false; private float _orbitAngle = 0f;
        public FlyNagaAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (!TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            if (_useDive && dist <= Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD)
            { DamageSystem.DealDamage(ref target, 8f, DamageCategory.Melee, ref unit, state.Units); StatusEffectSystem.Apply(ref target, StatusEffectType.Poison); unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW; }
            else if (dist <= 160f + target.Radius * BattleConstants.TARGET_RADIUS_PAD)
            { float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 4f, 160f, new[] { StatusEffectType.Poison }); state.Projectiles.Add(proj); }
            else return false;
            _useDive = !_useDive; unit.AttackCooldown = 3f; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (unit.AttackCooldown > 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId); if (tIdx >= 0) { ref var target = ref state.Units[tIdx]; float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); if (d > 0.01f) { _orbitAngle += dt * 2f; float orbitRadius = 100f; float desiredX = target.X + Mathf.Cos(_orbitAngle) * orbitRadius; float desiredY = target.Y + Mathf.Sin(_orbitAngle) * orbitRadius; float moveDx = desiredX - unit.X, moveDy = desiredY - unit.Y; float moveD = Mathf.Sqrt(moveDx * moveDx + moveDy * moveDy); if (moveD > 0.01f) { unit.X += (moveDx / moveD) * unit.MoveSpeed * dt; unit.Y += (moveDy / moveD) * unit.MoveSpeed * dt; } } } }
        }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class VexAbility : IAbilityComponent
    {
        public VexAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            DamageSystem.DealDamage(ref target, 13f, DamageCategory.Melee, ref unit, state.Units);
            unit.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW;
            unit.AttackCooldown = 5f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { if (unit.AttackCooldown > 0) MovementSystem.IdleWander(ref unit, dt, state.RNG); }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class EvokerAbility : IAbilityComponent
    {
        private float _fangCd = 0f; private float _summonCd = 0f;
        public EvokerAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_fangCd > 0) _fangCd -= dt;
            if (_summonCd > 0) _summonCd -= dt;
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (_summonCd <= 0) { for (int i = 0; i < 2; i++) { float angle = (float)(state.RNG.NextDouble() * Mathf.PI * 2); float r = 30f + (float)state.RNG.NextDouble() * 30f; var vex = new UnitState { Id = state.NextId(), Team = unit.Team, MonsterId = "vex", X = unit.X + Mathf.Cos(angle) * r, Y = unit.Y + Mathf.Sin(angle) * r, Facing = unit.Facing, Hp = 14, MaxHp = 14, Attack = 13, Armor = 0, ArmorToughness = 0, MoveSpeed = 100, AttackRange = 42, AttackInterval = 5f, Radius = 12, MoveType = MoveType.Fly, AttackType = AttackType.Melee, State = UnitStateEnum.Idle, BaseMoveSpeed = 100, BaseAttackInterval = 5f, TargetId = -1, RiderUnitId = -1, MountUnitId = -1, RetargetTimer = BattleConstants.TARGET_RETARGET_INTERVAL, Tags = new[] { "fly" } }; vex.StatusEffects = new StatusEffectList(); vex.SkillState = new SkillStateMap(); state.Units.Add(vex); } _summonCd = 15f; unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Attack; return true; }
            if (unit.AttackCooldown > 0) return false;
            if (_fangCd <= 0) { if (dist <= 64f) AreaEffectSystem.DealInstantAoe(ref unit, unit.X, unit.Y, 64f, state.Units, 6f, DamageCategory.Melee, null, true); else if (dist <= 160f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) AreaEffectSystem.DealInstantAoe(ref unit, target.X, target.Y, 24f, state.Units, 6f, DamageCategory.Melee, null, true); else return false; _fangCd = 3f; unit.AttackCooldown = 3f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack; return true; }
            return false;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class BrainiacAbility : IAbilityComponent
    {
        private bool _bucketThrown = false;
        public BrainiacAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (!_bucketThrown && unit.Hp < 30f) { _bucketThrown = true; AreaEffectSystem.DealInstantAoe(ref unit, target.X, target.Y, 48f, state.Units, 10f, DamageCategory.Ranged, null, false); state.AreaEffects.Add(AreaEffectSystem.CreatePollutionZone(state.NextId(), unit.Team, target.X, target.Y, 48f, 30f, 5f)); unit.AttackCooldown = 1f; unit.State = UnitStateEnum.Attack; return true; }
            if (target.MoveType == MoveType.Fly || dist > 100f) { if (dist > 160f + target.Radius * BattleConstants.TARGET_RADIUS_PAD) return false; float dx = target.X - unit.X, dy = target.Y - unit.Y; float d = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, unit.X, unit.Y, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 4f, 160f, new[] { StatusEffectType.Poison }); state.Projectiles.Add(proj); unit.AttackCooldown = 1.1f; }
            else { float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD; if (dist > range) return false; DamageSystem.DealDamage(ref target, 5f, DamageCategory.Melee, ref unit, state.Units); StatusEffectSystem.Apply(ref target, StatusEffectType.Poison); unit.AttackCooldown = 0.85f; }
            unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt) { }
        public float GetEngageRange(ref UnitState unit) => 160f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class MurmurAbility : IAbilityComponent
    {
        private float _headX, _headY; private bool _headActive = false;
        public MurmurAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { _headX = unit.X; _headY = unit.Y; }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            if (dist > 200f + target.Radius * BattleConstants.TARGET_RADIUS_PAD || !TargetingSystem.CanTargetForAttack(ref unit, ref target, true)) return false;
            _headActive = true; unit.SkillState.SetBool(SkillKeys.MurmurHeadActive, true);
            _headX = target.X + (float)(state.RNG.NextDouble() - 0.5) * 40f; _headY = target.Y + (float)(state.RNG.NextDouble() - 0.5) * 40f;
            float dx = target.X - _headX, dy = target.Y - _headY; float d = Mathf.Sqrt(dx * dx + dy * dy);
            var proj = ProjectileSystem.CreateDefault(state.NextId(), unit.Team, _headX, _headY, d > 0.01f ? dx / d : 1f, d > 0.01f ? dy / d : 0f, unit.Id, unit.MonsterId, 5f, 60f);
            proj.MaxTravel = 60f; state.Projectiles.Add(proj);
            unit.AttackCooldown = 1.1f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (!_headActive) return;
            int tIdx = TargetingSystem.GetTargetIndex(state.Units, unit.TargetId);
            if (tIdx >= 0) { ref var target = ref state.Units[tIdx]; float dx = target.X - _headX, dy = target.Y - _headY; float d = Mathf.Sqrt(dx * dx + dy * dy); if (d > 50f) { _headX += (dx / d) * 100f * dt; _headY += (dy / d) * 100f * dt; } }
        }
        public float GetEngageRange(ref UnitState unit) => 200f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => true;
    }

    public class SpiderRiderAbility : IAbilityComponent
    {
        private int _riderId = -1; private bool _dismounted = false;
        public SpiderRiderAbility(MonsterDefSO def) { }
        public void OnInit(ref UnitState unit) { }
        public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
        {
            if (_riderId < 0 && !_dismounted) { var rider = new UnitState { Id = state.NextId(), Team = unit.Team, MonsterId = "twilightforest_skeleton_druid", X = unit.X, Y = unit.Y + 8, Facing = unit.Facing, Hp = 20, MaxHp = 20, Attack = 2, Armor = 0, ArmorToughness = 0, MoveSpeed = 0, AttackRange = 160, AttackInterval = 1.1f, Radius = 14, MoveType = MoveType.Ground, AttackType = AttackType.Ranged, State = UnitStateEnum.Idle, BaseMoveSpeed = 0, BaseAttackInterval = 1.1f, TargetId = -1, RiderUnitId = -1, MountUnitId = unit.Id, RetargetTimer = BattleConstants.TARGET_RETARGET_INTERVAL, Tags = new[] { "spider_rider" } }; rider.StatusEffects = new StatusEffectList(); rider.SkillState = new SkillStateMap(); state.Units.Add(rider); _riderId = rider.Id; unit.RiderUnitId = _riderId; }
            if (targetIdx < 0) return false;
            ref var target = ref state.Units[targetIdx];
            if (unit.AttackCooldown > 0) return false;
            float range = Mathf.Max(42f, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
            if (dist > range || !TargetingSystem.CanTargetForAttack(ref unit, ref target, false)) return false;
            DamageSystem.DealDamage(ref target, 6f, DamageCategory.Melee, ref unit, state.Units);
            unit.AttackCooldown = 0.85f; unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME; unit.State = UnitStateEnum.Attack;
            return true;
        }
        public void TickCast(ref UnitState unit, BattleState state, float dt)
        {
            if (_riderId >= 0 && !_dismounted) { int rIdx = state.Units.FindIndexById(_riderId); if (rIdx >= 0) { ref var rider = ref state.Units[rIdx]; if (rider.State == UnitStateEnum.Dead) { _dismounted = true; return; } rider.X = unit.X; rider.Y = unit.Y + 8; rider.Facing = unit.Facing; if (rider.AttackCooldown <= 0 && rider.TargetId >= 0) { int tIdx = TargetingSystem.GetTargetIndex(state.Units, rider.TargetId); if (tIdx >= 0) { ref var t = ref state.Units[tIdx]; float d = DamageSystem.Dist(rider.X, rider.Y, t.X, t.Y); if (d <= 160f) { float dx = t.X - rider.X, dy = t.Y - rider.Y; float dd = Mathf.Sqrt(dx * dx + dy * dy); var proj = ProjectileSystem.CreateDefault(state.NextId(), rider.Team, rider.X, rider.Y, dd > 0.01f ? dx / dd : 1f, dd > 0.01f ? dy / dd : 0f, rider.Id, rider.MonsterId, 2f, 160f, new[] { StatusEffectType.Poison }); state.Projectiles.Add(proj); rider.AttackCooldown = 1.1f; } } } } else { _dismounted = true; if (_riderId >= 0) { int ri = state.Units.FindIndexById(_riderId); if (ri >= 0) { ref var rider = ref state.Units[ri]; rider.MoveSpeed = 48f; rider.BaseMoveSpeed = 48f; rider.MountUnitId = -1; } } } }
            if (unit.State == UnitStateEnum.Dead && !_dismounted) { _dismounted = true; if (_riderId >= 0) { int ri = state.Units.FindIndexById(_riderId); if (ri >= 0) { ref var rider = ref state.Units[ri]; rider.MoveSpeed = 48f; rider.BaseMoveSpeed = 48f; rider.MountUnitId = -1; } } }
        }
        public float GetEngageRange(ref UnitState unit) => 42f;
        public bool IsBusy(ref UnitState unit) => false;
        public bool AllowAntiAir(ref UnitState unit) => false;
    }
}