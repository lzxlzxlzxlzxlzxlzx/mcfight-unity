using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 单位战斗统计（赛后用） </summary>
    public struct UnitBattleStats
    {
        public int UnitId;
        public string MonsterId;
        public int Team;
        public float MaxHp;
        public float FinalHp;
        public bool Survived;
        public float SurvivalTime;
        // 伤害统计
        public float DamageDealt;
        public float DamageTaken;
        public float MeleeDamageDealt;
        public float RangedDamageDealt;
        public float BeamDamageDealt;
        public float ExplosionDamageDealt;
        public float DotDamageDealt;
        public float TrueDamageDealt;
        public int Kills;
        // Buff/Debuff 统计
        public int PoisonApplied;
        public int BurnApplied;
        public int WitherApplied;
        public int SlowApplied;
        public int FearApplied;
        public int FreezeApplied;
        public int StunApplied;
        // 承受统计
        public int PoisonReceived;
        public int BurnReceived;
        public int WitherReceived;
    }

    /// <summary> 战斗统计收集器 </summary>
    public class BattleStatsCollector
    {
        private Dictionary<int, UnitBattleStats> _stats = new();
        private Dictionary<int, float> _lastHpMap = new();
        private float _battleDuration;

        public void Init(BattleState state)
        {
            _stats.Clear();
            _lastHpMap.Clear();
            for (int i = 0; i < state.Units.Count; i++)
            {
                ref var u = ref state.Units[i];
                _stats[u.Id] = new UnitBattleStats
                {
                    UnitId = u.Id,
                    MonsterId = u.MonsterId,
                    Team = u.Team,
                    MaxHp = u.MaxHp,
                    FinalHp = u.Hp,
                    Survived = u.State != UnitStateEnum.Dead,
                };
                _lastHpMap[u.Id] = u.Hp;
            }
        }

        public void OnDamageDealt(int attackerId, int targetId, float damage, DamageCategory category, bool isDot, UnitList units)
        {
            if (!_stats.ContainsKey(attackerId)) return;
            if (!_stats.ContainsKey(targetId)) return;

            var attackerStats = _stats[attackerId];
            var targetStats = _stats[targetId];

            attackerStats.DamageDealt += damage;
            targetStats.DamageTaken += damage;

            if (isDot)
            {
                attackerStats.DotDamageDealt += damage;
            }
            else switch (category)
            {
                case DamageCategory.Melee: attackerStats.MeleeDamageDealt += damage; break;
                case DamageCategory.Ranged: attackerStats.RangedDamageDealt += damage; break;
                case DamageCategory.Beam: attackerStats.BeamDamageDealt += damage; break;
                case DamageCategory.Explosion: attackerStats.ExplosionDamageDealt += damage; break;
                case DamageCategory.True: attackerStats.TrueDamageDealt += damage; break;
            }

            if (_lastHpMap.ContainsKey(targetId))
            {
                int tIdx = units.FindIndexById(targetId);
                if (tIdx >= 0)
                {
                    float prevHp = _lastHpMap[targetId];
                    float currHp = units[tIdx].Hp;
                    if (units[tIdx].State == UnitStateEnum.Dead && prevHp > 0)
                        attackerStats.Kills++;
                    _lastHpMap[targetId] = currHp;
                }
            }

            _stats[attackerId] = attackerStats;
            _stats[targetId] = targetStats;
        }

        public void OnStatusApplied(int attackerId, int targetId, StatusEffectType effect)
        {
            if (!_stats.ContainsKey(attackerId)) return;
            var attacker = _stats[attackerId];

            switch (effect)
            {
                case StatusEffectType.Poison: attacker.PoisonApplied++; break;
                case StatusEffectType.Burn: attacker.BurnApplied++; break;
                case StatusEffectType.Wither: attacker.WitherApplied++; break;
                case StatusEffectType.Slow: attacker.SlowApplied++; break;
                case StatusEffectType.Fear: attacker.FearApplied++; break;
                case StatusEffectType.Freeze: attacker.FreezeApplied++; break;
                case StatusEffectType.Stun: attacker.StunApplied++; break;
            }

            _stats[attackerId] = attacker;

            if (_stats.ContainsKey(targetId))
            {
                var target = _stats[targetId];
                switch (effect)
                {
                    case StatusEffectType.Poison: target.PoisonReceived++; break;
                    case StatusEffectType.Burn: target.BurnReceived++; break;
                    case StatusEffectType.Wither: target.WitherReceived++; break;
                }
                _stats[targetId] = target;
            }
        }

        public void UpdateFinalStats(UnitList units, float duration)
        {
            _battleDuration = duration;
            for (int i = 0; i < units.Count; i++)
            {
                ref var u = ref units[i];
                if (_stats.ContainsKey(u.Id))
                {
                    var s = _stats[u.Id];
                    s.FinalHp = u.Hp;
                    s.Survived = u.State != UnitStateEnum.Dead;
                    s.SurvivalTime = duration;
                    _stats[u.Id] = s;
                }
            }
        }

        public Dictionary<int, UnitBattleStats> GetAllStats() => _stats;
        public float BattleDuration => _battleDuration;
    }
}
