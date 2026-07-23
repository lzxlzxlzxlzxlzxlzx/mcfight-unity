using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    public class ShopUI : MonoBehaviour
    {
        public Transform cardGridParent;
        public ScrollRect cardGridScroll;
        public GameObject cardPrefab;

        public Text goldText;
        public Text teamLabel;
        public Button deployButton;
        public Button autoDeployButton;

        private List<MonsterDefSO> _sortedMonsters;
        private GameManager _gm;
        private ScrollRect _scrollRect;

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null) return;
            if (_sortedMonsters == null || _sortedMonsters.Count == 0)
                _sortedMonsters = new List<MonsterDefSO>(_gm.Database.GetAllSortedByPrice());

            if (deployButton != null) { deployButton.onClick.RemoveAllListeners(); deployButton.onClick.AddListener(OnDeployClick); }
            if (autoDeployButton != null) { autoDeployButton.onClick.RemoveAllListeners(); autoDeployButton.onClick.AddListener(OnAutoDeployClick); }

            var team0Btn = transform.Find("Team0Btn")?.GetComponent<Button>();
            var team1Btn = transform.Find("Team1Btn")?.GetComponent<Button>();
            if (team0Btn != null) { team0Btn.onClick.RemoveAllListeners(); team0Btn.onClick.AddListener(() => _gm.SwitchTeam(0)); }
            if (team1Btn != null) { team1Btn.onClick.RemoveAllListeners(); team1Btn.onClick.AddListener(() => _gm.SwitchTeam(1)); }

            var deployBtn = transform.Find("DeployBtn")?.GetComponent<Button>();
            var autoBtn = transform.Find("AutoBtn")?.GetComponent<Button>();
            if (deployBtn != null) { deployBtn.onClick.RemoveAllListeners(); deployBtn.onClick.AddListener(OnDeployClick); }
            if (autoBtn != null) { autoBtn.onClick.RemoveAllListeners(); autoBtn.onClick.AddListener(OnAutoDeployClick); }

            Refresh();
        }

        void Update()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponentInChildren<ScrollRect>();
            if (_scrollRect != null && gameObject.activeSelf)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0)
                    _scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + scroll * 0.15f);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm != null && (_sortedMonsters == null || _sortedMonsters.Count == 0))
                _sortedMonsters = new List<MonsterDefSO>(_gm.Database.GetAllSortedByPrice());
            Refresh();
        }
        public void Hide() { gameObject.SetActive(false); }

        public void Refresh()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null) return;

            if (goldText == null) return;
            goldText.text = $"蓝方: {_gm.Gold[0]}G  |  红方: {_gm.Gold[1]}G";
            if (teamLabel == null) return;
            teamLabel.text = _gm.ActiveTeam == 0 ? "蓝方" : "红方";
            teamLabel.color = _gm.ActiveTeam == 0 ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.3f);

            if (deployButton != null) deployButton.interactable = _gm.CanStartDeploy();
            if (autoDeployButton != null) autoDeployButton.interactable = _gm.CanStartDeploy();

            var dBtn = transform.Find("DeployBtn")?.GetComponent<Button>();
            var aBtn = transform.Find("AutoBtn")?.GetComponent<Button>();
            if (dBtn != null) dBtn.interactable = _gm.CanStartDeploy();
            if (aBtn != null) aBtn.interactable = _gm.CanStartDeploy();

            if (cardGridParent == null) return;
            for (int i = cardGridParent.childCount - 1; i >= 0; i--)
                Destroy(cardGridParent.GetChild(i).gameObject);

            foreach (var def in _sortedMonsters)
            {
                if (def.price <= 0) continue;
                var card = Instantiate(cardPrefab, cardGridParent);
                card.SetActive(true);
                SetupCard(card, def);
            }
        }

        void SetupCard(GameObject card, MonsterDefSO def)
        {
            var bg = card.GetComponent<Image>();
            if (bg != null) bg.color = GetRarityColor(def.price);

            var art = card.transform.Find("Art")?.GetComponent<Image>();
            if (art != null && def.idleSprite != null)
            {
                art.sprite = def.idleSprite;
                art.preserveAspect = true;
            }

            var nameTxt = card.transform.Find("Name/NameText")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = def.displayName;

            var costTxt = card.transform.Find("Cost/Value")?.GetComponent<Text>();
            if (costTxt != null) costTxt.text = def.price.ToString();

            var statsTxt = card.transform.Find("Stats")?.GetComponent<Text>();
            if (statsTxt != null)
                statsTxt.text = $"HP {def.hp:F0}  ATK {def.attack:F0}" +
                    (def.armor > 0 ? $"  ARM {def.armor:F0}" : "");

            var buyBtn = card.transform.Find("BuyBtn")?.GetComponent<Button>();
            if (buyBtn != null)
            {
                bool canAfford = _gm.Gold[_gm.ActiveTeam] >= def.price;
                buyBtn.interactable = canAfford;
                var buyTxt = buyBtn.transform.Find("Text")?.GetComponent<Text>();
                if (buyTxt != null) buyTxt.text = canAfford ? "购买" : "不足";

                string id = def.monsterId;
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(() => _gm.BuyMonster(id, 1));
            }

            var bulkBtn = card.transform.Find("BulkBtn")?.GetComponent<Button>();
            if (bulkBtn != null)
            {
                int maxBatch = Mathf.Min(BattleConstants.BULK_BUY_COUNT, Mathf.FloorToInt(_gm.Gold[_gm.ActiveTeam] / Mathf.Max(1, def.price)));
                bulkBtn.interactable = maxBatch > 0;
                var bulkTxt = bulkBtn.transform.Find("Text")?.GetComponent<Text>();
                if (bulkTxt != null) bulkTxt.text = maxBatch > 0 ? $"×{maxBatch}" : "×0";

                string id = def.monsterId;
                int max = maxBatch;
                bulkBtn.onClick.RemoveAllListeners();
                bulkBtn.onClick.AddListener(() => { if (max > 0) _gm.BuyMonster(id, max); });
            }

            var countTxt = card.transform.Find("Count")?.GetComponent<Text>();
            if (countTxt != null)
            {
                int count0 = _gm.ShopEntries.FindAll(e => e.MonsterId == def.monsterId && e.Team == 0).Count;
                int count1 = _gm.ShopEntries.FindAll(e => e.MonsterId == def.monsterId && e.Team == 1).Count;
                countTxt.text = (count0 > 0 || count1 > 0) ? $"蓝{count0} 红{count1}" : "";
                countTxt.gameObject.SetActive(count0 > 0 || count1 > 0);
            }
        }

        Color GetRarityColor(int price)
        {
            if (price >= 700) return new Color(0.85f, 0.45f, 0.05f, 1f);
            if (price >= 400) return new Color(0.55f, 0.25f, 0.7f, 1f);
            if (price >= 150) return new Color(0.15f, 0.3f, 0.8f, 1f);
            if (price >= 50) return new Color(0.1f, 0.55f, 0.2f, 1f);
            return new Color(0.5f, 0.5f, 0.55f, 1f);
        }

        void OnDeployClick() { _gm.StartDeploy(); }

        void OnAutoDeployClick()
        {
            if (_gm.Gold[0] > 0) AutoBuyTeam(0);
            if (_gm.Gold[1] > 0) AutoBuyTeam(1);
            _gm.AutoDeploy();
            _gm.StartBattle();
        }

        void AutoBuyTeam(int team)
        {
            _gm.ActiveTeam = team;
            foreach (var def in _sortedMonsters)
            {
                if (def.price <= 0) continue;
                while (_gm.Gold[team] >= def.price)
                {
                    _gm.Gold[team] -= def.price;
                    _gm.ShopEntries.Add(new ShopEntry { MonsterId = def.monsterId, Team = team });
                }
            }
        }
    }
}