using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary> 部署 UI：点击战场放置单位 </summary>
    public class DeployUI : MonoBehaviour
    {
        [Header("引用")]
        public RectTransform battlefieldRect; // 战场区域的 RectTransform
        public Transform unitMarkerParent;
        public GameObject unitMarkerPrefab;
        public Text deployHint;
        public Button autoDeployButton;
        public Button startBattleButton;

        private GameManager _gm;
        private readonly List<GameObject> _markers = new();

        void Start()
        {
            _gm = GameManager.Instance;
            autoDeployButton.onClick.AddListener(OnAutoDeploy);
            startBattleButton.onClick.AddListener(OnStartBattle);
        }

        public void Show() { gameObject.SetActive(true); Refresh(); }
        public void Hide() { gameObject.SetActive(false); }

        void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_gm == null || _gm.Phase != GamePhase.Deploy) return;

            // 检测点击战场
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    battlefieldRect, Input.mousePosition, null, out localPoint);

                // 转换为世界坐标
                Rect rect = battlefieldRect.rect;
                float worldX = (localPoint.x - rect.x) / rect.width * BattleConstants.FIELD_WIDTH;
                float worldY = (localPoint.y - rect.y) / rect.height * BattleConstants.FIELD_HEIGHT;

                _gm.PlaceUnit(new Vector2(worldX, worldY));
            }
        }

        public void Refresh()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null) return;

            // 清理旧标记
            foreach (var m in _markers)
                if (m != null) Destroy(m);
            _markers.Clear();

            // 显示已部署单位
            foreach (var dep in _gm.DeployedUnits)
            {
                var marker = Instantiate(unitMarkerPrefab, unitMarkerParent);
                _markers.Add(marker);

                var def = _gm.Database.GetById(dep.MonsterId);
                var img = marker.GetComponent<Image>();
                if (img != null && def != null && def.idleSprite != null)
                {
                    img.sprite = def.idleSprite;
                    img.preserveAspect = true;
                }
                img.color = dep.Team == 0 ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(1f, 0.4f, 0.3f, 1f);

                // 定位
                var rt = marker.GetComponent<RectTransform>();
                Rect rect = battlefieldRect.rect;
                float localX = dep.X / BattleConstants.FIELD_WIDTH * rect.width + rect.x;
                float localY = dep.Y / BattleConstants.FIELD_HEIGHT * rect.height + rect.y;
                rt.anchoredPosition = new Vector2(localX, localY);

                // 名称
                var nameText = marker.transform.Find("Name")?.GetComponent<Text>();
                if (nameText != null && def != null)
                    nameText.text = def.displayName;
            }

            // 更新提示
            int remain0 = _gm.GetRemainingCount(0);
            int remain1 = _gm.GetRemainingCount(1);
            string teamName = _gm.ActiveTeam == 0 ? "蓝方" : "红方";
            deployHint.text = $"部署中：{teamName}  |  蓝方待放: {remain0}  红方待放: {remain1}";

            startBattleButton.interactable = _gm.AllDeployed();
        }

        void OnAutoDeploy()
        {
            _gm.AutoDeploy();
        }

        void OnStartBattle()
        {
            _gm.StartBattle();
        }
    }
}
