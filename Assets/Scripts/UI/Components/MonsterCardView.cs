using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary>
    /// 统一怪物卡片视图：商店和图鉴共用。
    /// Shop 模式显示购买按钮，Codex 模式点击打开详情。
    /// </summary>
    public class MonsterCardView : MonoBehaviour
    {
        public enum Mode { Shop, Codex }

        [Header("引用")]
        public Image borderImage;
        public Image artImage;
        public Text nameText;
        public Text costText;
        public Text statsText;
        public Button buyButton;
        public Text buyText;
        public Button bulkButton;
        public Text bulkText;
        public Text countText;
        public GameObject countContainer;
        public Button cardButton;

        private MonsterDefSO _def;
        private Mode _mode;
        private GameManager _gm;

        /// <summary> 初始化卡片 </summary>
        public void Bind(MonsterDefSO def, Mode mode, GameManager gm)
        {
            _def = def;
            _mode = mode;
            _gm = gm;

            // Re-resolve all references to point to THIS instance's children
            // (serialized references may point to original prefab)
            borderImage = GetComponent<Image>();
            artImage = transform.Find("Art")?.GetComponent<Image>();
            nameText = transform.Find("Name/NameText")?.GetComponent<Text>();
            costText = transform.Find("Cost/Value")?.GetComponent<Text>();
            statsText = transform.Find("Stats")?.GetComponent<Text>();
            buyButton = transform.Find("BuyBtn")?.GetComponent<Button>();
            buyText = transform.Find("BuyBtn/Text")?.GetComponent<Text>();
            bulkButton = transform.Find("BulkBtn")?.GetComponent<Button>();
            bulkText = transform.Find("BulkBtn/Text")?.GetComponent<Text>();
            countText = transform.Find("Count")?.GetComponent<Text>();
            countContainer = transform.Find("Count")?.gameObject;

            // Fix card layout
            LayoutCard();

            // Border color by rarity
            if (borderImage != null)
                borderImage.color = GetRarityColor(def.price);

            // Art
            if (artImage != null && def.idleSprite != null)
            {
                artImage.sprite = def.idleSprite;
                artImage.preserveAspect = true;
            }

            // Name
            if (nameText != null)
                nameText.text = def.displayName;

            // Cost
            if (costText != null)
                costText.text = def.price.ToString();

            // Stats
            if (statsText != null)
                statsText.text = $"HP {def.hp:F0}  ATK {def.attack:F0}" +
                    (def.armor > 0 ? $"  ARM {def.armor:F0}" : "");

            if (_mode == Mode.Shop)
            {
                SetupShopMode();
            }
            else
            {
                // Codex mode: hide buy buttons, use card click for detail
                if (buyButton != null) buyButton.gameObject.SetActive(false);
                if (bulkButton != null) bulkButton.gameObject.SetActive(false);
                if (countContainer != null) countContainer.SetActive(false);
            }
        }

        void SetupShopMode()
        {
            if (_gm == null) return;

            // Buy button
            if (buyButton != null)
            {
                bool canAfford = _gm.Gold[_gm.ActiveTeam] >= _def.price;
                buyButton.interactable = canAfford;
                if (buyText != null) buyText.text = canAfford ? "购买" : "不足";

                // Grey out buy button when can't afford
                var buyImg = buyButton.GetComponent<Image>();
                if (buyImg != null)
                    buyImg.color = canAfford ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

                string id = _def.monsterId;
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() =>
                {
                    _gm.BuyMonster(id, 1);
                    // Card pop animation on successful buy
                    StartCoroutine(UIAnimator.CardPop(transform));
                });
            }

            // Bulk buy button
            if (bulkButton != null)
            {
                int maxBatch = Mathf.Min(BattleConstants.BULK_BUY_COUNT,
                    Mathf.FloorToInt(_gm.Gold[_gm.ActiveTeam] / Mathf.Max(1, _def.price)));
                bulkButton.interactable = maxBatch > 0;
                if (bulkText != null) bulkText.text = maxBatch > 0 ? $"×{maxBatch}" : "×0";

                // Grey out bulk button when can't afford
                var bulkImg = bulkButton.GetComponent<Image>();
                if (bulkImg != null)
                    bulkImg.color = maxBatch > 0 ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

                string id = _def.monsterId;
                int max = maxBatch;
                bulkButton.onClick.RemoveAllListeners();
                bulkButton.onClick.AddListener(() => { if (max > 0) { _gm.BuyMonster(id, max); StartCoroutine(UIAnimator.CardPop(transform)); } });
            }

            // Count badge
            if (countText != null && countContainer != null)
            {
                int count0 = _gm.ShopEntries.FindAll(e => e.MonsterId == _def.monsterId && e.Team == 0).Count;
                int count1 = _gm.ShopEntries.FindAll(e => e.MonsterId == _def.monsterId && e.Team == 1).Count;
                countText.text = (count0 > 0 || count1 > 0) ? $"蓝{count0} 红{count1}" : "";
                countContainer.SetActive(count0 > 0 || count1 > 0);
            }
        }

        public static Color GetRarityColor(int price)
        {
            if (price >= 600) return new Color(1.0f, 0.80f, 0.15f, 1f);   // 亮金色
            if (price >= 120) return new Color(0.65f, 0.30f, 0.85f, 1f);   // 亮紫色
            if (price >= 50) return new Color(0.90f, 0.50f, 0.10f, 1f);    // 亮橙色
            return new Color(0.85f, 0.85f, 0.80f, 1f);                      // 亮白色
        }

        /// <summary>
        /// 修复卡片子元素的 RectTransform 布局。
        /// 在 Bind() 时调用，确保即使 Prefab 的值没保存也能正确显示。
        /// 卡片尺寸 145x215，从上到下：
        /// Cost(24) → Art(填充中间) → Name(22) → Stats(18) → BuyBtn+BulkBtn(26) → Count(14)
        /// </summary>
        void LayoutCard()
        {
            // Card is 145x215. Use absolute positioning (center anchors) instead of stretch.
            // Layout from top to bottom:
            // Cost: y=+95.5, h=24 | Art: y=+27.5, h=111 | Name: y=-43.5, h=22
            // Stats: y=-61.5, h=18 (if exists)
            // BuyBtn: y=-78, h=26 (left half) | BulkBtn: y=-78, h=26 (right half)
            // Count: y=-97, h=14
            SetAbs("Border", 0, 0, 149, 219);
            SetAbs("Cost", 0, 95.5f, 145, 24);
            SetAbs("Art", 0, 27.5f, 141, 111);
            SetAbs("Name", 0, -43.5f, 141, 22);
            SetAbs("Stats", 0, -61.5f, 141, 18);
            SetAbs("BuyBtn", -36, -78, 70, 26);
            SetAbs("BulkBtn", 36, -78, 70, 26);
            SetAbs("Count", 0, -97, 141, 14);
        }

        void SetAbs(string childName, float xPos, float yPos, float width, float height)
        {
            var child = transform.Find(childName);
            if (child == null) return;
            var rt = child.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
