using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 纯逻辑战斗模拟器。不继承 MonoBehaviour，不依赖渲染。
    /// 可在 Edit Mode 单元测试 / Headless 批量平衡测试 / Server 权威网络中使用。
    /// </summary>
    public class BattleSimulator
    {
        private BattleState _state;
        private readonly Dictionary<int, IAbilityComponent> _abilities = new();

        public BattleState State => _state;
        public bool IsFinished => _state.Winner >= 0;
        public int Winner => _state.Winner;
        public float ElapsedTime => _state.ElapsedTime;

        /// <summary> 初始化战斗 </summary>
        public void Initialize(List<DeployedUnit> deployments, MonsterDatabase database, int seed = 0)
        {
            _state = new BattleState
            {
                Tick = 0,
                Winner = -1,
                ElapsedTime = 0,
                RNG = new System.Random(seed),
                NextIdCounter = 1,
            };

            foreach (var dep in deployments)
            {
                var def = database.GetById(dep.MonsterId);
                if (def == null) continue;

                var unit = CreateUnit(def, dep, _state.NextId());
                _state.Units.Add(unit);
            }
        }

        UnitState CreateUnit(MonsterDefSO def, DeployedUnit dep, int id)
        {
            var unit = new UnitState
            {
                Id = id,
                Team = dep.Team,
                MonsterId = def.monsterId,
                X = dep.X,
                Y = dep.Y,
                Facing = dep.Team == 0 ? 1f : -1f,
                Hp = def.hp,
                MaxHp = def.hp,
                Attack = def.attack,
                Armor = def.armor,
                ArmorToughness = def.armorToughness,
                MoveSpeed = def.moveSpeed,
                AttackRange = def.attackRange,
                AttackInterval = def.attackInterval,
                Radius = def.radius,
                MoveType = def.moveType,
                AttackType = def.attackType,
                State = UnitStateEnum.Idle,
                AttackCooldown = 0,
                AttackAnimTimer = 0,
                TargetId = -1,
                BaseMoveSpeed = def.moveSpeed,
                BaseAttackInterval = def.attackInterval,
                VulnerableWindow = 0,
                SkillCooldown = 0,
                RiderUnitId = -1,
                MountUnitId = -1,
                DriftAngle = 0,
                DriftTimer = 0,
                RetargetTimer = BattleConstants.TARGET_RETARGET_INTERVAL,
                Tags = def.tags,
            };
            unit.SkillState = new SkillStateMap();
            unit.StatusEffects = new StatusEffectList();

            // 注册通用技能组件
            RegisterAbility(unit, def);

            return unit;
        }

        void RegisterAbility(UnitState unit, MonsterDefSO def)
        {
            IAbilityComponent ability = null;

            // 优先根据 abilityComponentType 字符串创建
            if (!string.IsNullOrEmpty(def.abilityComponentType))
            {
                ability = AbilityFactory.Create(def.abilityComponentType, def);
            }

            // 如果没有自定义技能，使用通用攻击模式
            if (ability == null)
            {
                if (def.HasTag("explosive"))
                {
                    ability = new ExplosiveAbility(
                        MonsterConfigLoader.GetAbilityParam(def.monsterId, "explodeRadius"),
                        MonsterConfigLoader.GetAbilityParam(def.monsterId, "fuseDuration"),
                        MonsterConfigLoader.GetAbilityParam(def.monsterId, "centerDamage"),
                        def.monsterId == "alexscaves_nucleeper" ? MonsterConfigLoader.GetAbilityParam(def.monsterId, "edgeDamage") : 0);
                }
                else if (def.attackType == AttackType.Ranged)
                {
                    ability = new RangedAbility();
                }
                else if (def.HasTag("aoe_melee"))
                {
                    ability = new AoeMeleeAbility();
                }
                else
                {
                    ability = new MeleeAbility();
                }
            }

            _abilities[unit.Id] = ability;
            ability.OnInit(ref unit);
        }

        /// <summary> 推进一帧 </summary>
        public void Tick(float dt)
        {
            if (_state.Winner >= 0) return;

            // 注册伤害事件回调
            DamageEvents.OnDamage -= OnDamageEvent;
            DamageEvents.OnDamage += OnDamageEvent;

            // Phase A: 全局效果更新
            AreaEffectSystem.Tick(_state.AreaEffects, _state.Units, dt);
            ProjectileSystem.Tick(_state.Projectiles, _state.Units, dt);

            // Phase B: 单位循环
            for (int i = 0; i < _state.Units.Count; i++)
            {
                ref var unit = ref _state.Units[i];
                if (unit.State == UnitStateEnum.Dead) continue;

                // B.1 状态效果 tick
                bool diedFromDot = StatusEffectSystem.Tick(ref unit, _state.Units, dt);
                if (diedFromDot) continue;

                // B.2 递减冷却
                unit.AttackCooldown = Mathf.Max(0, unit.AttackCooldown - dt);
                unit.AttackAnimTimer = Mathf.Max(0, unit.AttackAnimTimer - dt);
                unit.SkillCooldown = Mathf.Max(0, unit.SkillCooldown - dt);
                unit.RetargetTimer -= dt;

                // B.3 检查施法中
                IAbilityComponent ability = GetAbility(unit.Id);
                if (ability != null && ability.IsBusy(ref unit))
                {
                    ability.TickCast(ref unit, _state, dt);
                    continue;
                }

                // B.4 恐惧 → 随机游走
                if (unit.StatusEffects.Has(StatusEffectType.Fear))
                {
                    MovementSystem.IdleWander(ref unit, dt, _state.RNG);
                    continue;
                }

                // B.5 重选目标
                bool forceRetarget = unit.RetargetTimer <= 0;
                if (forceRetarget)
                    unit.RetargetTimer = BattleConstants.TARGET_RETARGET_INTERVAL;

                int targetId = TargetingSystem.PickTarget(ref unit, _state.Units, forceRetarget, ability);
                unit.TargetId = targetId;

                // B.6 施法中 tick
                if (ability != null)
                    ability.TickCast(ref unit, _state, dt);

                // B.7 无目标 → 游走
                if (targetId < 0)
                {
                    MovementSystem.IdleWander(ref unit, dt, _state.RNG);
                    continue;
                }

                // 获取目标
                int targetIdx = TargetingSystem.GetTargetIndex(_state.Units, targetId);
                if (targetIdx < 0)
                {
                    MovementSystem.IdleWander(ref unit, dt, _state.RNG);
                    continue;
                }
                ref var target = ref _state.Units[targetIdx];

                float dist = DamageSystem.Dist(unit.X, unit.Y, target.X, target.Y);
                MovementSystem.SetFacing(ref unit, target.X);

                // B.8 尝试释放技能
                if (ability != null && ability.TryExecute(ref unit, targetIdx, dist, _state, dt))
                    continue;

                // B.9 标准战斗逻辑
                // 使用交战半径判断是否需要追击
                float engageRange = ability != null ? ability.GetEngageRange(ref unit) : unit.AttackRange;
                float meleeRange = Mathf.Max(unit.AttackRange, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
                bool inMelee = dist <= meleeRange &&
                    TargetingSystem.CanTargetForAttack(ref unit, ref target, false);
                // 远程攻击也加入目标半径补偿（与近战一致）
                float rangedRange = Mathf.Max(unit.AttackRange, unit.Radius + target.Radius) + target.Radius * BattleConstants.TARGET_RADIUS_PAD;
                bool inRanged = dist <= rangedRange &&
                    TargetingSystem.CanTargetForAttack(ref unit, ref target, true);
                bool inEngageRange = dist <= engageRange + target.Radius * BattleConstants.TARGET_RADIUS_PAD;

                if ((inMelee || inRanged) && unit.AttackCooldown > 0)
                {
                    // 在范围内但冷却中 → 小范围随机移动
                    MovementSystem.IdleWander(ref unit, dt, _state.RNG);
                    unit.State = UnitStateEnum.Attack;
                }
                else if (!inEngageRange)
                {
                    // 超出交战半径 → 追击
                    MovementSystem.ChaseTowardTarget(ref unit, ref target, dt);
                }
                else if (inEngageRange && !inMelee && !inRanged)
                {
                    // 在交战半径内但不在攻击距离内 → 追击（靠近目标）
                    MovementSystem.ChaseTowardTarget(ref unit, ref target, dt);
                }
            }

            // Phase C: 后处理
            MovementSystem.SeparateAllUnits(_state.Units, dt);

            // 钳制所有单位到战场
            for (int i = 0; i < _state.Units.Count; i++)
            {
                ref var u = ref _state.Units[i];
                if (u.State != UnitStateEnum.Dead)
                    DamageSystem.ClampToField(ref u);
            }

            // Phase D: 胜负判定
            _state.Winner = CheckWinner(_state.Units);

            // 超时判定：120秒后按剩余总血量百分比判定
            if (_state.Winner < 0 && _state.ElapsedTime > 120f)
            {
                float hp0 = 0, hp1 = 0, maxHp0 = 0, maxHp1 = 0;
                for (int i = 0; i < _state.Units.Count; i++)
                {
                    ref var u = ref _state.Units[i];
                    if (u.State == UnitStateEnum.Dead) continue;
                    if (u.Team == 0) { hp0 += u.Hp; maxHp0 += u.MaxHp; }
                    else { hp1 += u.Hp; maxHp1 += u.MaxHp; }
                }
                float ratio0 = maxHp0 > 0 ? hp0 / maxHp0 : 0;
                float ratio1 = maxHp1 > 0 ? hp1 / maxHp1 : 0;
                if (ratio0 > ratio1) _state.Winner = 0;
                else if (ratio1 > ratio0) _state.Winner = 1;
                else _state.Winner = 0; // 平局蓝方胜
            }

            // Phase E
            _state.Tick++;
            _state.ElapsedTime += dt;
        }

        /// <summary> 伤害事件回调：转发给统计收集器 </summary>
        void OnDamageEvent(DamageEvent evt)
        {
            if (GameManager.Instance?.StatsCollector != null)
                GameManager.Instance.StatsCollector.OnDamageDealt(
                    evt.AttackerId, evt.TargetId, evt.Damage, evt.Category, evt.IsDot, _state.Units);
        }

        IAbilityComponent GetAbility(int unitId)
        {
            _abilities.TryGetValue(unitId, out var ability);
            return ability;
        }

        /// <summary> 注册自定义技能组件（Boss 用） </summary>
        public void SetAbility(int unitId, IAbilityComponent ability)
        {
            _abilities[unitId] = ability;
        }

        int CheckWinner(UnitList units)
        {
            bool hasTeam0 = false, hasTeam1 = false;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].State == UnitStateEnum.Dead) continue;
                if (units[i].Team == 0) hasTeam0 = true;
                else hasTeam1 = true;
            }
            if (!hasTeam0 && !hasTeam1) return -1; // 同归于尽
            if (!hasTeam0) return 1;
            if (!hasTeam1) return 0;
            return -1;
        }
    }
}
