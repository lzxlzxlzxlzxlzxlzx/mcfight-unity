using System.Collections.Generic;
using System.Linq;

namespace MCFight.BalanceLab
{
    /// <summary> 检测自然语言中的历史会话引用 </summary>
    public static class SessionReferenceDetector
    {
        /// <summary> 从用户输入中检测引用的历史会话 </summary>
        public static List<ArchiveEntry> Detect(string text, ArchiveIndex index)
        {
            var result = new List<ArchiveEntry>();
            if (string.IsNullOrWhiteSpace(text) || index?.sessions == null) return result;

            string lower = text.ToLower();

            // 1. "上次" / "之前" / "上一个" → 最近的会话
            if (lower.Contains("上次") || lower.Contains("之前") || lower.Contains("上一个") || lower.Contains("上一个测试"))
            {
                if (index.sessions.Count > 0)
                    result.Add(index.sessions[0]); // 最新的在前面
            }

            // 2. session_id 直接匹配
            foreach (var s in index.sessions)
            {
                if (!string.IsNullOrEmpty(s.session_id) && text.Contains(s.session_id))
                    if (!result.Contains(s)) result.Add(s);
            }

            // 3. 标题关键词匹配
            foreach (var s in index.sessions)
            {
                if (string.IsNullOrEmpty(s.title)) continue;
                // 提取标题中的关键词（去掉常见后缀）
                var keywords = s.title.Split(new[] { " ", "_", "-", "：", ":", "与", "和", "对比", "测试", "评估" },
                    System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var kw in keywords)
                {
                    if (kw.Length >= 2 && text.Contains(kw))
                    {
                        if (!result.Contains(s)) result.Add(s);
                        break;
                    }
                }
            }

            // 4. key_finding 关键词匹配
            foreach (var s in index.sessions)
            {
                if (string.IsNullOrEmpty(s.key_finding)) continue;
                // 提取单位名等关键词
                var parts = s.key_finding.Split(new[] { ' ', ',', '，', '。', '的' },
                    System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (p.Length >= 3 && text.Contains(p))
                    {
                        if (!result.Contains(s)) result.Add(s);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary> 生成引用摘要文本（注入到规划上下文） </summary>
        public static string BuildReferenceContext(List<ArchiveEntry> references)
        {
            if (references == null || references.Count == 0) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[引用历史测试]");
            foreach (var r in references)
            {
                sb.AppendLine($"会话: {r.session_id}");
                sb.AppendLine($"标题: {r.title}");
                sb.AppendLine($"场次: {r.completed_matches}/{r.total_matches}");
                if (!string.IsNullOrEmpty(r.key_finding))
                    sb.AppendLine($"关键发现: {r.key_finding}");

                // 加载详细报告
                var data = KnowledgePersistence.LoadSession(r.session_id);
                if (data != null)
                {
                    if (data.rankings != null && data.rankings.Count > 0)
                    {
                        sb.AppendLine("排名前3:");
                        foreach (var rk in data.rankings.Take(3))
                            sb.AppendLine($"  {rk.display_name} — 胜率{rk.win_rate:P0} [{rk.balance_status}]");
                    }
                    if (data.counters != null && data.counters.Count > 0)
                    {
                        sb.AppendLine("克制关系:");
                        foreach (var c in data.counters.Take(3))
                            sb.AppendLine($"  {c.attacker_name} > {c.target_name} ({c.win_rate:P0})");
                    }
                    if (data.suggestions != null && data.suggestions.Count > 0)
                    {
                        sb.AppendLine("平衡建议:");
                        foreach (var s in data.suggestions.Take(3))
                            sb.AppendLine($"  {s.display_name} {s.field}: {s.current_value}→{s.suggested_value}");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
