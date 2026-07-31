using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCFight.BalanceLab
{
    // ===== JSON 可序列化模型 (匹配 .balancetest.json 格式) =====

    [Serializable]
    public class BalanceTestPlanFile
    {
        public string version = "1.0";
        public string generated_at;
        public string generated_by;
        public BalanceTestMetadata metadata;
        public List<BalanceTestEntry> tests;
    }

    [Serializable]
    public class BalanceTestMetadata
    {
        public string title;
        public string description;
        public int estimated_duration_minutes;
        public int total_matches;
    }

    [Serializable]
    public class BalanceTestEntry
    {
        public string id;
        public string label;
        public string category;
        public string description;
        public string deploy_strategy = "StandardSpread";
        public BalanceTestTeam team_red;
        public BalanceTestTeam team_blue;
        public int repeat_count = 1;
        public int priority = 1;
    }

    [Serializable]
    public class BalanceTestTeam
    {
        public List<BalanceTestMonster> monsters;
    }

    [Serializable]
    public class BalanceTestMonster
    {
        public string monster_id;
        public int count;
    }

    // ===== 运行时测试计划模型 =====

    /// <summary> 测试计划（运行时可编辑） </summary>
    public class TestPlan
    {
        public string Title;
        public string Description;
        public string GeneratedBy;
        public List<TestPlanCase> Cases = new List<TestPlanCase>();
        public bool IsModified;

        public int TotalMatches => Cases.Sum(c => c.RepeatCount);
        public int TotalCases => Cases.Count;

        /// <summary> 从 JSON 文件模型转换 </summary>
        public static TestPlan FromFile(BalanceTestPlanFile file)
        {
            var plan = new TestPlan
            {
                Title = file.metadata?.title ?? "未命名计划",
                Description = file.metadata?.description ?? "",
                GeneratedBy = file.generated_by ?? "",
            };
            if (file.tests != null)
            {
                foreach (var entry in file.tests)
                {
                    plan.Cases.Add(TestPlanCase.FromEntry(entry));
                }
            }
            return plan;
        }

        /// <summary> 转换为 JSON 文件模型 </summary>
        public BalanceTestPlanFile ToFile()
        {
            var file = new BalanceTestPlanFile
            {
                version = "1.0",
                generated_at = DateTime.UtcNow.ToString("o"),
                generated_by = GeneratedBy,
                metadata = new BalanceTestMetadata
                {
                    title = Title,
                    description = Description,
                    total_matches = TotalMatches,
                    estimated_duration_minutes = Mathf.CeilToInt(TotalMatches * 0.4f),
                },
                tests = new List<BalanceTestEntry>()
            };
            foreach (var c in Cases)
                file.tests.Add(c.ToEntry());
            return file;
        }

        /// <summary> 转换为 LabSessionController 可用的 List&lt;LabTestCase&gt; </summary>
        public List<LabTestCase> ToLabTestCases()
        {
            var list = new List<LabTestCase>();
            foreach (var c in Cases)
                list.Add(c.ToLabTestCase());
            return list;
        }
    }

    /// <summary> 单个测试用例（运行时可编辑） </summary>
    public class TestPlanCase
    {
        public string Id;
        public string Label;
        public string Description = "";
        public string DeployStrategy = "StandardSpread";
        public List<TestPlanMonster> TeamBlue = new List<TestPlanMonster>();
        public List<TestPlanMonster> TeamRed = new List<TestPlanMonster>();
        public int RepeatCount = 1;

        public int BlueCost(MonsterDatabase db) => TeamBlue.Sum(m => m.GetCost(db));
        public int RedCost(MonsterDatabase db) => TeamRed.Sum(m => m.GetCost(db));

        public static TestPlanCase FromEntry(BalanceTestEntry entry)
        {
            var tc = new TestPlanCase
            {
                Id = entry.id,
                Label = entry.label,
                Description = entry.description ?? "",
                DeployStrategy = entry.deploy_strategy ?? "StandardSpread",
                RepeatCount = Mathf.Max(1, entry.repeat_count),
            };
            if (entry.team_blue?.monsters != null)
                foreach (var m in entry.team_blue.monsters)
                    tc.TeamBlue.Add(new TestPlanMonster(m.monster_id, m.count));
            if (entry.team_red?.monsters != null)
                foreach (var m in entry.team_red.monsters)
                    tc.TeamRed.Add(new TestPlanMonster(m.monster_id, m.count));
            return tc;
        }

        public BalanceTestEntry ToEntry()
        {
            var entry = new BalanceTestEntry
            {
                id = Id,
                label = Label,
                description = Description,
                deploy_strategy = DeployStrategy,
                repeat_count = RepeatCount,
                team_blue = new BalanceTestTeam { monsters = new List<BalanceTestMonster>() },
                team_red = new BalanceTestTeam { monsters = new List<BalanceTestMonster>() },
            };
            foreach (var m in TeamBlue)
                entry.team_blue.monsters.Add(new BalanceTestMonster { monster_id = m.MonsterId, count = m.Count });
            foreach (var m in TeamRed)
                entry.team_red.monsters.Add(new BalanceTestMonster { monster_id = m.MonsterId, count = m.Count });
            return entry;
        }

        public LabTestCase ToLabTestCase()
        {
            return new LabTestCase
            {
                Id = Id,
                Label = Label,
                TeamBlue = TeamBlue.Select(m => new LabLineupEntry { MonsterId = m.MonsterId, Count = m.Count }).ToArray(),
                TeamRed = TeamRed.Select(m => new LabLineupEntry { MonsterId = m.MonsterId, Count = m.Count }).ToArray(),
                RepeatCount = RepeatCount,
            };
        }

        public string GetSummary(MonsterDatabase db)
        {
            string blue = string.Join("+", TeamBlue.Select(m => $"{(db.GetById(m.MonsterId)?.displayName ?? m.MonsterId)}×{m.Count}"));
            string red = string.Join("+", TeamRed.Select(m => $"{(db.GetById(m.MonsterId)?.displayName ?? m.MonsterId)}×{m.Count}"));
            return $"{blue} vs {red}  ×{RepeatCount}";
        }
    }

    /// <summary> 阵容条目 </summary>
    public class TestPlanMonster
    {
        public string MonsterId;
        public int Count = 1;

        public TestPlanMonster(string id, int count = 1) { MonsterId = id; Count = count; }

        public int GetCost(MonsterDatabase db)
        {
            var def = db.GetById(MonsterId);
            return def != null ? def.price * Count : 0;
        }

        public string GetDisplayName(MonsterDatabase db)
        {
            var def = db.GetById(MonsterId);
            return def != null ? def.displayName : MonsterId;
        }
    }

    // ===== 意图预解析 + Catalog (P2 新增) =====

    public class IntentPreview
    {
        public int? TargetPrice;
        public int? PriceMin, PriceMax;
        public string AttackTypeFilter;
        public string MoveTypeFilter;
        public List<string> MentionedUnitIds = new();
        public bool IsMirror;
        public bool IsMatrix;
        public int RepeatCount = 5;
        public string Summary;

        public bool HasFilter =>
            TargetPrice.HasValue || PriceMin.HasValue || PriceMax.HasValue ||
            AttackTypeFilter != null || MoveTypeFilter != null ||
            MentionedUnitIds.Count > 0;
    }

    [Serializable]
    public class MonsterDetail
    {
        public string MonsterId;
        public string DisplayName;
        public int Price;
        public float Hp, Attack, Armor;
        public string AttackType;
        public string MoveType;
        public string[] Tags;
        public string AbilityType;
    }

    public class MonsterCatalogSnapshot
    {
        public List<MonsterDetail> Units = new();
        public int TotalCount;
        public Dictionary<int, int> CountByPrice = new();
    }
}
