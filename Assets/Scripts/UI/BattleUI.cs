using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    public class BattleUI : MonoBehaviour
    {
        [Header("引用")]
        public Text statusText;
        public Text timerText;
        private GameManager _gm;
        private bool _battleEnded = false;

        void Start() { _gm = GameManager.Instance; }

        public void Show() { gameObject.SetActive(true); _battleEnded = false; }
        public void Hide() { gameObject.SetActive(false); }

        void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm?.BattleBridge?.Simulator == null) return;

            var sim = _gm.BattleBridge.Simulator;

            if (timerText != null) timerText.text = $"时间: {sim.ElapsedTime:F1}s";

            int alive0 = 0, alive1 = 0;
            var units = sim.State.Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].State == UnitStateEnum.Dead) continue;
                if (units[i].Team == 0) alive0++; else alive1++;
            }

            if (statusText != null) statusText.text = $"蓝方: {alive0}  vs  红方: {alive1}";

            if (!_battleEnded && sim.IsFinished && _gm.Phase == GamePhase.Battle)
            {
                _battleEnded = true;
                _gm.OnBattleEnd(sim.Winner);
            }
        }
    }
}
