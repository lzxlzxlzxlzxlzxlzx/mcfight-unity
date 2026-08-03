using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    [RequireComponent(typeof(LabSessionController))]
    public class LabUI : MonoBehaviour
    {
        private LabSessionController _controller;
        private GameObject _bg;
        private GameObject _panel;
        private Text _statusText;
        private Text _resultText;
        private Image _progressBar;
        private Button _pauseBtn;
        private Button _stopBtn;
        private bool _uiCreated = false;

        void Start()
        {
            _controller = GetComponent<LabSessionController>();
            _controller.OnMatchCompleted += OnMatch;
            _controller.OnCaseCompleted += OnCase;
            _controller.OnCaseSkipped += OnCaseSkipped;
            _controller.OnSessionCompleted += OnSession;
            _controller.OnPhaseChanged += OnPhaseChanged;
            _controller.OnReportGenerated += OnReportGenerated;
        }

        void Update()
        {
            if (!_uiCreated) return;
            if (_controller.Phase == LabPhase.Running || _controller.Phase == LabPhase.Paused)
                UpdateStatus();
        }

        /// <summary>显示实验室背景+对话框（用于需求输入阶段）</summary>
        public void ShowLabMode()
        {
            if (!_uiCreated) CreateUI();
            if (_bg != null) _bg.SetActive(true);
            _panel.SetActive(true);
        }

        /// <summary>显示底部控制条（用于战斗执行阶段）</summary>
        public void ShowExecutionMode()
        {
            if (!_uiCreated) CreateUI();
            if (_bg != null) _bg.SetActive(false);
            _panel.SetActive(true);
        }

        public void HideUI()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_bg != null) _bg.SetActive(false);
        }

        void CreateUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogError("[LabUI] No Canvas"); return; }

            // Full-screen lab background (hidden during execution)
            _bg = LabTheme.CreateLabBackground(canvas.transform);
            _bg.transform.SetSiblingIndex(0);

            // Bottom control bar
            _panel = LabTheme.CreatePanel("LabPanel", 0f, 0f, 1f, 0f, canvas.transform, 0.92f);
            var prt = _panel.GetComponent<RectTransform>();
            prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(0f, 130f);

            // === Row 1: Status + Progress percent (top, 24px font) ===
            _statusText = LabTheme.CreateText("Status", "准备中...",
                0.02f, 0.60f, 0.65f, 0.95f, _panel.transform,
                22, Color.white, TextAnchor.MiddleLeft);

            var pctText = LabTheme.CreateText("Pct", "",
                0.65f, 0.60f, 0.98f, 0.95f, _panel.transform,
                22, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleRight);

            // === Row 2: Progress bar (middle) ===
            var progBg = new GameObject("ProgBg", typeof(RectTransform), typeof(Image));
            progBg.transform.SetParent(_panel.transform, false);
            var pbrt = progBg.GetComponent<RectTransform>();
            pbrt.anchorMin = new Vector2(0.02f, 0.42f);
            pbrt.anchorMax = new Vector2(0.98f, 0.58f);
            pbrt.offsetMin = Vector2.zero; pbrt.offsetMax = Vector2.zero;
            var bgImg = progBg.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
            var bgSprite = LabTheme.Theme?.PanelSprite;
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.type = Image.Type.Sliced; }

            var progFill = new GameObject("ProgFill", typeof(RectTransform), typeof(Image));
            progFill.transform.SetParent(progBg.transform, false);
            _progressBar = progFill.GetComponent<Image>();
            var fillSprite = LabTheme.ButtonSprite(UIButtonStyled.Style.Success);
            if (fillSprite != null) { _progressBar.sprite = fillSprite; _progressBar.type = Image.Type.Sliced; }
            _progressBar.fillMethod = Image.FillMethod.Horizontal;
            _progressBar.fillAmount = 0f;
            var pfrt = progFill.GetComponent<RectTransform>();
            pfrt.anchorMin = Vector2.zero; pfrt.anchorMax = Vector2.one;
            pfrt.offsetMin = Vector2.zero; pfrt.offsetMax = Vector2.zero;

            // === Row 3: Result text ===
            _resultText = LabTheme.CreateText("Result", "",
                0.02f, 0.25f, 0.98f, 0.42f, _panel.transform,
                16, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleLeft);

            // === Row 4: Buttons ===
            _pauseBtn = LabTheme.CreateButton("PauseBtn", "⏸ 暂停",
                0.01f, 0f, 0.20f, 0.23f, _panel.transform, UIButtonStyled.Style.Primary, 18);
            _pauseBtn.onClick.AddListener(() =>
            {
                if (_controller.Phase == LabPhase.Running) { _controller.Pause(); _pauseBtn.GetComponentInChildren<Text>().text = "▶ 继续"; }
                else if (_controller.Phase == LabPhase.Paused) { _controller.Resume(); _pauseBtn.GetComponentInChildren<Text>().text = "⏸ 暂停"; }
            });

            var skipMatchBtn = LabTheme.CreateButton("SkipMatchBtn", "⏭ 跳过此场",
                0.21f, 0f, 0.45f, 0.23f, _panel.transform, UIButtonStyled.Style.Secondary, 16);
            skipMatchBtn.onClick.AddListener(() => _controller.SkipCurrentMatch());

            var skipCaseBtn = LabTheme.CreateButton("SkipCaseBtn", "⏩ 跳过此用例",
                0.46f, 0f, 0.70f, 0.23f, _panel.transform, UIButtonStyled.Style.Secondary, 16);
            skipCaseBtn.onClick.AddListener(() => _controller.SkipCurrentCase());

            _stopBtn = LabTheme.CreateButton("StopBtn", "⏹ 停止",
                0.71f, 0f, 0.90f, 0.23f, _panel.transform, UIButtonStyled.Style.Danger, 18);
            _stopBtn.onClick.AddListener(() => _controller.Stop());

            var historyBtn = LabTheme.CreateButton("HistoryBtn", "📋",
                0.91f, 0f, 0.99f, 0.23f, _panel.transform, UIButtonStyled.Style.Secondary, 18);
            historyBtn.onClick.AddListener(() => { var h = GetComponent<HistoryBrowserUI>(); if (h != null) h.Show(); });

            // Store pctText reference via name for Update
            pctText.name = "PctText";

            _uiCreated = true;
        }

        Text GetPctText()
        {
            var t = _panel?.transform.Find("PctText");
            return t?.GetComponent<Text>();
        }

        void UpdateStatus()
        {
            if (_statusText == null) return;

            var sb = new StringBuilder();
            sb.Append($"场次 {_controller.CompletedMatches}/{_controller.TotalMatches}");
            sb.Append($"  |  {_controller.CurrentLabel}");
            if (_controller.Phase == LabPhase.Paused) sb.Append("  [已暂停]");
            _statusText.text = sb.ToString();

            float pct = _controller.ProgressPercent;
            if (_progressBar != null) _progressBar.fillAmount = pct / 100f;

            var pctTxt = GetPctText();
            if (pctTxt != null)
            {
                string elapsed = FormatTime(_controller.ElapsedTime);
                string remaining = FormatTime(_controller.EstimatedRemainingSeconds);
                string skipInfo = _controller.SkippedMatches > 0 ? $"  跳过{_controller.SkippedMatches}场" : "";
                pctTxt.text = $"{pct:F0}%  {elapsed} / ~{remaining}{skipInfo}";
            }
        }

        static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:00}";
        }

        void OnMatch(LabMatchResult result)
        {
            string winner = result.Winner == 0 ? "蓝方胜" : result.Winner == 1 ? "红方胜" : "平局";
            if (_resultText != null)
                _resultText.text = $"上一场: {winner}  耗时 {result.Duration:F1}s";
        }

        void OnCase(LabTestCaseResult result)
        {
            if (_resultText != null)
                _resultText.text = $"{result.Label}: 蓝{result.BlueWins} 红{result.RedWins} 平{result.Draws}";
        }

        void OnCaseSkipped(LabTestCaseResult result)
        {
            if (_resultText != null)
                _resultText.text = $"已跳过: {result.Label}";
        }

        void OnSession(List<LabTestCaseResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 测试完成 ===");
            int totalBlue = 0, totalRed = 0;
            foreach (var r in results) { sb.AppendLine($"{r.Label}: 蓝{r.BlueWins} 红{r.RedWins} 平{r.Draws}"); totalBlue += r.BlueWins; totalRed += r.RedWins; }
            sb.AppendLine($"总计: 蓝方 {totalBlue}胜 / 红方 {totalRed}胜");
            if (_controller.SkippedMatches > 0) sb.AppendLine($"跳过: {_controller.SkippedMatches}场");
            if (_resultText != null) _resultText.text = sb.ToString();
        }

        void OnReportGenerated(SessionReport report)
        {
            Debug.Log($"[LabUI] Report generated: {report.SessionId}");
            var reporter = GetComponent<ReportViewerUI>();
            if (reporter != null) reporter.Show(report);
        }

        void OnPhaseChanged(LabPhase phase)
        {
            if (_pauseBtn == null) return;
            if (phase == LabPhase.Paused) _pauseBtn.GetComponentInChildren<Text>().text = "▶ 继续";
            else if (phase == LabPhase.Running) _pauseBtn.GetComponentInChildren<Text>().text = "⏸ 暂停";
        }
    }
}
