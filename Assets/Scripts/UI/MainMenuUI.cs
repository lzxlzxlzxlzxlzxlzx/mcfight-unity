using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary> 主菜单 UI </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("按钮")]
        public Button pvpButton;
        public Button pvAIButton;
        public Button codexButton;
        public Button quitButton;

        [Header("文本")]
        public Text titleText;
        public Text subtitleText;

        private GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (pvpButton) pvpButton.onClick.AddListener(OnPvP);
            if (pvAIButton) pvAIButton.onClick.AddListener(OnPvAI);
            if (codexButton) codexButton.onClick.AddListener(OnCodex);
            if (quitButton) quitButton.onClick.AddListener(OnQuit);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void OnPvP()
        {
            if (_gm != null) _gm.StartPvP();
        }

        void OnPvAI()
        {
            if (_gm != null) _gm.StartPvAI();
        }

        void OnCodex()
        {
            if (_gm != null) _gm.EnterCodex();
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
