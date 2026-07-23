using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary> 赛后结算 UI：统计每个怪物的战斗数据 </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("引用")]
        public Text winnerText;
        public Button restartButton;
        public Button mainMenuButton;
        public Image overlay;
        public Text statsText;
        public ScrollRect statsScroll;

        private GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (restartButton) restartButton.onClick.AddListener(OnRestart);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenu);
            // 不在 Start 中 Hide，因为 GameManager 会管理显隐
        }

        public void Show(int winner)
        {
            gameObject.SetActive(true);
            Debug.Log($"[ResultUI] Show called, winner={winner}, active={gameObject.activeSelf}");

            if (winner == 0)
            {
                if (winnerText != null)
                {
                    winnerText.text = "蓝方胜利！";
                    winnerText.color = new Color(0.3f, 0.6f, 1f);
                }
            }
            else if (winner == 1)
            {
                if (winnerText != null)
                {
                    winnerText.text = "红方胜利！";
                    winnerText.color = new Color(1f, 0.4f, 0.3f);
                }
            }
            else
            {
                if (winnerText != null)
                {
                    winnerText.text = "同归于尽！";
                    winnerText.color = Color.white;
                }
            }

            ShowStats();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!gameObject.activeSelf || statsScroll == null) return;
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
                statsScroll.verticalNormalizedPosition =
                    Mathf.Clamp01(statsScroll.verticalNormalizedPosition + scroll * 0.15f);
        }

        void ShowStats()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm?.StatsCollector == null)
            {
                if (statsText != null) statsText.text = "无统计数据";
                return;
            }

            var stats = _gm.StatsCollector.GetAllStats();
            float duration = _gm.StatsCollector.BattleDuration;

            var sb = new StringBuilder();
            sb.AppendLine($"=== 战斗统计 ===");
            sb.AppendLine($"战斗时长: {duration:F1}s");
            sb.AppendLine();

            // 按队伍分组
            for (int team = 0; team < 2; team++)
            {
                string teamName = team == 0 ? "蓝方" : "红方";
                string color = team == 0 ? "#4A9EFF" : "#FF6B4A";
                sb.AppendLine($"<color={color}><b>--- {teamName} ---</b></color>");
                sb.AppendLine();

                // 按 DamageDealt 降序
                var teamStats = new List<UnitBattleStats>();
                foreach (var kv in stats)
                    if (kv.Value.Team == team) teamStats.Add(kv.Value);
                teamStats.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));

                foreach (var s in teamStats)
                {
                    var def = _gm.Database.GetById(s.MonsterId);
                    string name = def != null ? def.displayName : s.MonsterId;
                    string status = s.Survived ? $"<color=#4FFF4F>存活</color> ({s.FinalHp:F0}/{s.MaxHp:F0})" : $"<color=#FF4F4F>阵亡</color>";

                    sb.AppendLine($"<b>{name}</b> [{status}]");
                    sb.AppendLine($"  造成伤害: {s.DamageDealt:F0} (近战{s.MeleeDamageDealt:F0}/远程{s.RangedDamageDealt:F0}/光束{s.BeamDamageDealt:F0}/爆炸{s.ExplosionDamageDealt:F0})");
                    if (s.DotDamageDealt > 0)
                        sb.AppendLine($"  DoT伤害: {s.DotDamageDealt:F0} (中毒/燃烧/凋零)");
                    sb.AppendLine($"  承受伤害: {s.DamageTaken:F0}  击杀: {s.Kills}");

                    // Buff/Debuff 统计
                    var buffs = new List<string>();
                    if (s.PoisonApplied > 0) buffs.Add($"中毒×{s.PoisonApplied}");
                    if (s.BurnApplied > 0) buffs.Add($"燃烧×{s.BurnApplied}");
                    if (s.WitherApplied > 0) buffs.Add($"凋零×{s.WitherApplied}");
                    if (s.SlowApplied > 0) buffs.Add($"减速×{s.SlowApplied}");
                    if (s.FearApplied > 0) buffs.Add($"恐惧×{s.FearApplied}");
                    if (s.FreezeApplied > 0) buffs.Add($"冰冻×{s.FreezeApplied}");
                    if (s.StunApplied > 0) buffs.Add($"蛰晕×{s.StunApplied}");
                    if (buffs.Count > 0) sb.AppendLine($"  施加效果: {string.Join(" ", buffs)}");

                    // 承受的 debuff
                    var received = new List<string>();
                    if (s.PoisonReceived > 0) received.Add($"中毒×{s.PoisonReceived}");
                    if (s.BurnReceived > 0) received.Add($"燃烧×{s.BurnReceived}");
                    if (s.WitherReceived > 0) received.Add($"凋零×{s.WitherReceived}");
                    if (received.Count > 0) sb.AppendLine($"  承受效果: {string.Join(" ", received)}");

                    sb.AppendLine();
                }
            }

            if (statsText != null) statsText.text = sb.ToString();
        }

        void OnRestart()
        {
            if (_gm != null) _gm.ResetToShop();
        }

        void OnMainMenu()
        {
            if (_gm != null) _gm.ReturnToMainMenu();
        }
    }
}
