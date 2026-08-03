using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCFight.BalanceLab
{
    /// <summary>
    /// 平衡实验室控制器：逐场执行测试用例，收集战报。
    /// 支持：暂停/继续、跳过此场/此用例、停止、动态占位符。
    /// </summary>
    public class LabSessionController : MonoBehaviour
    {
        // ===== 公开状态 =====
        public LabPhase Phase { get; private set; } = LabPhase.Idle;
        public int CurrentCaseIndex { get; private set; }
        public int CurrentRunIndex { get; private set; }
        public int TotalMatches { get; private set; }
        public int CompletedMatches { get; private set; }
        public int SkippedMatches { get; private set; }
        public int SkippedCases { get; private set; }
        public string CurrentLabel { get; private set; }
        public float ElapsedTime { get; private set; }
        public float ProgressPercent => TotalMatches > 0 ? (float)CompletedMatches / TotalMatches * 100f : 0f;
        public int RemainingMatches => TotalMatches - CompletedMatches - SkippedMatches;
        public float EstimatedRemainingSeconds { get; private set; }
        public List<LabTestCaseResult> Results => _results;

        // ===== 事件 =====
        public event System.Action<LabMatchResult> OnMatchCompleted;
        public event System.Action<LabMatchResult> OnMatchSkipped;
        public event System.Action<LabTestCaseResult> OnCaseCompleted;
        public event System.Action<LabTestCaseResult> OnCaseSkipped;
        public event System.Action<List<LabTestCaseResult>> OnSessionCompleted;
        public event System.Action<LabPhase> OnPhaseChanged;
        public event System.Action<SessionReport> OnReportGenerated;

        // ===== 内部 =====
        private List<LabTestCase> _testCases;
        private List<LabTestCaseResult> _results;
        private BattleStatsCollector _currentStats;
        private float _interMatchDelay = 2f;
        private float _delayTimer = 0f;
        private bool _battleStarted = false;
        private bool _skipMatchRequested = false;
        private bool _skipCaseRequested = false;
        private float _sessionStartTime;
        private float _avgMatchDuration = 15f; // 动态更新
        private string _planTitle = "测试会话";

        // 动态占位符结果（phase_1 排名等）
        private Dictionary<string, string> _resolvedDynamics = new();

        public void StartSession(List<LabTestCase> testCases, string planTitle = null)
        {
            _testCases = testCases;
            _planTitle = planTitle ?? "测试会话";
            _results = new List<LabTestCaseResult>();
            TotalMatches = testCases.Sum(tc => tc.RepeatCount);
            CompletedMatches = 0;
            SkippedMatches = 0;
            SkippedCases = 0;
            CurrentCaseIndex = 0;
            CurrentRunIndex = 0;
            ElapsedTime = 0f;
            _sessionStartTime = Time.time;
            SetPhase(LabPhase.Running);
            StartNextMatch();
        }

        public void Pause()
        {
            if (Phase == LabPhase.Running) SetPhase(LabPhase.Paused);
        }

        public void Resume()
        {
            if (Phase == LabPhase.Paused) SetPhase(LabPhase.Running);
        }

        public void Stop()
        {
            SetPhase(LabPhase.Stopped);
            BattleBridge.Instance?.StopBattle();
            _battleStarted = false;
            if (_results.Count > 0)
                OnSessionCompleted?.Invoke(_results);
            Debug.Log($"[Lab] Stopped. Completed {CompletedMatches}/{TotalMatches}, Skipped {SkippedMatches}.");
            AutoAnalyze();
        }

        public void SkipCurrentMatch()
        {
            _skipMatchRequested = true;
        }

        public void SkipCurrentCase()
        {
            _skipCaseRequested = true;
        }

        /// <summary>注册动态占位符解析结果（如 phase_1_rank_1 → "creeper"）</summary>
        public void RegisterDynamic(string key, string monsterId)
        {
            _resolvedDynamics[key] = monsterId;
            Debug.Log($"[Lab] Dynamic resolved: {key} → {monsterId}");
        }

        void SetPhase(LabPhase newPhase)
        {
            Phase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        void Update()
        {
            if (Phase != LabPhase.Running) return;

            ElapsedTime = Time.time - _sessionStartTime;
            UpdateEstimate();

            var bridge = BattleBridge.Instance;
            if (bridge == null) return;

            // 跳过当前场
            if (_skipMatchRequested && _battleStarted)
            {
                bridge.StopBattle();
                _battleStarted = false;
                SkippedMatches++;
                OnMatchSkipped?.Invoke(new LabMatchResult
                {
                    CaseId = _testCases[CurrentCaseIndex].Id,
                    RunIndex = CurrentRunIndex,
                    Winner = -2 // skipped
                });
                _skipMatchRequested = false;
                AdvanceAfterMatch();
                return;
            }

            // 跳过当前用例所有剩余场
            if (_skipCaseRequested && _battleStarted)
            {
                bridge.StopBattle();
                _battleStarted = false;
                int remaining = _testCases[CurrentCaseIndex].RepeatCount - CurrentRunIndex;
                SkippedMatches += remaining;
                SkippedCases++;
                if (_results.Count > CurrentCaseIndex)
                {
                    OnCaseSkipped?.Invoke(_results[CurrentCaseIndex]);
                }
                else
                {
                    OnCaseSkipped?.Invoke(new LabTestCaseResult
                    {
                        CaseId = _testCases[CurrentCaseIndex].Id,
                        Label = _testCases[CurrentCaseIndex].Label,
                        Matches = new List<LabMatchResult>(),
                    });
                }
                _skipCaseRequested = false;
                CurrentCaseIndex++;
                CurrentRunIndex = 0;
                _delayTimer = 0.5f;
                return;
            }

            // 战斗进行中 → 检测结束
            if (_battleStarted && bridge.Simulator != null && bridge.Simulator.IsFinished)
            {
                CollectMatchResult();
                _battleStarted = false;
                _delayTimer = _interMatchDelay;
                return;
            }

            // 局间等待
            if (!_battleStarted && _delayTimer > 0f)
            {
                _delayTimer -= Time.deltaTime;
                if (_delayTimer <= 0f)
                    StartNextMatch();
            }
        }

        void UpdateEstimate()
        {
            if (CompletedMatches > 0)
            {
                _avgMatchDuration = Mathf.Lerp(_avgMatchDuration, ElapsedTime / CompletedMatches, 0.1f);
            }
            EstimatedRemainingSeconds = RemainingMatches * _avgMatchDuration;
        }

        void StartNextMatch()
        {
            if (CurrentCaseIndex >= _testCases.Count)
            {
                SetPhase(LabPhase.Completed);
                Debug.Log($"[Lab] Session completed! {CompletedMatches}/{TotalMatches} matches run, {SkippedMatches} skipped.");
                OnSessionCompleted?.Invoke(_results);
                AutoAnalyze();
                return;
            }

            var testCase = _testCases[CurrentCaseIndex];
            CurrentLabel = $"{testCase.Label} ({CurrentRunIndex + 1}/{testCase.RepeatCount})";
            Debug.Log($"[Lab] Starting: {CurrentLabel}  [{CompletedMatches + 1}/{TotalMatches}]");

            // 解析动态占位符
            var deployments = GenerateDeployments(testCase);

            _currentStats = new BattleStatsCollector();
            GameManager.Instance.StatsCollector = _currentStats;

            var bridge = BattleBridge.Instance;

            // 隐藏主菜单，进入战斗模式
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Phase = GamePhase.Battle;
                gm.IsLabMode = true;
                gm.HideAllUI();
            }

            // 切换到执行模式：隐藏实验室背景+对话框，显示底部控制条
            var labUI = GetComponent<LabUI>();
            if (labUI != null) labUI.ShowExecutionMode();
            var chatUI = GetComponent<RequirementChatUI>();
            if (chatUI != null) chatUI.Hide();

            bridge.StartBattle(deployments, GameManager.Instance.Database);
            _currentStats.Init(bridge.Simulator.State);
            _battleStarted = true;
        }

        void CollectMatchResult()
        {
            var bridge = BattleBridge.Instance;
            var sim = bridge.Simulator;
            _currentStats.UpdateFinalStats(sim.State.Units, sim.ElapsedTime);

            var result = new LabMatchResult
            {
                CaseId = _testCases[CurrentCaseIndex].Id,
                RunIndex = CurrentRunIndex,
                Winner = sim.Winner,
                Duration = sim.ElapsedTime,
                UnitStats = new Dictionary<int, UnitBattleStats>(_currentStats.GetAllStats())
            };

            CompletedMatches++;
            Debug.Log($"[Lab] Match done: winner={result.Winner}, duration={result.Duration:F1}s");

            OnMatchCompleted?.Invoke(result);

            if (_results.Count <= CurrentCaseIndex)
                _results.Add(new LabTestCaseResult
                {
                    CaseId = _testCases[CurrentCaseIndex].Id,
                    Label = _testCases[CurrentCaseIndex].Label,
                    Matches = new List<LabMatchResult>()
                });

            _results[CurrentCaseIndex].Matches.Add(result);
            var tr = _results[CurrentCaseIndex];
            if (result.Winner == 0) tr.BlueWins++;
            else if (result.Winner == 1) tr.RedWins++;
            else tr.Draws++;
            _results[CurrentCaseIndex] = tr;

            bridge.StopBattle();
            AdvanceAfterMatch();
        }

        void AdvanceAfterMatch()
        {
            CurrentRunIndex++;
            if (CurrentRunIndex >= _testCases[CurrentCaseIndex].RepeatCount)
            {
                if (_results.Count > CurrentCaseIndex)
                {
                    var tr = _results[CurrentCaseIndex];
                    Debug.Log($"[Lab] Case done: {tr.Label} — Blue {tr.BlueWins} / Red {tr.RedWins} / Draw {tr.Draws}");
                    OnCaseCompleted?.Invoke(tr);
                }
                CurrentCaseIndex++;
                CurrentRunIndex = 0;
            }
        }

        List<DeployedUnit> GenerateDeployments(LabTestCase testCase)
        {
            var list = new List<DeployedUnit>();
            float blueX = 200f, blueY = 200f;
            foreach (var entry in testCase.TeamBlue)
            {
                string mid = ResolveDynamicId(entry.MonsterId);
                for (int i = 0; i < entry.Count; i++)
                {
                    list.Add(new DeployedUnit { MonsterId = mid, Team = 0, X = blueX, Y = blueY });
                    blueY += 80f;
                    if (blueY > 600f) { blueY = 200f; blueX -= 60f; }
                }
            }
            float redX = 1080f, redY = 200f;
            foreach (var entry in testCase.TeamRed)
            {
                string mid = ResolveDynamicId(entry.MonsterId);
                for (int i = 0; i < entry.Count; i++)
                {
                    list.Add(new DeployedUnit { MonsterId = mid, Team = 1, X = redX, Y = redY });
                    redY += 80f;
                    if (redY > 600f) { redY = 200f; redX += 60f; }
                }
            }
            return list;
        }

        void AutoAnalyze()
        {
            if (_results == null || _results.Count == 0) return;
            var db = GameManager.Instance.Database;
            float duration = Time.time - _sessionStartTime;
            var report = SessionAnalyzer.Analyze(_results, db, _planTitle, duration);
            KnowledgePersistence.SaveSession(report);
            OnReportGenerated?.Invoke(report);
            Debug.Log($"[Lab] Report generated: {report.SessionId} — {report.Rankings.Count} ranked, {report.Counters.Count} counters, {report.Suggestions.Count} suggestions");

            // 恢复主菜单（测试期间 Phase=Battle，完成后回到 MainMenu）
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.IsLabMode = false;
                BattleBridge.Instance?.StopBattle();
                gm.Phase = GamePhase.MainMenu;
                gm.HideAllUI();
                gm.mainMenuUI?.Show();

                // 隐藏实验室 UI（底部控制条+背景），报告面板会弹出在上面
                var labUI = GetComponent<LabUI>();
                if (labUI != null) labUI.HideUI();
            }
        }

        string ResolveDynamicId(string monsterId)
        {
            if (monsterId != null && monsterId.StartsWith("__DYNAMIC:") && _resolvedDynamics.TryGetValue(monsterId, out var resolved))
                return resolved;
            return monsterId;
        }

        public static List<LabTestCase> CreateHardcodedPlan()
        {
            return new List<LabTestCase>
            {
                new LabTestCase
                {
                    Id = "test_1",
                    Label = "苦力怕 vs 僵尸",
                    TeamBlue = new[] { new LabLineupEntry { MonsterId = "creeper", Count = 1 } },
                    TeamRed = new[] { new LabLineupEntry { MonsterId = "zombie", Count = 1 } },
                    RepeatCount = 1
                },
                new LabTestCase
                {
                    Id = "test_2",
                    Label = "烈焰人 vs 骷髅",
                    TeamBlue = new[] { new LabLineupEntry { MonsterId = "blaze", Count = 1 } },
                    TeamRed = new[] { new LabLineupEntry { MonsterId = "skeleton", Count = 1 } },
                    RepeatCount = 1
                },
                new LabTestCase
                {
                    Id = "test_3",
                    Label = "监守者 vs 烈焰人",
                    TeamBlue = new[] { new LabLineupEntry { MonsterId = "warden", Count = 1 } },
                    TeamRed = new[] { new LabLineupEntry { MonsterId = "blaze", Count = 1 } },
                    RepeatCount = 1
                },
            };
        }
    }
}
