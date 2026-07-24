using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 统一 UI 主题：颜色、字体、Sprite 引用。
    /// 所有 UI 组件引用此 ScriptableObject，换肤只改一处。
    /// </summary>
    [CreateAssetMenu(menuName = "MC Fight/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("队伍颜色")]
        public Color TeamBlue = new(0.30f, 0.60f, 1.00f);
        public Color TeamRed = new(1.00f, 0.40f, 0.30f);
        public Color TeamBlueDim = new(0.20f, 0.40f, 0.70f, 0.6f);
        public Color TeamRedDim = new(0.70f, 0.25f, 0.20f, 0.6f);

        [Header("稀有度颜色")]
        public Color RarityCommon = new(0.55f, 0.55f, 0.60f);
        public Color RarityRare = new(0.15f, 0.55f, 0.25f);
        public Color RarityEpic = new(0.55f, 0.25f, 0.70f);
        public Color RarityLegendary = new(0.85f, 0.45f, 0.05f);

        [Header("功能色")]
        public Color Gold = new(1.00f, 0.84f, 0.00f);
        public Color Success = new(0.20f, 0.80f, 0.30f);
        public Color Danger = new(0.90f, 0.25f, 0.20f);

        [Header("面板")]
        public Color PanelOverlay = new(0, 0, 0, 0.55f);
        public Color PanelBg = new(0.12f, 0.12f, 0.15f, 0.92f);

        [Header("按钮 Sprite")]
        public Sprite BtnBlueNormal;
        public Sprite BtnBlueHover;
        public Sprite BtnRedNormal;
        public Sprite BtnRedHover;
        public Sprite BtnGreenNormal;
        public Sprite BtnGreenHover;
        public Sprite BtnYellowNormal;
        public Sprite BtnGreyNormal;
        public Sprite BtnGreyHover;

        [Header("面板 Sprite")]
        public Sprite PanelSprite;
        public Sprite PanelModalSprite;

        [Header("其他")]
        public Sprite GoldIcon;
        public Sprite StarIcon;
        public Sprite CloseIcon;

        /// <summary> 根据价格获取稀有度颜色 </summary>
        public Color GetRarityColor(int price)
        {
            if (price >= 600) return RarityLegendary;
            if (price >= 120) return RarityEpic;
            if (price >= 50) return RarityRare;
            return RarityCommon;
        }

        /// <summary> 根据价格获取稀有度等级 </summary>
        public static int GetRarityTier(int price)
        {
            if (price >= 600) return 3;
            if (price >= 120) return 2;
            if (price >= 50) return 1;
            return 0;
        }

        private static UITheme _instance;
        /// <summary> 运行时单例（从 Resources 加载） </summary>
        public static UITheme Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<UITheme>("UITheme");
                return _instance;
            }
        }
    }
}
