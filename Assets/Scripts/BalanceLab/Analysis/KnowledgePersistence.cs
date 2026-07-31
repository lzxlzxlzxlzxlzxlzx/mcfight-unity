using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary> 持久化：保存/加载会话报告 + 索引 </summary>
    public static class KnowledgePersistence
    {
        static string ArchiveRoot => Path.Combine(Application.dataPath, "Resources/LabArchive");
        static string SessionsDir => Path.Combine(ArchiveRoot, "sessions");
        static string IndexPath => Path.Combine(ArchiveRoot, "index.json");

        public static void SaveSession(SessionReport report)
        {
            Directory.CreateDirectory(SessionsDir);
            var sessionDir = Path.Combine(SessionsDir, report.SessionId);
            Directory.CreateDirectory(sessionDir);

            // 保存 report.json
            var storable = SessionAnalyzer.ToStorable(report);
            var file = new SessionReportFile { data = storable };
            string json = JsonUtility.ToJson(file, true);
            File.WriteAllText(Path.Combine(sessionDir, "report.json"), json);

            // 保存 report.md (Markdown 可读格式)
            var md = BuildMarkdown(report);
            File.WriteAllText(Path.Combine(sessionDir, "report.md"), md);

            // 更新索引
            UpdateIndex(report);

            // 更新跨会话知识库
            KnowledgeBase.UpdateFromReport(report);

            Debug.Log($"[KnowledgePersistence] Saved session: {report.SessionId} → {sessionDir}");
        }

        static string BuildMarkdown(SessionReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {report.PlanTitle}");
            sb.AppendLine();
            sb.AppendLine($"**会话 ID**: {report.SessionId}");
            sb.AppendLine($"**时间**: {report.StartTime:yyyy-MM-dd HH:mm} ~ {report.EndTime:HH:mm} ({report.TotalDuration:F0}s)");
            sb.AppendLine($"**场次**: {report.CompletedMatches}/{report.TotalMatches} 完成, {report.SkippedMatches} 跳过");
            sb.AppendLine();
            sb.AppendLine("## 摘要");
            sb.AppendLine(report.Summary);
            sb.AppendLine();

            if (report.KeyFindings.Count > 0)
            {
                sb.AppendLine("## 关键发现");
                foreach (var f in report.KeyFindings)
                    sb.AppendLine($"- {f}");
                sb.AppendLine();
            }

            if (report.Rankings.Count > 0)
            {
                sb.AppendLine("## 单位排名");
                sb.AppendLine("| # | 单位 | 价格 | 胜 | 负 | 平 | 胜率 | PowerScore | 状态 | 置信度 |");
                sb.AppendLine("|---|------|------|----|----|----|------|------------|------|--------|");
                for (int i = 0; i < report.Rankings.Count; i++)
                {
                    var r = report.Rankings[i];
                    sb.AppendLine($"| {i + 1} | {r.DisplayName} | {r.Price}G | {r.Wins} | {r.Losses} | {r.Draws} | {r.WinRate:P0} | {r.PowerScore:F2} | {r.BalanceStatus} | {r.Confidence:P0} |");
                }
                sb.AppendLine();
            }

            if (report.Counters.Count > 0)
            {
                sb.AppendLine("## 克制关系");
                sb.AppendLine("| 攻击方 | 目标 | 胜率 | 样本 |");
                sb.AppendLine("|--------|------|------|------|");
                foreach (var c in report.Counters)
                    sb.AppendLine($"| {c.AttackerName} | {c.TargetName} | {c.WinRate:P0} | {c.SampleSize} |");
                sb.AppendLine();
            }

            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("## 平衡建议");
                sb.AppendLine("| 单位 | 字段 | 当前 | 建议 | 变化 | 原因 | 置信度 |");
                sb.AppendLine("|------|------|------|------|------|------|--------|");
                foreach (var s in report.Suggestions)
                    sb.AppendLine($"| {s.DisplayName} | {s.Field} | {s.CurrentValue} | {s.SuggestedValue} | {s.ChangePercent:+0.0;-0.0}% | {s.Reason} | {s.Confidence:P0} |");
            }

            return sb.ToString();
        }

        static void UpdateIndex(SessionReport report)
        {
            ArchiveIndex index;
            if (File.Exists(IndexPath))
            {
                try
                {
                    index = JsonUtility.FromJson<ArchiveIndex>(File.ReadAllText(IndexPath)) ?? new ArchiveIndex();
                }
                catch { index = new ArchiveIndex(); }
            }
            else
            {
                index = new ArchiveIndex();
                Directory.CreateDirectory(ArchiveRoot);
            }

            var entry = new ArchiveEntry
            {
                session_id = report.SessionId,
                title = report.PlanTitle,
                created_at = report.StartTime.ToString("o"),
                status = "Completed",
                total_matches = report.TotalMatches,
                completed_matches = report.CompletedMatches,
                key_finding = report.KeyFindings.Count > 0 ? report.KeyFindings[0] : ""
            };

            // 去重：如果同 id 存在则替换
            index.sessions.RemoveAll(s => s.session_id == entry.session_id);
            index.sessions.Insert(0, entry);

            File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
        }

        public static ArchiveIndex LoadIndex()
        {
            if (!File.Exists(IndexPath)) return new ArchiveIndex();
            try { return JsonUtility.FromJson<ArchiveIndex>(File.ReadAllText(IndexPath)) ?? new ArchiveIndex(); }
            catch { return new ArchiveIndex(); }
        }

        public static SessionReportData LoadSession(string sessionId)
        {
            var path = Path.Combine(SessionsDir, sessionId, "report.json");
            if (!File.Exists(path)) return null;
            try
            {
                var file = JsonUtility.FromJson<SessionReportFile>(File.ReadAllText(path));
                return file?.data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[KnowledgePersistence] Failed to load {sessionId}: {e.Message}");
                return null;
            }
        }
    }
}
