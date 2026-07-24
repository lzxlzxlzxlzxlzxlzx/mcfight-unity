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

        [Header("窗口模式")]
        public Button fullscreenToggleBtn;
        public Text fullscreenToggleText;

        private GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (pvpButton) pvpButton.onClick.AddListener(OnPvP);
            if (pvAIButton) pvAIButton.onClick.AddListener(OnPvAI);
            if (codexButton) codexButton.onClick.AddListener(OnCodex);
            if (quitButton) quitButton.onClick.AddListener(OnQuit);
            if (fullscreenToggleBtn) fullscreenToggleBtn.onClick.AddListener(OnToggleFullscreen);
            UpdateFullscreenText();
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }

        void UpdateFullscreenText()
        {
            if (fullscreenToggleText != null)
                fullscreenToggleText.text = Screen.fullScreen ? "全屏模式" : "窗口模式";
        }

        void OnToggleFullscreen()
        {
            if (Screen.fullScreen)
            {
                // 切换到窗口模式
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            }
            else
            {
                // 切换到全屏模式
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
            UpdateFullscreenText();
        }

        void OnPvP() { if (_gm != null) _gm.StartPvP(); }
        void OnPvAI() { if (_gm != null) _gm.StartPvAI(); }
        void OnCodex() { if (_gm != null) _gm.EnterCodex(); }

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
