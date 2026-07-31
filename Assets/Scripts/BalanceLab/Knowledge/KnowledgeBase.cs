using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary>
    /// 跨会话知识库：积累策略知识 + 平衡知识，持久化到 LabArchive/knowledge/。
    /// 每次 SessionReport 生成后调用 UpdateFromReport() 增量更新。
    /// </summary>
    public static class KnowledgeBase
    {
        static string KnowledgeDir => Path.Combine(Application.dataPath, "Resources/LabArchive/knowledge");
        static string StrategyPath => Path.Combine(KnowledgeDir, "strategy_knowledge.json");
        static string BalancePath => Path.Combine(KnowledgeDir, "balance_knowledge.json");

        // ===== 策略知识 =====

        [Serializable]
        public class StrategyKnowledge
        {
            public int totalSessions;
            public int totalMatches;
            public List<KnowledgeUnitEntry> units = new();
            public List<KnowledgeCounterEntry> counters = new();
        }

        [Serializable]
        public class KnowledgeUnitEntry
        {
            public string monster_id;
            public string display_name;
            public int price;
            public int total_wins;
            public int total_losses;
            public int total_draws;
            public float avg_damage;
            public float avg_kills;
            public int sample_count;
            public float win_rate;
            public float power_score;
            public string role; // Tank / DPS / AOE / Assassin / Support
        }

        [Serializable]
        public class KnowledgeCounterEntry
        {
            public string attacker_id;
            public string attacker_name;
            public string target_id;
            public string target_name;
            public int wins;
            public int total;
            public float win_rate;
        }

        // ===== 平衡知识 =====

        [Serializable]
        public class BalanceKnowledge
        {
            public List<BalanceUnitEntry> units = new();
            public List<string> recentFindings = new();
        }

        [Serializable]
        public class BalanceUnitEntry
        {
            public string monster_id;
            public string display_name;
            public int price;
            public float expected_power;
            public float actual_power;
            public float power_delta;
            public string status; // Overpowered / Underpowered / Balanced
            public float confidence;
            public int sample_count;
        }

        // ===== 加载/保存 =====

        public static StrategyKnowledge LoadStrategy()
        {
            if (!File.Exists(StrategyPath)) return new StrategyKnowledge();
            try { return JsonUtility.FromJson<StrategyKnowledge>(File.ReadAllText(StrategyPath)) ?? new StrategyKnowledge(); }
            catch { return new StrategyKnowledge(); }
        }

        public static BalanceKnowledge LoadBalance()
        {
            if (!File.Exists(BalancePath)) return new BalanceKnowledge();
            try { return JsonUtility.FromJson<BalanceKnowledge>(File.ReadAllText(BalancePath)) ?? new BalanceKnowledge(); }
            catch { return new BalanceKnowledge(); }
        }

        public static void Save(StrategyKnowledge strategy, BalanceKnowledge balance)
        {
            Directory.CreateDirectory(KnowledgeDir);
            File.WriteAllText(StrategyPath, JsonUtility.ToJson(strategy, true));
            File.WriteAllText(BalancePath, JsonUtility.ToJson(balance, true));
        }

        // ===== 增量更新 =====

        public static void UpdateFromReport(SessionReport report)
        {
            var strategy = LoadStrategy();
            var balance = LoadBalance();

            strategy.totalSessions++;
            strategy.totalMatches += report.TotalMatches;

            // 更新单位统计
            foreach (var r in report.Rankings)
            {
                var existing = strategy.units.Find(u => u.monster_id == r.MonsterId);
                if (existing == null)
                {
                    existing = new KnowledgeUnitEntry
                    {
                        monster_id = r.MonsterId,
                        display_name = r.DisplayName,
                        price = r.Price
                    };
                    strategy.units.Add(existing);
                }
                existing.total_wins += r.Wins;
                existing.total_losses += r.Losses;
                existing.total_draws += r.Draws;
                existing.sample_count += r.TotalMatches;
                int total = existing.total_wins + existing.total_losses + existing.total_draws;
                existing.win_rate = total > 0 ? (float)existing.total_wins / total : 0f;
                existing.avg_damage = (existing.avg_damage * (existing.sample_count - r.TotalMatches) + r.AvgDamageDealt * r.TotalMatches) / Mathf.Max(1, existing.sample_count);
                existing.avg_kills = (existing.avg_kills * (existing.sample_count - r.TotalMatches) + r.AvgKills * r.TotalMatches) / Mathf.Max(1, existing.sample_count);
                existing.power_score = (existing.power_score * (existing.sample_count - r.TotalMatches) + r.PowerScore * r.TotalMatches) / Mathf.Max(1, existing.sample_count);
                existing.role = InferRole(r);
            }

            // 更新克制关系
            foreach (var c in report.Counters)
            {
                var key = $"{c.AttackerId}>{c.TargetId}";
                var existing = strategy.counters.Find(x => $"{x.attacker_id}>{x.target_id}" == key);
                if (existing == null)
                {
                    existing = new KnowledgeCounterEntry
                    {
                        attacker_id = c.AttackerId,
                        attacker_name = c.AttackerName,
                        target_id = c.TargetId,
                        target_name = c.TargetName
                    };
                    strategy.counters.Add(existing);
                }
                existing.wins += Mathf.RoundToInt(c.WinRate * c.SampleSize);
                existing.total += c.SampleSize;
                existing.win_rate = existing.total > 0 ? (float)existing.wins / existing.total : 0f;
            }

            // 更新平衡知识
            foreach (var r in report.Rankings)
            {
                var existing = balance.units.Find(u => u.monster_id == r.MonsterId);
                if (existing == null)
                {
                    existing = new BalanceUnitEntry
                    {
                        monster_id = r.MonsterId,
                        display_name = r.DisplayName,
                        price = r.Price
                    };
                    balance.units.Add(existing);
                }
                existing.expected_power = r.PowerScore - r.PowerDelta;
                existing.actual_power = r.PowerScore;
                existing.power_delta = r.PowerDelta;
                existing.status = r.BalanceStatus;
                existing.confidence = r.Confidence;
                existing.sample_count += r.TotalMatches;
            }

            // 记录最近发现（保留最近 20 条）
            balance.recentFindings.AddRange(report.KeyFindings);
            if (balance.recentFindings.Count > 20)
                balance.recentFindings = balance.recentFindings.GetRange(balance.recentFindings.Count - 20, 20);

            Save(strategy, balance);
            Debug.Log($"[KnowledgeBase] Updated: {strategy.totalSessions} sessions, {strategy.totalMatches} matches, {strategy.units.Count} units, {strategy.counters.Count} counters");
        }

        static string InferRole(UnitRanking r)
        {
            if (r.AvgKills > 1.5f && r.WinRate > 0.6f) return "Assassin";
            if (r.AvgDamageDealt > 80f) return "AOE";
            if (r.Price >= 200) return "Tank";
            if (r.AvgDamageDealt > 40f && r.AvgKills < 0.5f) return "Support";
            return "DPS";
        }

        // ===== 查询 =====

        public static StrategyKnowledge GetStrategySnapshot()
        {
            var s = LoadStrategy();
            s.units.Sort((a, b) => b.power_score.CompareTo(a.power_score));
            s.counters.Sort((a, b) => b.win_rate.CompareTo(a.win_rate));
            return s;
        }

        public static string GetStrategySummary()
        {
            var s = LoadStrategy();
            if (s.totalSessions == 0) return "尚无历史测试数据";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"已积累 {s.totalSessions} 次测试, {s.totalMatches} 场战斗");
            var top = s.units.Take(5).ToList();
            if (top.Count > 0)
            {
                sb.AppendLine("最强单位:");
                foreach (var u in top)
                    sb.AppendLine($"  {u.display_name} ({u.price}G) — 胜率{u.win_rate:P0} | {u.role}");
            }
            var topCounters = s.counters.Take(3).ToList();
            if (topCounters.Count > 0)
            {
                sb.AppendLine("克制关系:");
                foreach (var c in topCounters)
                    sb.AppendLine($"  {c.attacker_name} > {c.target_name} ({c.win_rate:P0})");
            }
            return sb.ToString().Trim();
        }
    }
}
