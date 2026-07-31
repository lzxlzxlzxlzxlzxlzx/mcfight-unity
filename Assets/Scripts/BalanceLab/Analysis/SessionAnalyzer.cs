using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary>
    /// 本地数据分析器：从 LabTestCaseResult 列表计算排名、克制关系、平衡建议。
    /// 不依赖 LLM — 纯代码计算。
    /// </summary>
    public static class SessionAnalyzer
    {
        public static SessionReport Analyze(
            List<LabTestCaseResult> results,
            MonsterDatabase db,
            string planTitle,
            float sessionDuration)
        {
            var report = new SessionReport
            {
                SessionId = $"session_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                PlanTitle = planTitle,
                StartTime = System.DateTime.Now.AddSeconds(-sessionDuration),
                EndTime = System.DateTime.Now,
                TotalDuration = sessionDuration,
            };

            // 聚合每个单位的战斗数据
            var unitData = new Dictionary<string, UnitAggregateData>();
            var matchupData = new Dictionary<string, MatchupAggregateData>();

            foreach (var caseResult in results)
            {
                // 从 case label 推断双方单位 (1v1 模式: "A vs B")
                var parts = caseResult.Label.Split(" vs ");
                if (parts.Length != 2) continue;

                string blueId = ExtractMonsterId(parts[0], db);
                string redId = ExtractMonsterId(parts[1], db);

                if (string.IsNullOrEmpty(blueId) || string.IsNullOrEmpty(redId)) continue;

                EnsureUnit(unitData, blueId, db);
                EnsureUnit(unitData, redId, db);

                foreach (var match in caseResult.Matches)
                {
                    report.TotalMatches++;

                    if (match.Winner == 0) // blue wins
                    {
                        unitData[blueId].Wins++;
                        unitData[redId].Losses++;
                        RecordMatchup(matchupData, blueId, redId, true);
                    }
                    else if (match.Winner == 1)
                    {
                        unitData[redId].Wins++;
                        unitData[blueId].Losses++;
                        RecordMatchup(matchupData, redId, blueId, true);
                    }
                    else
                    {
                        unitData[blueId].Draws++;
                        unitData[redId].Draws++;
                    }

                    // 统计伤害/击杀
                    if (match.UnitStats != null)
                    {
                        foreach (var kv in match.UnitStats)
                        {
                            var s = kv.Value;
                            if (!unitData.ContainsKey(s.MonsterId)) continue;
                            var agg = unitData[s.MonsterId];
                            agg.TotalDamage += s.DamageDealt;
                            agg.TotalKills += s.Kills;
                            agg.TotalSurvivalTime += s.Survived ? s.SurvivalTime : 0f;
                            agg.MatchCount++;
                        }
                    }
                }
            }

            report.CompletedMatches = report.TotalMatches;

            // 计算排名
            var avgPrice = unitData.Values.Where(u => u.Price > 0).Select(u => u.Price).DefaultIfEmpty(1).Average();
            var avgWinRate = unitData.Values.Any() ? unitData.Values.Average(u => u.TotalMatches > 0 ? (float)u.Wins / u.TotalMatches : 0f) : 0f;

            foreach (var kv in unitData)
            {
                var u = kv.Value;
                int total = u.Wins + u.Losses + u.Draws;
                u.TotalMatches = total;
                u.WinRate = total > 0 ? (float)u.Wins / total : 0f;
                u.AvgDamage = u.MatchCount > 0 ? u.TotalDamage / u.MatchCount : 0f;
                u.AvgKills = u.MatchCount > 0 ? (float)u.TotalKills / u.MatchCount : 0f;
                u.AvgSurvival = u.MatchCount > 0 ? u.TotalSurvivalTime / u.MatchCount : 0f;

                // PowerScore = winRate * 0.6 + normalized_dps * 0.25 + survival_factor * 0.15
                float dpsNorm = Mathf.Clamp01(u.AvgDamage / 50f);
                float survNorm = Mathf.Clamp01(u.AvgSurvival / 30f);
                u.PowerScore = u.WinRate * 0.6f + dpsNorm * 0.25f + survNorm * 0.15f;

                // ExpectedPower = price / avgPrice * avgWinRate
                u.ExpectedPower = u.Price > 0 ? (u.Price / (float)avgPrice) * avgWinRate : avgWinRate;
                u.PowerDelta = u.PowerScore - u.ExpectedPower;
                u.Confidence = Mathf.Min(1f, total / 10f);

                if (u.PowerDelta > 0.15f) u.BalanceStatus = "Overpowered";
                else if (u.PowerDelta < -0.15f) u.BalanceStatus = "Underpowered";
                else u.BalanceStatus = "Balanced";

                report.Rankings.Add(new UnitRanking
                {
                    MonsterId = u.MonsterId,
                    DisplayName = u.DisplayName,
                    Price = u.Price,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws,
                    TotalMatches = total,
                    WinRate = u.WinRate,
                    AvgDamageDealt = u.AvgDamage,
                    AvgKills = u.AvgKills,
                    AvgSurvivalTime = u.AvgSurvival,
                    PowerScore = u.PowerScore,
                    PowerDelta = u.PowerDelta,
                    BalanceStatus = u.BalanceStatus,
                    Confidence = u.Confidence
                });
            }

            report.Rankings.Sort((a, b) => b.PowerScore.CompareTo(a.PowerScore));

            // 克制关系
            foreach (var kv in matchupData)
            {
                var m = kv.Value;
                if (m.SampleSize < 1) continue;
                float wr = (float)m.Wins / m.SampleSize;
                report.Counters.Add(new CounterRelation
                {
                    AttackerId = m.AttackerId,
                    AttackerName = GetName(m.AttackerId, db),
                    TargetId = m.TargetId,
                    TargetName = GetName(m.TargetId, db),
                    WinRate = wr,
                    SampleSize = m.SampleSize
                });
            }
            report.Counters.Sort((a, b) => b.WinRate.CompareTo(a.WinRate));

            // 平衡建议
            foreach (var r in report.Rankings)
            {
                if (r.Confidence < 0.3f) continue;
                if (r.BalanceStatus == "Overpowered")
                {
                    var def = db.GetById(r.MonsterId);
                    if (def != null)
                    {
                        report.Suggestions.Add(new BalanceSuggestion
                        {
                            MonsterId = r.MonsterId,
                            DisplayName = r.DisplayName,
                            Field = "price",
                            CurrentValue = def.price,
                            SuggestedValue = Mathf.CeilToInt(def.price * 1.2f),
                            ChangePercent = 20f,
                            Reason = $"胜率 {r.WinRate:P0}，PowerDelta {r.PowerDelta:+0.00;-0.00}，建议涨价 20%",
                            Confidence = r.Confidence
                        });
                    }
                }
                else if (r.BalanceStatus == "Underpowered")
                {
                    var def = db.GetById(r.MonsterId);
                    if (def != null)
                    {
                        report.Suggestions.Add(new BalanceSuggestion
                        {
                            MonsterId = r.MonsterId,
                            DisplayName = r.DisplayName,
                            Field = "hp",
                            CurrentValue = def.hp,
                            SuggestedValue = Mathf.CeilToInt(def.hp * 1.15f),
                            ChangePercent = 15f,
                            Reason = $"胜率 {r.WinRate:P0}，PowerDelta {r.PowerDelta:+0.00;-0.00}，建议增血 15%",
                            Confidence = r.Confidence
                        });
                    }
                }
            }

            // 生成摘要和关键发现
            var sb = new StringBuilder();
            sb.AppendLine($"共 {report.TotalMatches} 场战斗，{report.Rankings.Count} 个单位参与。");
            if (report.Rankings.Count > 0)
            {
                var top = report.Rankings[0];
                sb.AppendLine($"最强单位: {top.DisplayName} (胜率 {top.WinRate:P0}, PowerScore {top.PowerScore:F2})");
            }
            if (report.Counters.Count > 0)
            {
                var c = report.Counters[0];
                sb.AppendLine($"最显著克制: {c.AttackerName} > {c.TargetName} (胜率 {c.WinRate:P0})");
            }
            int opCount = report.Rankings.FindAll(r => r.BalanceStatus == "Overpowered").Count;
            int upCount = report.Rankings.FindAll(r => r.BalanceStatus == "Underpowered").Count;
            if (opCount > 0) sb.AppendLine($"超模单位: {opCount} 个");
            if (upCount > 0) sb.AppendLine($"弱势单位: {upCount} 个");
            report.Summary = sb.ToString().Trim();

            report.KeyFindings = new List<string>();
            if (report.Rankings.Count > 0)
                report.KeyFindings.Add($"{report.Rankings[0].DisplayName} 以 {report.Rankings[0].WinRate:P0} 胜率排名第一");
            if (report.Counters.Count > 0)
                report.KeyFindings.Add($"{report.Counters[0].AttackerName} 对 {report.Counters[0].TargetName} 有明显克制 ({report.Counters[0].WinRate:P0})");
            if (opCount > 0)
                report.KeyFindings.Add($"发现 {opCount} 个可能超模的单位");
            if (upCount > 0)
                report.KeyFindings.Add($"发现 {upCount} 个可能弱势的单位");

            return report;
        }

        static string ExtractMonsterId(string label, MonsterDatabase db)
        {
            foreach (var def in db.GetAllSortedByPrice())
            {
                if (!string.IsNullOrEmpty(def.displayName) && label.Contains(def.displayName))
                    return def.monsterId;
            }
            return label.Trim().ToLower();
        }

        static void EnsureUnit(Dictionary<string, UnitAggregateData> dict, string id, MonsterDatabase db)
        {
            if (!dict.ContainsKey(id))
            {
                var def = db.GetById(id);
                dict[id] = new UnitAggregateData
                {
                    MonsterId = id,
                    DisplayName = def?.displayName ?? id,
                    Price = def?.price ?? 0
                };
            }
        }

        static void RecordMatchup(Dictionary<string, MatchupAggregateData> dict, string attacker, string target, bool won)
        {
            string key = $"{attacker}>{target}";
            if (!dict.ContainsKey(key))
                dict[key] = new MatchupAggregateData { AttackerId = attacker, TargetId = target };
            dict[key].SampleSize++;
            if (won) dict[key].Wins++;
        }

        static string GetName(string id, MonsterDatabase db)
        {
            var def = db.GetById(id);
            return def?.displayName ?? id;
        }

        class UnitAggregateData
        {
            public string MonsterId;
            public string DisplayName;
            public int Price;
            public int Wins, Losses, Draws, TotalMatches;
            public float WinRate;
            public float TotalDamage, AvgDamage;
            public int TotalKills;
            public float AvgKills;
            public float TotalSurvivalTime, AvgSurvival;
            public float PowerScore, ExpectedPower, PowerDelta;
            public string BalanceStatus;
            public float Confidence;
            public int MatchCount;
        }

        class MatchupAggregateData
        {
            public string AttackerId, TargetId;
            public int Wins, SampleSize;
        }

        /// <summary> 序列化为可存储格式 </summary>
        public static SessionReportData ToStorable(SessionReport report)
        {
            var d = new SessionReportData
            {
                session_id = report.SessionId,
                plan_title = report.PlanTitle,
                start_time = report.StartTime.ToString("o"),
                end_time = report.EndTime.ToString("o"),
                total_matches = report.TotalMatches,
                completed_matches = report.CompletedMatches,
                skipped_matches = report.SkippedMatches,
                summary = report.Summary,
                key_findings = report.KeyFindings,
                rankings = report.Rankings.ConvertAll(r => new RankingEntry
                {
                    monster_id = r.MonsterId,
                    display_name = r.DisplayName,
                    price = r.Price,
                    wins = r.Wins, losses = r.Losses, draws = r.Draws, total = r.TotalMatches,
                    win_rate = r.WinRate,
                    avg_damage_dealt = r.AvgDamageDealt,
                    avg_kills = r.AvgKills,
                    power_score = r.PowerScore,
                    power_delta = r.PowerDelta,
                    balance_status = r.BalanceStatus,
                    confidence = r.Confidence
                }),
                counters = report.Counters.ConvertAll(c => new CounterEntry
                {
                    attacker_id = c.AttackerId,
                    attacker_name = c.AttackerName,
                    target_id = c.TargetId,
                    target_name = c.TargetName,
                    win_rate = c.WinRate,
                    sample_size = c.SampleSize
                }),
                suggestions = report.Suggestions.ConvertAll(s => new SuggestionEntry
                {
                    monster_id = s.MonsterId,
                    display_name = s.DisplayName,
                    field = s.Field,
                    current_value = s.CurrentValue,
                    suggested_value = s.SuggestedValue,
                    change_percent = s.ChangePercent,
                    reason = s.Reason,
                    confidence = s.Confidence
                })
            };
            return d;
        }
    }
}
