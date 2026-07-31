using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    public class ReportViewerUI : MonoBehaviour
    {
        private GameObject _panel;
        private Text _titleText;
        private Text _bodyText;
        private ScrollRect _scroll;
        private bool _uiCreated = false;

        void Start() { }

        public void Show(SessionReport report)
        {
            if (!_uiCreated) CreateUI();
            _titleText.text = $"📋 {report.PlanTitle}";
            _bodyText.text = FormatReport(report);
            _panel.SetActive(true);
        }

        public void ShowFromArchive(SessionReportData data)
        {
            if (!_uiCreated) CreateUI();
            _titleText.text = $"📋 {data.plan_title}";
            _bodyText.text = FormatStorableReport(data);
            _panel.SetActive(true);
        }

        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        void CreateUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogError("[ReportUI] No Canvas"); return; }

            _panel = LabTheme.CreatePanel("ReportPanel", 0.2f, 0.08f, 0.8f, 0.92f, canvas.transform, 0.96f);
            _panel.SetActive(false);

            // Title
            _titleText = LabTheme.CreateText("Title", "", 0f, 0.92f, 0.9f, 1f, _panel.transform, 20, new Color(0.9f, 0.85f, 0.3f), TextAnchor.MiddleLeft);

            // Close button
            var closeBtn = LabTheme.CreateButton("Close", "✕", 0.92f, 0.92f, 1f, 1f, _panel.transform, UIButtonStyled.Style.Danger, 16);
            closeBtn.onClick.AddListener(Hide);

            // Scroll area
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_panel.transform, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0.92f);
            srt.offsetMin = new Vector2(5f, 5f); srt.offsetMax = new Vector2(-5f, -5f);
            var sImg = scrollGo.GetComponent<Image>();
            sImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            var sSprite = LabTheme.Theme?.PanelSprite;
            if (sSprite != null) { sImg.sprite = sSprite; sImg.type = Image.Type.Sliced; }
            _scroll = scrollGo.GetComponent<ScrollRect>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f); crt.sizeDelta = Vector2.zero;
            contentGo.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
            contentGo.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = crt;

            _bodyText = LabTheme.CreateText("Body", "", 0f, 0f, 1f, 0f, contentGo.transform, 14, Color.white, TextAnchor.UpperLeft);

            _uiCreated = true;
        }

        string FormatReport(SessionReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#AAAAAA>会话: {report.SessionId}</color>");
            sb.AppendLine($"<color=#AAAAAA>耗时: {report.TotalDuration:F0}s | 场次: {report.CompletedMatches}/{report.TotalMatches}</color>\n");
            sb.AppendLine("<color=#5AAAFF>━━ 摘要 ━━</color>");
            sb.AppendLine(report.Summary + "\n");

            if (report.KeyFindings.Count > 0)
            {
                sb.AppendLine("<color=#FFD700>━━ 关键发现 ━━</color>");
                foreach (var f in report.KeyFindings) sb.AppendLine($"  • {f}");
                sb.AppendLine();
            }
            if (report.Rankings.Count > 0)
            {
                sb.AppendLine("<color=#88FF88>━━ 单位排名 ━━</color>");
                for (int i = 0; i < report.Rankings.Count; i++)
                {
                    var r = report.Rankings[i];
                    string sc = r.BalanceStatus == "Overpowered" ? "<color=#FF4444>" : r.BalanceStatus == "Underpowered" ? "<color=#44AAFF>" : "<color=#CCCCCC>";
                    sb.AppendLine($"  {i + 1}. {r.DisplayName} ({r.Price}G) — 胜率{r.WinRate:P0} | Power {r.PowerScore:F2} {sc}[{r.BalanceStatus}]</color> ({r.Confidence:P0})");
                    sb.AppendLine($"     胜{r.Wins} 负{r.Losses} 平{r.Draws} | 伤害 {r.AvgDamageDealt:F0} | 击杀 {r.AvgKills:F1}");
                }
                sb.AppendLine();
            }
            if (report.Counters.Count > 0)
            {
                sb.AppendLine("<color=#FF8844>━━ 克制关系 ━━</color>");
                foreach (var c in report.Counters) sb.AppendLine($"  {c.AttackerName} → {c.TargetName} : {c.WinRate:P0} ({c.SampleSize}场)");
                sb.AppendLine();
            }
            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("<color=#BB66FF>━━ 平衡建议 ━━</color>");
                foreach (var s in report.Suggestions) sb.AppendLine($"  {s.DisplayName} {s.Field}: {s.CurrentValue}→{s.SuggestedValue} ({s.ChangePercent:+0.0;-0.0}%) — {s.Reason}");
            }
            return sb.ToString();
        }

        string FormatStorableReport(SessionReportData d)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#AAAAAA>会话: {d.session_id}</color>");
            sb.AppendLine($"<color=#AAAAAA>场次: {d.completed_matches}/{d.total_matches}</color>\n");
            sb.AppendLine("<color=#5AAAFF>━━ 摘要 ━━</color>");
            sb.AppendLine(d.summary + "\n");
            if (d.key_findings != null && d.key_findings.Count > 0)
            {
                sb.AppendLine("<color=#FFD700>━━ 关键发现 ━━</color>");
                foreach (var f in d.key_findings) sb.AppendLine($"  • {f}");
                sb.AppendLine();
            }
            if (d.rankings != null && d.rankings.Count > 0)
            {
                sb.AppendLine("<color=#88FF88>━━ 单位排名 ━━</color>");
                for (int i = 0; i < d.rankings.Count; i++)
                {
                    var r = d.rankings[i];
                    sb.AppendLine($"  {i + 1}. {r.display_name} ({r.price}G) — 胜率{r.win_rate:P0} | Power {r.power_score:F2} [{r.balance_status}]");
                }
                sb.AppendLine();
            }
            if (d.counters != null && d.counters.Count > 0)
            {
                sb.AppendLine("<color=#FF8844>━━ 克制关系 ━━</color>");
                foreach (var c in d.counters) sb.AppendLine($"  {c.attacker_name} → {c.target_name} : {c.win_rate:P0} ({c.sample_size}场)");
                sb.AppendLine();
            }
            if (d.suggestions != null && d.suggestions.Count > 0)
            {
                sb.AppendLine("<color=#BB66FF>━━ 平衡建议 ━━</color>");
                foreach (var s in d.suggestions) sb.AppendLine($"  {s.display_name} {s.field}: {s.current_value}→{s.suggested_value} ({s.change_percent:+0.0;-0.0}%)");
            }
            return sb.ToString();
        }
    }
}
