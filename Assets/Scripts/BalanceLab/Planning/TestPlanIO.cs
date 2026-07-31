using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary> .balancetest.json 导入/导出/校验/转换 </summary>
    public static class TestPlanIO
    {
        public static BalanceTestPlanFile LoadFromFile(string path)
        {
            if (!File.Exists(path)) { Debug.LogError($"[TestPlanIO] File not found: {path}"); return null; }
            return LoadFromJson(File.ReadAllText(path));
        }

        public static BalanceTestPlanFile LoadFromJson(string json)
        {
            try
            {
                var file = JsonUtility.FromJson<BalanceTestPlanFile>(json);
                if (file == null || file.tests == null || file.tests.Count == 0)
                { Debug.LogError("[TestPlanIO] Invalid or empty test file"); return null; }
                return file;
            }
            catch (Exception e)
            { Debug.LogError($"[TestPlanIO] JSON parse error: {e.Message}"); return null; }
        }

        public static BalanceTestPlanFile LoadFromResources(string resourceName)
        {
            var text = Resources.Load<TextAsset>($"BalanceLab/Tests/{resourceName}");
            if (text == null) { Debug.LogError($"[TestPlanIO] Resource not found: {resourceName}"); return null; }
            return LoadFromJson(text.text);
        }

        public static void SaveToFile(BalanceTestPlanFile file, string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(file, true));
            Debug.Log($"[TestPlanIO] Saved: {path}");
        }

        public static bool Validate(BalanceTestPlanFile file, MonsterDatabase db,
            out List<string> errors, out List<string> warnings)
        {
            errors = new(); warnings = new();
            if (file?.tests == null || file.tests.Count == 0) { errors.Add("测试列表为空"); return false; }

            var idSet = new HashSet<string>();
            foreach (var t in file.tests)
            {
                if (string.IsNullOrEmpty(t.id)) errors.Add("测试项缺少 id");
                else if (!idSet.Add(t.id)) warnings.Add($"重复 id: {t.id}");
                if (t.repeat_count < 1) errors.Add($"[{t.id}] repeat_count < 1");
                ValidateTeam(t.team_red, db, t.id, "red", errors);
                ValidateTeam(t.team_blue, db, t.id, "blue", errors);
            }
            return errors.Count == 0;
        }

        static void ValidateTeam(BalanceTestTeam team, MonsterDatabase db, string caseId, string side, List<string> errors)
        {
            if (team?.monsters == null || team.monsters.Count == 0)
            { errors.Add($"[{caseId}] {side} 方为空"); return; }
            foreach (var m in team.monsters)
            {
                if (string.IsNullOrEmpty(m.monster_id)) errors.Add($"[{caseId}] {side} 空 monster_id");
                else if (db.GetById(m.monster_id) == null) errors.Add($"[{caseId}] {side} '{m.monster_id}' 不存在");
                if (m.count < 1) errors.Add($"[{caseId}] {side} {m.monster_id} count < 1");
            }
        }

        public static List<LabTestCase> ToLabTestCases(BalanceTestPlanFile file)
        {
            var result = new List<LabTestCase>();
            if (file?.tests == null) return result;
            foreach (var t in file.tests)
            {
                var blue = ConvertTeam(t.team_blue);
                var red = ConvertTeam(t.team_red);
                if (blue.Length == 0 || red.Length == 0) continue;
                result.Add(new LabTestCase
                {
                    Id = t.id, Label = t.label,
                    TeamBlue = blue, TeamRed = red,
                    RepeatCount = Mathf.Max(1, t.repeat_count)
                });
            }
            return result;
        }

        static LabLineupEntry[] ConvertTeam(BalanceTestTeam team)
        {
            if (team?.monsters == null || team.monsters.Count == 0) return Array.Empty<LabLineupEntry>();
            var list = new List<LabLineupEntry>();
            foreach (var m in team.monsters)
                list.Add(new LabLineupEntry { MonsterId = m.monster_id, Count = Mathf.Max(1, m.count) });
            return list.ToArray();
        }

        public static int CountTotalMatches(BalanceTestPlanFile file)
        {
            if (file?.tests == null) return 0;
            int total = 0;
            foreach (var t in file.tests) total += Mathf.Max(1, t.repeat_count);
            return total;
        }
    }
}
