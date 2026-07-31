using System.Collections.Generic;
using System.Text;

namespace MCFight.BalanceLab
{
    /// <summary> LLM Prompt 模板：组装系统提示 + 用户上下文 </summary>
    public static class PromptTemplates
    {
        public static string SystemPrompt => @"你是 MC Fight 平衡实验室的测试规划 AI。你的任务是根据用户的测试需求，生成结构化的测试计划 JSON。

输出必须是合法 JSON，格式如下：
{
  ""version"": ""1.0"",
  ""generated_at"": ""2026-01-01T00:00:00Z"",
  ""generated_by"": ""LLM:deepseek-chat"",
  ""metadata"": {
    ""title"": ""测试计划名称"",
    ""description"": ""计划描述"",
    ""total_matches"": 0
  },
  ""tests"": [
    {
      ""id"": ""唯一id"",
      ""label"": ""简短标签"",
      ""category"": ""1v1"",
      ""team_blue"": { ""monsters"": [{""monster_id"": ""creeper"", ""count"": 1}] },
      ""team_red"": { ""monsters"": [{""monster_id"": ""zombie"", ""count"": 1}] },
      ""repeat_count"": 5,
      ""priority"": 1
    }
  ]
}

规则：
1. 每个 test 的 team_blue 和 team_red 必须至少包含一个 monster
2. monster_id 必须是列表中提供的真实 ID
3. repeat_count >= 1
4. 全组合矩阵：N 个单位两两配对，共 C(N,2) 个 test
5. 只输出 JSON，不要任何其他文字";

        public static string BuildUserPrompt(
            string userRequest,
            List<MonsterDetail> relevantUnits,
            string knowledgeSummary = null,
            string referenceContext = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"## 用户需求");
            sb.AppendLine(userRequest);
            sb.AppendLine();

            // 单位列表
            sb.AppendLine($"## 可用单位 ({relevantUnits.Count} 个)");
            sb.AppendLine("| id | 名称 | 价格 | HP | 攻击 | 类型 | 移动 |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var u in relevantUnits)
            {
                sb.AppendLine($"| {u.MonsterId} | {u.DisplayName} | {u.Price}G | {u.Hp:F0} | {u.Attack:F0} | {u.AttackType} | {u.MoveType} |");
            }
            sb.AppendLine();

            // 知识库
            if (!string.IsNullOrEmpty(knowledgeSummary))
            {
                sb.AppendLine("## 知识库摘要");
                sb.AppendLine(knowledgeSummary);
                sb.AppendLine();
            }

            // 引用历史
            if (!string.IsNullOrEmpty(referenceContext))
            {
                sb.AppendLine("## 引用历史测试");
                sb.AppendLine(referenceContext);
                sb.AppendLine();
            }

            sb.AppendLine("## 任务");
            sb.AppendLine("根据以上信息，生成测试计划 JSON。只输出 JSON。");

            return sb.ToString();
        }

        /// <summary> Session 分析 prompt </summary>
        public static string BuildAnalysisPrompt(
            string planTitle,
            int totalMatches,
            List<UnitRanking> rankings,
            List<CounterRelation> counters,
            List<BalanceSuggestion> suggestions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"测试计划: {planTitle}");
            sb.AppendLine($"总场次: {totalMatches}");
            sb.AppendLine();

            sb.AppendLine("## 单位排名");
            for (int i = 0; i < rankings.Count; i++)
            {
                var r = rankings[i];
                sb.AppendLine($"{i + 1}. {r.DisplayName} ({r.Price}G) — 胜率{r.WinRate:P0} | Power {r.PowerScore:F2} [{r.BalanceStatus}]");
            }

            if (counters.Count > 0)
            {
                sb.AppendLine("\n## 克制关系");
                foreach (var c in counters)
                    sb.AppendLine($"{c.AttackerName} > {c.TargetName} ({c.WinRate:P0}, {c.SampleSize}场)");
            }

            if (suggestions.Count > 0)
            {
                sb.AppendLine("\n## 平衡建议");
                foreach (var s in suggestions)
                    sb.AppendLine($"{s.DisplayName} {s.Field}: {s.CurrentValue}→{s.SuggestedValue} ({s.Reason})");
            }

            sb.AppendLine("\n## 任务");
            sb.AppendLine("用中文简要分析测试结果，指出关键发现和平衡性建议。200字以内。");
            return sb.ToString();
        }
    }
}
