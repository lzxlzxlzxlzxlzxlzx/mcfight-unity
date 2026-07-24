using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    public class CodexUI : MonoBehaviour
    {
        public Transform cardGridParent;
        public ScrollRect cardGridScroll;
        public GameObject cardPrefab;
        public Button backButton;

        public GameObject detailPanel;
        public Text detailText;
        public Button detailBackButton;

        [Header("Filter")]
        public MonsterFilterBar filterBar;

        private GameManager _gm;
        private List<MonsterDefSO> _allMonsters;

        void Start()
        {
            _gm = GameManager.Instance;
            if (backButton) backButton.onClick.AddListener(OnBack);
            if (detailBackButton) detailBackButton.onClick.AddListener(CloseDetail);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null) return;
            _allMonsters = new List<MonsterDefSO>(_gm.Database.GetAllSortedByPrice());
            if (cardGridParent == null) return;
            if (detailPanel != null) detailPanel.SetActive(false);

            // Init filter bar
            if (filterBar != null)
                filterBar.Init(PopulateCards);

            PopulateCards();
        }

        public void Hide() { gameObject.SetActive(false); }

        void PopulateCards()
        {
            if (cardGridParent == null) return;

            for (int i = cardGridParent.childCount - 1; i >= 0; i--)
                Destroy(cardGridParent.GetChild(i).gameObject);

            var filtered = (filterBar != null && _allMonsters != null)
                ? filterBar.Apply(_allMonsters)
                : _allMonsters;

            if (filtered == null) return;

            foreach (var def in filtered)
            {
                if (def.price <= 0) continue;
                var card = Instantiate(cardPrefab, cardGridParent);
                card.SetActive(true);
                SetupCard(card, def);
            }
        }

        void SetupCard(GameObject card, MonsterDefSO def)
        {
            // Try MonsterCardView first
            var cardView = card.GetComponent<MonsterCardView>();
            if (cardView != null)
            {
                cardView.Bind(def, MonsterCardView.Mode.Codex, _gm);
                // Add click listener for detail
                var cardBtn = card.GetComponent<Button>();
                if (cardBtn == null) cardBtn = card.AddComponent<Button>();
                cardBtn.onClick.RemoveAllListeners();
                cardBtn.onClick.AddListener(() => ShowDetail(def));
                return;
            }

            // Fallback: manual setup
            var bg = card.GetComponent<Image>();
            if (bg != null) bg.color = MonsterCardView.GetRarityColor(def.price);

            var art = card.transform.Find("Art")?.GetComponent<Image>();
            if (art != null && def.idleSprite != null) { art.sprite = def.idleSprite; art.preserveAspect = true; }

            var nameTxt = card.transform.Find("Name/NameText")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = def.displayName;

            var costTxt = card.transform.Find("Cost/Value")?.GetComponent<Text>();
            if (costTxt != null) costTxt.text = def.price.ToString();

            var statsTxt = card.transform.Find("Stats")?.GetComponent<Text>();
            if (statsTxt != null)
                statsTxt.text = $"HP {def.hp:F0}  ATK {def.attack:F0}" + (def.armor > 0 ? $"  ARM {def.armor:F0}" : "");

            var btn = card.GetComponent<Button>();
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => ShowDetail(def)); }
        }

        void ShowDetail(MonsterDefSO def)
        {
            if (detailPanel == null || detailText == null) return;
            detailPanel.SetActive(true);

            var sb = new StringBuilder();
            sb.Append($"<size=32><b>{def.displayName}</b></size>\n\n");
            sb.Append($"<size=22><color=#FFD700>Price: {def.price}G</color></size>\n\n");
            sb.Append($"<size=20><b>Base Stats</b></size>\n");
            sb.Append($"HP: {def.hp}\n");
            sb.Append($"ATK: {def.attack}\n");
            sb.Append($"Armor: {def.armor}" + (def.armorToughness > 0 ? $" (Toughness: {def.armorToughness})" : "") + "\n");
            sb.Append($"Speed: {def.moveSpeed}\n");
            sb.Append($"Range: {def.attackRange}\n");
            sb.Append($"Interval: {def.attackInterval}s\n");
            sb.Append($"Radius: {def.radius}\n");
            sb.Append($"Type: {(def.moveType == MoveType.Fly ? "Fly" : "Ground")} / {(def.attackType == AttackType.Ranged ? "Ranged" : "Melee")}\n\n");

            if (def.tags != null && def.tags.Length > 0)
            {
                sb.Append("<size=20><b>Tags</b></size>\n");
                foreach (var t in def.tags) sb.Append($"[{t}] ");
                sb.Append("\n\n");
            }

            if (def.onHitEffects != null && def.onHitEffects.Length > 0)
            {
                sb.Append("<size=20><b>On Hit</b></size>\n");
                foreach (var e in def.onHitEffects) sb.Append($"[{e}] ");
                sb.Append("\n\n");
            }

            sb.Append("<size=20><b>Ability</b></size>\n");
            sb.Append($"Type: {(string.IsNullOrEmpty(def.abilityComponentType) ? "Standard" : def.abilityComponentType)}\n\n");

            sb.Append("<size=18><color=#AAAAAA>" + GetSkillDescription(def.monsterId) + "</color></size>");

            detailText.text = sb.ToString();
        }

        void CloseDetail()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        string GetSkillDescription(string monsterId)
        {
            return monsterId switch
            {
                "alexscaves_tremorzilla" => "AOE Stomp: 80px radius, 30 dmg, 1.0s interval\nSuper Beam: 5s beam, 15 ticks x 20 dmg = 300 total, range 500, cooldown 20s",
                "alexscaves_luxtructosaurus" => "Leap: jump to target, 48px AOE, 12 dmg, cd 10s\nTail/Stomp alternate: 96-112px AOE, 12 dmg\nMeteor passive: every 3s, 20 dmg + lava\nFire immune",
                "cataclysm_ancient_remnant" => "5 random skills: Bite(34+5%maxHp)/Tail(26+5%maxHp)/Sandstorm(3 orbiting tornadoes)/Stomp(cone 23+3.5%maxHp)/Obelisk(7-ring barrage 18+5%maxHp, cd 20s)\nAll cast 3s",
                "cataclysm_the_harbinger" => "Mode switch every 15s: Wither missiles(8dmg)/Laser(5dmg)\n4-skill cycle every 5s: Homing missiles(6x3)/Grenades(8x20)/Charge(11+6%maxHp)/Death Ray(5s,10+5%maxHp/s)\nPassive: 2HP/s regen + 5HP per kill",
                "warden" => "Melee: 30 dmg, 1.5s interval\nSonic: 10 dmg, range 220, cd 10s\nSpeed 78 (fastest ground unit)",
                "cataclysm_kobolediator" => "Charge: dash to target, 18 dmg AOE\nTriple Slash: 3s, 2x cone 14 + 18 finale\nStomp: cone 224px, 14 dmg\nBlocks 50% ranged. Cannot hit air.",
                _ => "Standard attack pattern",
            };
        }

        void OnBack() { if (_gm != null) _gm.ExitCodex(); }
    }
}
