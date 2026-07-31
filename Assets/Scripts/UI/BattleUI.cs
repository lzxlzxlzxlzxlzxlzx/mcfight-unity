using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    public class BattleUI : MonoBehaviour
    {
        [Header("引用")]
        public Text statusText;
        public Text timerText;
        
        [Header("速度控制")]
        public Button speed1xBtn;
        public Button speed2xBtn;
        public Button speed4xBtn;
        
        private GameManager _gm;
        private bool _battleEnded = false;
        private float _currentSpeed = 1f;

        void Start()
        {
            _gm = GameManager.Instance;
            SetupSpeedButtons();
        }

        void SetupSpeedButtons()
        {
            if (speed1xBtn != null) { speed1xBtn.onClick.RemoveAllListeners(); speed1xBtn.onClick.AddListener(() => SetSpeed(1f)); }
            if (speed2xBtn != null) { speed2xBtn.onClick.RemoveAllListeners(); speed2xBtn.onClick.AddListener(() => SetSpeed(2f)); }
            if (speed4xBtn != null) { speed4xBtn.onClick.RemoveAllListeners(); speed4xBtn.onClick.AddListener(() => SetSpeed(4f)); }
        }

        void SetSpeed(float speed)
        {
            _currentSpeed = speed;
            if (_gm?.BattleBridge != null)
                _gm.BattleBridge.SpeedMultiplier = speed;
            UpdateSpeedButtonColors();
        }

        void UpdateSpeedButtonColors()
        {
            if (speed1xBtn != null) speed1xBtn.interactable = _currentSpeed != 1f;
            if (speed2xBtn != null) speed2xBtn.interactable = _currentSpeed != 2f;
            if (speed4xBtn != null) speed4xBtn.interactable = _currentSpeed != 4f;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _battleEnded = false;
            SetSpeed(1f);
        }

        public void Hide() { gameObject.SetActive(false); }

        void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm?.BattleBridge?.Simulator == null) return;

            var sim = _gm.BattleBridge.Simulator;

            if (timerText != null)
            {
                timerText.text = $"{sim.ElapsedTime:F1}s";
                timerText.color = sim.ElapsedTime > 100f ? new Color(1f, 0.8f, 0f) : Color.white;
            }

            int alive0 = 0, alive1 = 0;
            var units = sim.State.Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].State == UnitStateEnum.Dead) continue;
                if (units[i].Team == 0) alive0++; else alive1++;
            }

            if (statusText != null) statusText.text = $"蓝 {alive0} vs {alive1} 红";

            if (!_battleEnded && sim.IsFinished && _gm.Phase == GamePhase.Battle && !_gm.IsLabMode)
            {
                _battleEnded = true;
                _gm.OnBattleEnd(sim.Winner);
            }
        }
    }
}
