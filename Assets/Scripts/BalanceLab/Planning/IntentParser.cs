using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary> 本地意图解析：从自然语言提取测试参数 </summary>
    public static class IntentParser
    {
        public static IntentPreview Parse(string text, MonsterDatabase db)
        {
            var intent = new IntentPreview();
            if (string.IsNullOrWhiteSpace(text)) return intent;

            string lower = text.ToLower().Trim();

            // 价格
            ParsePrice(lower, intent);

            // 攻击类型
            if (lower.Contains("近战") || lower.Contains("melee"))
                intent.AttackTypeFilter = "Melee";
            if (lower.Contains("远程") || lower.Contains("ranged"))
                intent.AttackTypeFilter = "Ranged";

            // 移动类型
            if (lower.Contains("飞行") || lower.Contains("空军") || lower.Contains("fly"))
                intent.MoveTypeFilter = "Fly";
            if (lower.Contains("地面") || lower.Contains("ground"))
                intent.MoveTypeFilter = "Ground";

            // 对战模式
            if (lower.Contains("镜像") || lower.Contains("mirror"))
                intent.IsMirror = true;
            if (lower.Contains("矩阵") || lower.Contains("全组合") || lower.Contains("所有") || lower.Contains("互相"))
                intent.IsMatrix = true;

            // 重复次数
            ParseRepeatCount(lower, intent);

            // 单位名称匹配
            MatchUnitNames(text, db, intent);

            // 生成摘要
            intent.Summary = BuildSummary(intent);

            return intent;
        }

        static void ParsePrice(string lower, IntentPreview intent)
        {
            // "20金币", "20G", "20g", "20金的"
            for (int i = 0; i < lower.Length - 1; i++)
            {
                if (!char.IsDigit(lower[i])) continue;
                int end = i;
                while (end < lower.Length && char.IsDigit(lower[end])) end++;
                string numStr = lower.Substring(i, end - i);
                string suffix = lower.Substring(end).TrimStart();
                if (suffix.StartsWith("金币") || suffix.StartsWith("g") || suffix.StartsWith("元"))
                {
                    if (int.TryParse(numStr, out int price))
                    {
                        intent.TargetPrice = price;
                        return;
                    }
                }
            }
        }

        static void ParseRepeatCount(string lower, IntentPreview intent)
        {
            // "跑5次", "重复5次", "x5", "×5"
            string[] patterns = { "跑", "重复", "跑", "x", "×", "执行" };
            foreach (var p in patterns)
            {
                int idx = lower.IndexOf(p);
                while (idx >= 0)
                {
                    int numStart = idx + p.Length;
                    while (numStart < lower.Length && !char.IsDigit(lower[numStart])) numStart++;
                    if (numStart < lower.Length && char.IsDigit(lower[numStart]))
                    {
                        int numEnd = numStart;
                        while (numEnd < lower.Length && char.IsDigit(lower[numEnd])) numEnd++;
                        if (int.TryParse(lower.Substring(numStart, numEnd - numStart), out int count) && count > 0)
                        {
                            intent.RepeatCount = count;
                            return;
                        }
                    }
                    idx = lower.IndexOf(p, idx + 1);
                }
            }
        }

        static void MatchUnitNames(string text, MonsterDatabase db, IntentPreview intent)
        {
            foreach (var def in db.GetAllSortedByPrice())
            {
                if (string.IsNullOrEmpty(def.displayName)) continue;
                if (text.Contains(def.displayName))
                {
                    if (!intent.MentionedUnitIds.Contains(def.monsterId))
                        intent.MentionedUnitIds.Add(def.monsterId);
                }
                if (!string.IsNullOrEmpty(def.monsterId) && text.Contains(def.monsterId))
                {
                    if (!intent.MentionedUnitIds.Contains(def.monsterId))
                        intent.MentionedUnitIds.Add(def.monsterId);
                }
            }
        }

        static string BuildSummary(IntentPreview intent)
        {
            var sb = new StringBuilder();
            if (intent.TargetPrice.HasValue)
                sb.Append($"{intent.TargetPrice}G ");
            if (intent.AttackTypeFilter != null)
                sb.Append($"{intent.AttackTypeFilter} ");
            if (intent.MoveTypeFilter != null)
                sb.Append($"{intent.MoveTypeFilter} ");

            if (intent.MentionedUnitIds.Count == 2 && !intent.IsMatrix)
                sb.Append("指定对决");
            else if (intent.IsMirror)
                sb.Append("镜像对决");
            else if (intent.IsMatrix || intent.HasFilter)
                sb.Append("矩阵对决");
            else if (intent.MentionedUnitIds.Count > 0)
                sb.Append("指定单位测试");
            else
                sb.Append("自由探索");

            sb.Append($" ×{intent.RepeatCount}");
            return sb.ToString().Trim();
        }
    }
}
