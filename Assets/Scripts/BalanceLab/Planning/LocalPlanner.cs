using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary> 本地规则规划器：根据 IntentPreview 生成 BalanceTestPlanFile </summary>
    public static class LocalPlanner
    {
        public class PlanResult
        {
            public BalanceTestPlanFile File;
            public List<MonsterDetail> FilteredUnits;
            public string Summary;
        }

        public static PlanResult GeneratePlan(IntentPreview intent, MonsterCatalogSnapshot catalog)
        {
            var units = MonsterCatalog.FilterByIntent(catalog, intent);
            var file = new BalanceTestPlanFile
            {
                generated_at = DateTime.UtcNow.ToString("o"),
                generated_by = "LocalPlanner",
                metadata = new BalanceTestMetadata { title = intent.Summary ?? "测试计划" },
                tests = new List<BalanceTestEntry>()
            };

            // 场景 A: 指定两个单位对决
            if (units.Count == 2 && !intent.IsMatrix && !intent.IsMirror)
            {
                file.tests.Add(MakeDuel(units[0], units[1], intent.RepeatCount));
            }
            // 场景 B: 镜像对决
            else if (intent.IsMirror && units.Count > 0)
            {
                foreach (var u in units)
                    file.tests.Add(MakeMirror(u, intent.RepeatCount));
            }
            // 场景 C: 全组合矩阵
            else if (units.Count >= 2)
            {
                for (int i = 0; i < units.Count; i++)
                    for (int j = i + 1; j < units.Count; j++)
                        file.tests.Add(MakeDuel(units[i], units[j], intent.RepeatCount));
            }
            // 场景 D: 单个指定单位 vs 同价随机
            else if (units.Count == 1)
            {
                var u = units[0];
                var all = catalog.Units.FindAll(x => x.Price == u.Price && x.MonsterId != u.MonsterId);
                foreach (var opp in all)
                    file.tests.Add(MakeDuel(u, opp, intent.RepeatCount));
            }

            file.metadata.total_matches = TestPlanIO.CountTotalMatches(file);
            file.metadata.estimated_duration_minutes = (int)Mathf.Ceil(file.metadata.total_matches * 0.4f);

            var sb = new StringBuilder();
            sb.AppendLine($"筛选到 {units.Count} 个单位");
            sb.AppendLine($"生成 {file.tests.Count} 个测试, 共 {file.metadata.total_matches} 场");
            sb.AppendLine($"预计耗时 ~{file.metadata.estimated_duration_minutes} 分钟");
            if (units.Count > 0)
                sb.AppendLine($"单位: {string.Join(", ", units.ConvertAll(u => u.DisplayName))}");

            return new PlanResult { File = file, FilteredUnits = units, Summary = sb.ToString() };
        }

        static BalanceTestEntry MakeDuel(MonsterDetail a, MonsterDetail b, int repeat)
        {
            return new BalanceTestEntry
            {
                id = $"duel_{a.MonsterId}_vs_{b.MonsterId}",
                label = $"{a.DisplayName} vs {b.DisplayName}",
                category = "1v1",
                team_blue = new BalanceTestTeam { monsters = new() { new BalanceTestMonster { monster_id = a.MonsterId, count = 1 } } },
                team_red = new BalanceTestTeam { monsters = new() { new BalanceTestMonster { monster_id = b.MonsterId, count = 1 } } },
                repeat_count = repeat
            };
        }

        static BalanceTestEntry MakeMirror(MonsterDetail u, int repeat)
        {
            return new BalanceTestEntry
            {
                id = $"mirror_{u.MonsterId}",
                label = $"{u.DisplayName} 镜像",
                category = "mirror",
                team_blue = new BalanceTestTeam { monsters = new() { new BalanceTestMonster { monster_id = u.MonsterId, count = 1 } } },
                team_red = new BalanceTestTeam { monsters = new() { new BalanceTestMonster { monster_id = u.MonsterId, count = 1 } } },
                repeat_count = repeat
            };
        }
    }
}
