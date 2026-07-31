using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary> 测试计划加载/校验/导入导出 </summary>
    public static class TestPlanLoader
    {
        public const string TEST_DIR = "Assets/Resources/BalanceLab/Tests/";

        // ===== 加载 =====

        public static TestPlan LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[TestPlanLoader] File not found: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        public static TestPlan LoadFromJson(string json)
        {
            var file = JsonUtility.FromJson<BalanceTestPlanFile>(json);
            if (file == null || file.tests == null)
            {
                Debug.LogError("[TestPlanLoader] Invalid JSON structure");
                return null;
            }
            var plan = TestPlan.FromFile(file);
            Debug.Log($"[TestPlanLoader] Loaded plan: {plan.Title} ({plan.TotalCases} cases, {plan.TotalMatches} matches)");
            return plan;
        }

        // ===== 保存 =====

        public static void SaveToFile(TestPlan plan, string path)
        {
            var file = plan.ToFile();
            string json = JsonUtility.ToJson(file, true);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            Debug.Log($"[TestPlanLoader] Saved plan to: {path}");
        }

        // ===== 校验 =====

        public class ValidationResult
        {
            public bool IsValid = true;
            public List<string> Errors = new List<string>();
            public List<string> Warnings = new List<string>();
        }

        public static ValidationResult Validate(TestPlan plan, MonsterDatabase db)
        {
            var result = new ValidationResult();

            if (plan.Cases.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("计划为空，至少需要 1 个测试用例");
                return result;
            }

            var usedIds = new HashSet<string>();
            int caseNum = 0;

            foreach (var tc in plan.Cases)
            {
                caseNum++;
                string prefix = $"用例 #{caseNum} ({tc.Label}): ";

                // ID 唯一
                if (string.IsNullOrEmpty(tc.Id))
                {
                    tc.Id = $"case_{caseNum}";
                }
                else if (usedIds.Contains(tc.Id))
                {
                    result.Warnings.Add($"{prefix}ID '{tc.Id}' 重复，已自动重命名");
                    tc.Id = $"case_{caseNum}";
                }
                usedIds.Add(tc.Id);

                // 重复次数
                if (tc.RepeatCount < 1)
                {
                    result.Errors.Add($"{prefix}重复次数 < 1");
                    result.IsValid = false;
                }

                // 双方不能都为空
                if (tc.TeamBlue.Count == 0 && tc.TeamRed.Count == 0)
                {
                    result.Errors.Add($"{prefix}双方阵容都为空");
                    result.IsValid = false;
                }

                // 校验 monsterId
                ValidateTeam(tc.TeamBlue, db, prefix, "蓝方", result);
                ValidateTeam(tc.TeamRed, db, prefix, "红方", result);
            }

            return result;
        }

        static void ValidateTeam(List<TestPlanMonster> team, MonsterDatabase db, string prefix, string teamName, ValidationResult result)
        {
            foreach (var m in team)
            {
                if (string.IsNullOrEmpty(m.MonsterId))
                {
                    result.Errors.Add($"{prefix}{teamName}有空 monsterId");
                    result.IsValid = false;
                    continue;
                }
                var def = db.GetById(m.MonsterId);
                if (def == null)
                {
                    result.Errors.Add($"{prefix}{teamName}单位 '{m.MonsterId}' 不存在");
                    result.IsValid = false;
                }
                if (m.Count < 1)
                {
                    result.Errors.Add($"{prefix}{teamName}单位 '{m.MonsterId}' 数量 < 1");
                    result.IsValid = false;
                }
            }
        }

        // ===== 扫描测试目录 =====

        public static List<string> GetAvailableTestFiles()
        {
            var files = new List<string>();
            if (!Directory.Exists(TEST_DIR)) return files;
            foreach (var f in Directory.GetFiles(TEST_DIR, "*.balancetest.json"))
                files.Add(f);
            return files;
        }
    }
}
