using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 状态效果实例 </summary>
    [System.Serializable]
    public struct StatusEffectInstance
    {
        public StatusEffectType Type;
        public float Remaining;
        public float DotTimer;
        public MoveType OriginalMoveType;
    }

    /// <summary> 并发效果列表（用类避免 struct ref 限制） </summary>
    [System.Serializable]
    public class StatusEffectList
    {
        private const int MAX = 8;
        private StatusEffectInstance[] _effects = new StatusEffectInstance[MAX];
        private int _count = 0;

        public int Count => _count;

        public void Add(StatusEffectType type, float duration, MoveType currentMoveType = MoveType.Ground)
        {
            int idx = IndexOf(type);
            if (idx >= 0)
            {
                _effects[idx].Remaining = duration;
                return;
            }
            if (_count < MAX)
            {
                _effects[_count] = new StatusEffectInstance
                {
                    Type = type,
                    Remaining = duration,
                    DotTimer = 0,
                    OriginalMoveType = (type == StatusEffectType.Stun) ? currentMoveType : MoveType.Ground
                };
                _count++;
            }
        }

        public bool Has(StatusEffectType type) => IndexOf(type) >= 0;

        public ref StatusEffectInstance Get(int idx)
        {
            return ref _effects[idx];
        }

        private int IndexOf(StatusEffectType type)
        {
            for (int i = 0; i < _count; i++)
                if (_effects[i].Type == type) return i;
            return -1;
        }

        public void RemoveAt(int idx)
        {
            if (idx < 0 || idx >= _count) return;
            _count--;
            if (idx != _count)
                _effects[idx] = _effects[_count];
        }

        public void Clear() => _count = 0;
    }

    /// <summary> 纯逻辑状态效果系统 </summary>
    public static class StatusEffectSystem
    {
        public static float GetDuration(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Poison => 5f,
                StatusEffectType.Burn => 10f,
                StatusEffectType.Wither => 4f,
                StatusEffectType.Slow => 5f,
                StatusEffectType.Fear => 2f,
                StatusEffectType.Freeze => 2f,
                StatusEffectType.Stun => 30f,
                _ => 0f,
            };
        }

        public static float GetDps(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Poison => 2f,
                StatusEffectType.Burn => 1f,
                StatusEffectType.Wither => 3f,
                _ => 0f,
            };
        }

        public static float GetSpeedMult(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Slow => 0.7f,
                StatusEffectType.Freeze => 0f,
                StatusEffectType.Stun => 0f,
                _ => 1f,
            };
        }

        public static void Apply(ref UnitState unit, StatusEffectType type)
        {
            if (type == StatusEffectType.Burn && unit.HasTag("fire_immune")) return;
            float duration = GetDuration(type);
            if (duration <= 0) return;

            unit.StatusEffects.Add(type, duration, unit.MoveType);

            if (type == StatusEffectType.Stun)
                unit.MoveType = MoveType.Ground;
        }

        public static void ApplyAll(ref UnitState unit, StatusEffectType[] types)
        {
            if (types == null) return;
            for (int i = 0; i < types.Length; i++)
                Apply(ref unit, types[i]);
        }

        public static bool Tick(ref UnitState unit, UnitList allUnits, float dt)
        {
            unit.VulnerableWindow = Mathf.Max(0, unit.VulnerableWindow - dt);

            for (int i = unit.StatusEffects.Count - 1; i >= 0; i--)
            {
                ref var effect = ref unit.StatusEffects.Get(i);
                effect.Remaining -= dt;
                if (effect.Remaining <= 0)
                {
                    if (effect.Type == StatusEffectType.Stun)
                        unit.MoveType = effect.OriginalMoveType;
                    unit.StatusEffects.RemoveAt(i);
                    continue;
                }

                float dps = GetDps(effect.Type);
                if (dps <= 0) continue;
                if (effect.Type == StatusEffectType.Burn && unit.HasTag("fire_immune")) continue;

                effect.DotTimer += dt;
                while (effect.DotTimer >= 1f)
                {
                    effect.DotTimer -= 1f;
                    DealTrueDamage(ref unit, dps, allUnits);
                    if (unit.State == UnitStateEnum.Dead) return true;
                }
            }

            bool slowed = unit.StatusEffects.Has(StatusEffectType.Slow);
            unit.MoveSpeed = slowed ? unit.BaseMoveSpeed * 0.7f : unit.BaseMoveSpeed;
            unit.AttackInterval = slowed ? unit.BaseAttackInterval / 0.7f : unit.BaseAttackInterval;

            if (unit.StatusEffects.Has(StatusEffectType.Freeze) ||
                unit.StatusEffects.Has(StatusEffectType.Stun))
            {
                unit.MoveSpeed = 0;
                if (unit.StatusEffects.Has(StatusEffectType.Freeze))
                    unit.AttackInterval = 1e6f;
            }

            return false;
        }

        static void DealTrueDamage(ref UnitState unit, float amount, UnitList allUnits)
        {
            if (unit.SkillState.GetBool(SkillKeys.CrabBurrowed)) return;

            unit.Hp -= amount;
            if (unit.Hp <= 0) unit.State = UnitStateEnum.Dead;

            if (unit.StatusEffects.Has(StatusEffectType.Burn))
                SpreadBurn(ref unit, allUnits);
        }

        static void SpreadBurn(ref UnitState source, UnitList allUnits)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                ref var other = ref allUnits[i];
                if (other.Id == source.Id || other.State == UnitStateEnum.Dead) continue;
                float dx = other.X - source.X;
                float dy = other.Y - source.Y;
                if (dx * dx + dy * dy <= BattleConstants.BURN_SPREAD_RADIUS * BattleConstants.BURN_SPREAD_RADIUS)
                    Apply(ref other, StatusEffectType.Burn);
            }
        }
    }
}
