using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 游戏阶段 </summary>
    public enum GamePhase { MainMenu, Shop, Deploy, Battle, Result, Codex }

    /// <summary> 游戏模式 </summary>
    public enum GameMode { PvP, PvAI }

    /// <summary>
    /// 游戏管理器：状态机驱动 主菜单→商店→部署→战斗→结算 流程
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("引用")]
        public MonsterDatabase Database;
        public BattleBridge BattleBridge;

        [Header("UI 引用")]
        public ShopUI shopUI;
        public DeployUI deployUI;
        public BattleUI battleUI;
        public ResultUI resultUI;
        public MainMenuUI mainMenuUI;
        public CodexUI codexUI;

        [Header("状态")]
        public GamePhase Phase = GamePhase.MainMenu;
        public GameMode Mode = GameMode.PvP;
        public int[] Gold = { BattleConstants.INITIAL_GOLD, BattleConstants.INITIAL_GOLD };
        public List<ShopEntry> ShopEntries = new();
        public List<DeployedUnit> DeployedUnits = new();
        public int ActiveTeam = 0;
        public int Winner = -1;

        /// <summary> 战斗统计收集器 </summary>
        public BattleStatsCollector StatsCollector { get; private set; }

        void Awake()
        {
            Instance = this;
            Database = new MonsterDatabase();
            Database.LoadAll();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // 递归查找所有 UI（包括未激活的 GameObject）
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null) FindUIRecursive(canvas.transform);
            BattleBridge = FindObjectOfType<BattleBridge>();

            Debug.Log($"[GameManager] Start. shopUI={shopUI != null} resultUI={resultUI != null} mainMenuUI={mainMenuUI != null} codexUI={codexUI != null} bridge={BattleBridge != null}");

            if (Database.Count == 0)
            {
                Debug.LogError("[GameManager] No monster data found!");
                return;
            }

            EnterMainMenu();
        }

        void FindUIRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (shopUI == null && child.TryGetComponent<ShopUI>(out var s)) shopUI = s;
                if (deployUI == null && child.TryGetComponent<DeployUI>(out var d)) deployUI = d;
                if (battleUI == null && child.TryGetComponent<BattleUI>(out var b)) battleUI = b;
                if (resultUI == null && child.TryGetComponent<ResultUI>(out var r)) resultUI = r;
                if (mainMenuUI == null && child.TryGetComponent<MainMenuUI>(out var m)) mainMenuUI = m;
                if (codexUI == null && child.TryGetComponent<CodexUI>(out var c)) codexUI = c;
                FindUIRecursive(child);
            }
        }

        // ===== 主菜单 =====
        public void EnterMainMenu()
        {
            Phase = GamePhase.MainMenu;
            HideAllUI();
            if (mainMenuUI != null) { mainMenuUI.Show(); Debug.Log("[GameManager] EnterMainMenu: shown"); }
            else Debug.LogError("[GameManager] EnterMainMenu: mainMenuUI is null!");
        }

        // ===== 开始游戏（单人） =====
        public void StartPvP()
        {
            Mode = GameMode.PvP;
            EnterShop();
        }

        // ===== 开始游戏（AI 对战） =====
        public void StartPvAI()
        {
            Mode = GameMode.PvAI;
            EnterShop();
        }

        // ===== 图鉴 =====
        public void EnterCodex()
        {
            Phase = GamePhase.Codex;
            HideAllUI();
            if (codexUI != null) { codexUI.Show(); Debug.Log("[GameManager] EnterCodex: shown"); }
            else Debug.LogError("[GameManager] EnterCodex: codexUI is null!");
        }

        public void ExitCodex()
        {
            EnterMainMenu();
        }

        // ===== 商店阶段 =====
        public void EnterShop()
        {
            if (Database == null || Database.Count == 0)
            {
                Database = new MonsterDatabase();
                Database.LoadAll();
            }
            Phase = GamePhase.Shop;
            ActiveTeam = 0;
            ShopEntries.Clear();
            DeployedUnits.Clear();
            Gold[0] = BattleConstants.INITIAL_GOLD;
            Gold[1] = BattleConstants.INITIAL_GOLD;

            HideAllUI();
            if (shopUI != null) { shopUI.Show(); Debug.Log("[GameManager] EnterShop: shopUI shown"); }
            else Debug.LogError("[GameManager] EnterShop: shopUI is null!");
        }

        public bool BuyMonster(string monsterId, int count)
        {
            var def = Database.GetById(monsterId);
            if (def == null) return false;

            int bought = 0;
            for (int i = 0; i < count; i++)
            {
                if (Gold[ActiveTeam] < def.price) break;
                Gold[ActiveTeam] -= def.price;
                ShopEntries.Add(new ShopEntry { MonsterId = monsterId, Team = ActiveTeam });
                bought++;
            }
            if (shopUI) shopUI.Refresh();
            return bought > 0;
        }

        public void SwitchTeam(int team)
        {
            ActiveTeam = team;
            if (shopUI) shopUI.Refresh();
            if (deployUI) deployUI.Refresh();
        }

        public bool CanStartDeploy()
        {
            bool has0 = ShopEntries.Exists(e => e.Team == 0);
            // PvAI 模式下，红方不需要手动购买
            if (Mode == GameMode.PvAI) return has0;
            bool has1 = ShopEntries.Exists(e => e.Team == 1);
            return has0 && has1;
        }

        public void StartDeploy()
        {
            if (!CanStartDeploy()) return;

            // PvAI: AI 自动购买红方
            if (Mode == GameMode.PvAI && Gold[1] > 0)
                AIBuyTeam(1);

            Phase = GamePhase.Deploy;
            DeployedUnits.Clear();
            HideAllUI();
            if (deployUI) deployUI.Show();
        }

        // ===== 部署阶段 =====
        public void PlaceUnit(Vector2 worldPos)
        {
            if (Phase != GamePhase.Deploy) return;
            bool onLeft = worldPos.x <= BattleConstants.FIELD_MID_X - 30f;
            bool onRight = worldPos.x >= BattleConstants.FIELD_MID_X + 30f;
            if (ActiveTeam == 0 && !onLeft) return;
            if (ActiveTeam == 1 && !onRight) return;

            int idx = ShopEntries.FindIndex(e => e.Team == ActiveTeam);
            if (idx < 0) return;
            var entry = ShopEntries[idx];
            ShopEntries.RemoveAt(idx);

            var def = Database.GetById(entry.MonsterId);
            float half = def != null ? Mathf.Max(def.radius, 20f) : 20f;
            float x = Mathf.Clamp(worldPos.x, half, BattleConstants.FIELD_WIDTH - half);
            float y = Mathf.Clamp(worldPos.y, half, BattleConstants.FIELD_HEIGHT - half);

            DeployedUnits.Add(new DeployedUnit { MonsterId = entry.MonsterId, Team = ActiveTeam, X = x, Y = y });
            if (deployUI) deployUI.Refresh();
        }

        public int GetRemainingCount(int team)
        {
            int count = 0;
            foreach (var e in ShopEntries) if (e.Team == team) count++;
            return count;
        }

        public bool AllDeployed()
        {
            if (ShopEntries.Count == 0) return true;
            // PvAI: 只需蓝方部署完
            if (Mode == GameMode.PvAI) return GetRemainingCount(0) == 0;
            return ShopEntries.Count == 0;
        }

        public void AutoDeploy()
        {
            var rng = new System.Random(42);

            // PvAI: AI 自动部署红方
            if (Mode == GameMode.PvAI)
                AIDeployTeam(1, rng);

            while (ShopEntries.Count > 0)
            {
                int idx = 0;
                var entry = ShopEntries[idx];
                ShopEntries.RemoveAt(idx);

                float halfWidth = entry.Team == 0 ? BattleConstants.FIELD_MID_X - 30f : BattleConstants.FIELD_MID_X + 30f;
                float x, y;
                if (entry.Team == 0)
                    x = halfWidth - (float)rng.NextDouble() * 400f;
                else
                    x = halfWidth + (float)rng.NextDouble() * 400f;
                y = 100f + (float)rng.NextDouble() * 520f;

                DeployedUnits.Add(new DeployedUnit { MonsterId = entry.MonsterId, Team = entry.Team, X = x, Y = y });
            }
            if (deployUI) deployUI.Refresh();
        }

        public void StartBattle()
        {
            if (DeployedUnits.Count == 0) return;
            Phase = GamePhase.Battle;
            HideAllUI();
            if (battleUI) battleUI.Show();

            // 初始化统计收集
            if (BattleBridge != null)
            {
                BattleBridge.StartBattle(DeployedUnits, Database);
                StatsCollector = new BattleStatsCollector();
                StatsCollector.Init(BattleBridge.Simulator.State);
            }
        }

        // ===== 战斗结束 =====
        public void OnBattleEnd(int winner)
        {
            Phase = GamePhase.Result;
            Winner = winner;

            // 收集最终统计
            if (StatsCollector != null && BattleBridge?.Simulator != null)
                StatsCollector.UpdateFinalStats(BattleBridge.Simulator.State.Units, BattleBridge.Simulator.ElapsedTime);

            Debug.Log($"[GameManager] OnBattleEnd: winner={winner}, resultUI={resultUI != null}");

            HideAllUI();
            if (resultUI != null) resultUI.Show(winner);
            else Debug.LogError("[GameManager] resultUI is null in OnBattleEnd!");
        }

        // ===== 重新开始 =====
        public void ResetToShop()
        {
            if (BattleBridge != null) BattleBridge.StopBattle();
            EnterShop();
        }

        public void ReturnToMainMenu()
        {
            if (BattleBridge != null) BattleBridge.StopBattle();
            EnterMainMenu();
        }

        void HideAllUI()
        {
            if (shopUI) shopUI.Hide();
            if (deployUI) deployUI.Hide();
            if (battleUI) battleUI.Hide();
            if (resultUI) resultUI.Hide();
            if (mainMenuUI) mainMenuUI.Hide();
            if (codexUI) codexUI.Hide();
        }

        // ===== AI 购买（红方自动搭配） =====
        void AIBuyTeam(int team)
        {
            var rng = new System.Random(System.DateTime.Now.Millisecond);
            var monsters = new List<MonsterDefSO>(Database.GetAllSortedByPrice());

            // 策略：随机选怪直到花完金币
            int attempts = 0;
            while (Gold[team] > 0 && attempts < 100)
            {
                attempts++;
                // 随机选一个能买得起的
                var affordable = monsters.FindAll(m => m.price > 0 && m.price <= Gold[team]);
                if (affordable.Count == 0) break;
                var pick = affordable[rng.Next(affordable.Count)];
                Gold[team] -= pick.price;
                ShopEntries.Add(new ShopEntry { MonsterId = pick.monsterId, Team = team });
            }
        }

        void AIDeployTeam(int team, System.Random rng)
        {
            float halfWidth = team == 0 ? BattleConstants.FIELD_MID_X - 30f : BattleConstants.FIELD_MID_X + 30f;
            var teamEntries = ShopEntries.FindAll(e => e.Team == team);
            foreach (var entry in teamEntries)
            {
                float x = halfWidth + (team == 0 ? -1 : 1) * (float)rng.NextDouble() * 400f;
                float y = 100f + (float)rng.NextDouble() * 520f;
                DeployedUnits.Add(new DeployedUnit { MonsterId = entry.MonsterId, Team = team, X = x, Y = y });
            }
            ShopEntries.RemoveAll(e => e.Team == team);
        }
    }

    /// <summary> 商店购买条目 </summary>
    public class ShopEntry
    {
        public string MonsterId;
        public int Team;
    }
}
