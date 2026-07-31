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
        private Text _progressText;
        private Image _progressBar;
        private Button _pauseBtn;
        private Button _skipMatchBtn;
        private Button _skipCaseBtn;
        private Button _stopBtn;
        private Button _historyBtn;
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

        public void ShowUI()
        {
            if (!_uiCreated) CreateUI();
            if (_bg != null) _bg.SetActive(true);
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

            // Full-screen lab background
            _bg = LabTheme.CreateLabBackground(canvas.transform);
            // Move background to the very bottom (before all other panels)
            _bg.transform.SetSiblingIndex(0);

            // Bottom control panel
            _panel = LabTheme.CreatePanel("LabPanel", 0.15f, 0f, 0.85f, 0f, canvas.transform, 0.95f);
            var prt = _panel.GetComponent<RectTransform>();
            prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(0f, 150f);

            // Status text (top)
            _statusText = LabTheme.CreateText("Status", "准备中...", 0f, 0.65f, 1f, 1f, _panel.transform, 18, Color.white, TextAnchor.MiddleCenter);

            // Progress bar background
            var progBg = new GameObject("ProgBg", typeof(RectTransform), typeof(Image));
            progBg.transform.SetParent(_panel.transform, false);
            var pbrt = progBg.GetComponent<RectTransform>();
            pbrt.anchorMin = new Vector2(0.05f, 0.50f);
            pbrt.anchorMax = new Vector2(0.95f, 0.62f);
            var bgImg = progBg.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            var bgSprite = LabTheme.Theme?.PanelSprite;
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.type = Image.Type.Sliced; }

            // Progress bar fill
            var progFill = new GameObject("ProgFill", typeof(RectTransform), typeof(Image));
            progFill.transform.SetParent(progBg.transform, false);
            _progressBar = progFill.GetComponent<Image>();
            var fillSprite = LabTheme.ButtonSprite(UIButtonStyled.Style.Success);
            if (fillSprite != null) { _progressBar.sprite = fillSprite; _progressBar.type = Image.Type.Sliced; }
            else _progressBar.color = new Color(0.2f, 0.6f, 0.3f, 0.9f);
            _progressBar.fillMethod = Image.FillMethod.Horizontal;
            _progressBar.fillAmount = 0f;
            var pfrt = progFill.GetComponent<RectTransform>();
            pfrt.anchorMin = Vector2.zero; pfrt.anchorMax = Vector2.one;
            pfrt.offsetMin = Vector2.zero; pfrt.offsetMax = Vector2.zero;

            // Progress text
            _progressText = LabTheme.CreateText("Progress", "0%  |  0:00 / ~0:00", 0f, 0.35f, 1f, 0.50f, _panel.transform, 14, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);

            // Result text
            _resultText = LabTheme.CreateText("Result", "", 0f, 0.18f, 1f, 0.35f, _panel.transform, 14, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);

            // Buttons
            _pauseBtn = LabTheme.CreateButton("PauseBtn", "暂停", 0.02f, 0f, 0.22f, 0.16f, _panel.transform, UIButtonStyled.Style.Primary);
            _pauseBtn.onClick.AddListener(() =>
            {
                if (_controller.Phase == LabPhase.Running) { _controller.Pause(); _pauseBtn.GetComponentInChildren<Text>().text = "继续"; }
                else if (_controller.Phase == LabPhase.Paused) { _controller.Resume(); _pauseBtn.GetComponentInChildren<Text>().text = "暂停"; }
            });

            _skipMatchBtn = LabTheme.CreateButton("SkipMatchBtn", "跳过此场", 0.24f, 0f, 0.44f, 0.16f, _panel.transform, UIButtonStyled.Style.Secondary);
            _skipMatchBtn.onClick.AddListener(() => _controller.SkipCurrentMatch());

            _skipCaseBtn = LabTheme.CreateButton("SkipCaseBtn", "跳过此用例", 0.46f, 0f, 0.66f, 0.16f, _panel.transform, UIButtonStyled.Style.Secondary);
            _skipCaseBtn.onClick.AddListener(() => _controller.SkipCurrentCase());

            _stopBtn = LabTheme.CreateButton("StopBtn", "停止", 0.68f, 0f, 0.88f, 0.16f, _panel.transform, UIButtonStyled.Style.Danger);
            _stopBtn.onClick.AddListener(() => _controller.Stop());

            // History button (top-right corner)
            _historyBtn = LabTheme.CreateButton("HistoryBtn", "📋历史", 0.90f, 0f, 1f, 0.16f, _panel.transform, UIButtonStyled.Style.Secondary, 13);
            _historyBtn.onClick.AddListener(() => { var h = GetComponent<HistoryBrowserUI>(); if (h != null) h.Show(); });

            _uiCreated = true;
        }

        void UpdateStatus()
        {
            if (_statusText == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"场次 {CompletedMatches}/{_controller.TotalMatches}  |  {_controller.CurrentLabel}");
            var phase = _controller.Phase == LabPhase.Paused ? "  [已暂停]" : "";
            sb.Append(phase);
            _statusText.text = sb.ToString();

            float pct = _controller.ProgressPercent;
            if (_progressBar != null) _progressBar.fillAmount = pct / 100f;

            if (_progressText != null)
            {
                string elapsed = FormatTime(_controller.ElapsedTime);
                string remaining = FormatTime(_controller.EstimatedRemainingSeconds);
                string skipInfo = _controller.SkippedMatches > 0 ? $"  跳过{_controller.SkippedMatches}场" : "";
                _progressText.text = $"{pct:F0}%  |  {elapsed} / ~{remaining}{skipInfo}";
            }
        }

        static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:00}";
        }

        int CompletedMatches => _controller.CompletedMatches;

        void OnMatch(LabMatchResult result)
        {
            string winner = result.Winner == 0 ? "蓝方胜" : result.Winner == 1 ? "红方胜" : "平局";
            if (_resultText != null) _resultText.text = $"上一场: {winner}  耗时 {result.Duration:F1}s";
        }

        void OnCase(LabTestCaseResult result)
        {
            if (_resultText != null) _resultText.text = $"{result.Label}: 蓝{result.BlueWins} 红{result.RedWins} 平{result.Draws}";
        }

        void OnCaseSkipped(LabTestCaseResult result)
        {
            if (_resultText != null) _resultText.text = $"已跳过: {result.Label}";
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
            if (phase == LabPhase.Paused) _pauseBtn.GetComponentInChildren<Text>().text = "继续";
            else if (phase == LabPhase.Running) _pauseBtn.GetComponentInChildren<Text>().text = "暂停";
        }
    }
}
