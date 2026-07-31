using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary> 赛后结算 UI：每个单位一个概要卡片 + MVP 高亮 </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("引用")]
        public Text winnerText;
        public Button restartButton;
        public Button mainMenuButton;
        public Text battleDurationText;
        public ScrollRect statsScroll;
        public RectTransform statsContent;
        public GameObject statRowPrefab;
        public Button tabAllBtn;
        public Button tabBlueBtn;
        public Button tabRedBtn;
        public Text tabAllText;
        public Text tabBlueText;
        public Text tabRedText;

        private GameManager _gm;
        private int _currentTab = 0; // 0=all, 1=blue, 2=red

        void Start()
        {
            _gm = GameManager.Instance;
            if (restartButton) restartButton.onClick.AddListener(OnRestart);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (tabAllBtn) tabAllBtn.onClick.AddListener(() => SetTab(0));
            if (tabBlueBtn) tabBlueBtn.onClick.AddListener(() => SetTab(1));
            if (tabRedBtn) tabRedBtn.onClick.AddListener(() => SetTab(2));
        }

        public void Show(int winner)
        {
            gameObject.SetActive(true);
            Debug.Log($"[ResultUI] Show called, winner={winner}");

            if (winner == 0) { if (winnerText != null) { winnerText.text = "蓝方胜利！"; winnerText.color = new Color(0.3f, 0.6f, 1f); } }
            else if (winner == 1) { if (winnerText != null) { winnerText.text = "红方胜利！"; winnerText.color = new Color(1f, 0.4f, 0.3f); } }
            else { if (winnerText != null) { winnerText.text = "同归于尽！"; winnerText.color = Color.white; } }

            SetTab(0);
        }

        public void Hide() { gameObject.SetActive(false); }

        void SetTab(int tab)
        {
            _currentTab = tab;
            if (tabAllBtn != null) tabAllBtn.interactable = tab != 0;
            if (tabBlueBtn != null) tabBlueBtn.interactable = tab != 1;
            if (tabRedBtn != null) tabRedBtn.interactable = tab != 2;
            RefreshStats();
        }

        void RefreshStats()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm?.StatsCollector == null) return;

            var stats = _gm.StatsCollector.GetAllStats();
            float duration = _gm.StatsCollector.BattleDuration;

            if (battleDurationText != null)
                battleDurationText.text = $"战斗时长: {duration:F1}秒";

            // Clear old rows
            if (statsContent == null) return;
            for (int i = statsContent.childCount - 1; i >= 0; i--)
                Destroy(statsContent.GetChild(i).gameObject);

            // Collect and sort
            var teamStats = new List<UnitBattleStats>();
            foreach (var kv in stats)
            {
                var s = kv.Value;
                if (_currentTab == 1 && s.Team != 0) continue;
                if (_currentTab == 2 && s.Team != 1) continue;
                teamStats.Add(s);
            }
            teamStats.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));

            // Find MVP (most damage)
            int mvpId = -1;
            float mvpDmg = 0;
            foreach (var s in teamStats)
                if (s.DamageDealt > mvpDmg) { mvpDmg = s.DamageDealt; mvpId = s.UnitId; }

            // Create stat rows
            foreach (var s in teamStats)
            {
                var row = statRowPrefab != null
                    ? Instantiate(statRowPrefab, statsContent)
                    : CreateDefaultStatRow(statsContent);
                row.SetActive(true);
                SetupStatRow(row, s, s.UnitId == mvpId);
            }
        }

        GameObject CreateDefaultStatRow(Transform parent)
        {
            var go = new GameObject("StatRow");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);
            return go;
        }

        void SetupStatRow(GameObject row, UnitBattleStats s, bool isMvp)
        {
            var cnFont = Resources.Load<Font>("Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
            if (cnFont == null)
#if UNITY_EDITOR
                cnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
#endif

            var def = _gm?.Database?.GetById(s.MonsterId);
            string name = def != null ? def.displayName : s.MonsterId;
            Color teamColor = s.Team == 0 ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.3f);

            // Add horizontal layout
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.padding = new RectOffset(8, 8, 4, 4);
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
            }

            // MVP star + Name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(row.transform, false);
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = cnFont; nameTxt.fontSize = 18; nameTxt.color = teamColor;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.text = isMvp ? $"★ {name} (MVP)" : name;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.preferredWidth = 160; nameLe.minWidth = 120;

            // Status
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(row.transform, false);
            var statusTxt = statusGo.AddComponent<Text>();
            statusTxt.font = cnFont; statusTxt.fontSize = 14;
            statusTxt.alignment = TextAnchor.MiddleCenter;
            statusTxt.text = s.Survived ? $"存活 ({s.FinalHp:F0}/{s.MaxHp:F0})" : "阵亡";
            statusTxt.color = s.Survived ? new Color(0.2f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.3f);
            var statusLe = statusGo.AddComponent<LayoutElement>();
            statusLe.preferredWidth = 120; statusLe.minWidth = 90;

            // Damage bar
            var dmgGo = new GameObject("Damage");
            dmgGo.transform.SetParent(row.transform, false);
            var dmgTxt = dmgGo.AddComponent<Text>();
            dmgTxt.font = cnFont; dmgTxt.fontSize = 14; dmgTxt.color = Color.white;
            dmgTxt.alignment = TextAnchor.MiddleRight;

            var sb = new StringBuilder();
            sb.Append($"伤害 {s.DamageDealt:F0}");
            if (s.Kills > 0) sb.Append($" 击杀{s.Kills}");
            dmgTxt.text = sb.ToString();
            var dmgLe = dmgGo.AddComponent<LayoutElement>();
            dmgLe.preferredWidth = 200; dmgLe.minWidth = 140;
            dmgLe.flexibleWidth = 1;

            // Damage type breakdown (small)
            var detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(row.transform, false);
            var detailTxt = detailGo.AddComponent<Text>();
            detailTxt.font = cnFont; detailTxt.fontSize = 12; detailTxt.color = new Color(0.7f, 0.7f, 0.7f);
            detailTxt.alignment = TextAnchor.MiddleRight;

            var parts = new List<string>();
            if (s.MeleeDamageDealt > 0) parts.Add($"近{s.MeleeDamageDealt:F0}");
            if (s.RangedDamageDealt > 0) parts.Add($"远{s.RangedDamageDealt:F0}");
            if (s.BeamDamageDealt > 0) parts.Add($"光{s.BeamDamageDealt:F0}");
            if (s.ExplosionDamageDealt > 0) parts.Add($"爆{s.ExplosionDamageDealt:F0}");
            if (s.DotDamageDealt > 0) parts.Add($"毒{s.DotDamageDealt:F0}");
            detailTxt.text = string.Join(" ", parts);
            var detailLe = detailGo.AddComponent<LayoutElement>();
            detailLe.preferredWidth = 160;

            // Background tint
            var rowImg = row.GetComponent<Image>();
            if (rowImg == null)
                rowImg = row.AddComponent<Image>();
            rowImg.color = isMvp
                ? new Color(1f, 0.85f, 0.15f, 0.3f)
                : new Color(0.15f, 0.15f, 0.18f, 0.6f);
        }

        void OnRestart() { if (_gm != null) _gm.ResetToShop(); }
        void OnMainMenu() { if (_gm != null) _gm.ReturnToMainMenu(); }
    }
}
