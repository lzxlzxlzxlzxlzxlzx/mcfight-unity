using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary>
    /// 部署 UI：顶部卡片栏（按怪物类型合并显示数量）+ 点击战场放置。
    /// </summary>
    public class DeployUI : MonoBehaviour
    {
        [Header("引用")]
        public Transform cardGridParent;
        public GameObject cardPrefab;
        public Transform markerLayer;
        public Text deployHint;
        public Button autoDeployButton;
        public Button startBattleButton;

        [Header("队伍切换")]
        public Button teamToggleBtn;
        public Text teamToggleText;

        private GameManager _gm;
        private readonly List<GameObject> _cardObjs = new();
        private string _selectedMonsterId = null;
        private Camera _battleCam;

        void Start()
        {
            _gm = GameManager.Instance;
            if (autoDeployButton) { autoDeployButton.onClick.RemoveAllListeners(); autoDeployButton.onClick.AddListener(OnAutoDeploy); }
            if (startBattleButton) { startBattleButton.onClick.RemoveAllListeners(); startBattleButton.onClick.AddListener(OnStartBattle); }
            if (teamToggleBtn) { teamToggleBtn.onClick.RemoveAllListeners(); teamToggleBtn.onClick.AddListener(OnTeamToggle); }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_gm == null) _gm = GameManager.Instance;
            _selectedMonsterId = null;
            Refresh();
        }

        public void Hide() { gameObject.SetActive(false); }

        void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_gm == null || _gm.Phase != GamePhase.Deploy) return;

            if (Input.GetMouseButtonDown(1)) { _selectedMonsterId = null; Refresh(); }

            if (Input.GetMouseButtonDown(0))
            {
                if (string.IsNullOrEmpty(_selectedMonsterId)) return;

                // Only block if clicking on a UI Button or InputField
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null)
                {
                    var selected = es.currentSelectedGameObject;
                    // Allow clicks if not over an interactive UI element
                    // (panel background raycastTarget is now false, so it won't block)
                }

                Vector2 worldPos = GetWorldPosFromMouse();
                bool success = PlaceUnitByMonsterId(worldPos);
                if (success)
                {
                    // Keep selected if there are more of this type
                    int remaining = 0;
                    foreach (var e in _gm.ShopEntries)
                        if (e.Team == _gm.ActiveTeam && e.MonsterId == _selectedMonsterId)
                            remaining++;
                    if (remaining == 0) _selectedMonsterId = null;
                }
            }
        }

        bool PlaceUnitByMonsterId(Vector2 worldPos)
        {
            if (_gm == null || string.IsNullOrEmpty(_selectedMonsterId)) return false;

            // Find first ShopEntry of this monsterId for current team
            for (int i = 0; i < _gm.ShopEntries.Count; i++)
            {
                if (_gm.ShopEntries[i].Team == _gm.ActiveTeam && _gm.ShopEntries[i].MonsterId == _selectedMonsterId)
                {
                    return _gm.PlaceSpecificUnit(i, worldPos);
                }
            }
            return false;
        }

        Vector2 GetWorldPosFromMouse()
        {
            if (_battleCam == null)
            {
                _battleCam = Camera.main;
                if (_battleCam == null)
                {
                    var bridge = FindObjectOfType<BattleBridge>();
                    if (bridge != null && bridge.battleCamera != null)
                        _battleCam = bridge.battleCamera;
                }
            }

            if (_battleCam != null && _battleCam.orthographic)
            {
                Vector3 worldPos = _battleCam.ScreenToWorldPoint(Input.mousePosition);
                return new Vector2(worldPos.x, worldPos.y);
            }

            float x = (float)Input.mousePosition.x / Screen.width * BattleConstants.FIELD_WIDTH;
            float y = (float)Input.mousePosition.y / Screen.height * BattleConstants.FIELD_HEIGHT;
            return new Vector2(x, y);
        }

        public void Refresh()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null) return;

            // Update hint
            int remain0 = _gm.GetRemainingCount(0);
            int remain1 = _gm.GetRemainingCount(1);
            if (deployHint != null)
            {
                string teamName = _gm.ActiveTeam == 0 ? "蓝方" : "红方";
                string selected = !string.IsNullOrEmpty(_selectedMonsterId) ? "  [已选中]" : "";
                string hint = remain0 == 0 && remain1 == 0 ? "所有单位已部署完毕" : $"当前: {teamName}{selected}  |  蓝待放: {remain0}  红待放: {remain1}  |  左键放置 右键取消";
                deployHint.text = hint;
            }

            if (teamToggleText != null)
                teamToggleText.text = _gm.ActiveTeam == 0 ? "蓝方" : "红方";
            if (teamToggleBtn != null)
                teamToggleBtn.interactable = true; // always allow toggling teams

            if (startBattleButton != null)
                startBattleButton.interactable = remain0 == 0 && remain1 == 0 || _gm.Mode == GameMode.PvAI && remain0 == 0;

            // Update placed unit markers
            UpdateMarkers();

            // Rebuild card grid - group by monsterId, show count
            if (cardGridParent == null) return;
            for (int i = _cardObjs.Count - 1; i >= 0; i--)
                if (_cardObjs[i] != null) Destroy(_cardObjs[i].gameObject);
            _cardObjs.Clear();

            // Count monsters by type for current team
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var entry in _gm.ShopEntries)
            {
                if (entry.Team != _gm.ActiveTeam) continue;
                if (!counts.ContainsKey(entry.MonsterId))
                {
                    counts[entry.MonsterId] = 0;
                    order.Add(entry.MonsterId);
                }
                counts[entry.MonsterId]++;
            }

            // Create one card per monster type
            foreach (var monsterId in order)
            {
                int count = counts[monsterId];
                var def = _gm.Database.GetById(monsterId);
                var card = Instantiate(cardPrefab, cardGridParent);
                card.SetActive(true);
                _cardObjs.Add(card);
                SetupDeployCard(card, def, monsterId, count);
            }
        }

        void SetupDeployCard(GameObject card, MonsterDefSO def, string monsterId, int count)
        {
            var cnFont = Resources.Load<Font>("Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
            if (cnFont == null) cnFont = AssetDatabaseLoadFont();

            // Art
            var art = card.transform.Find("Art")?.GetComponent<Image>();
            if (art != null && def != null && def.idleSprite != null)
            {
                art.sprite = def.idleSprite;
                art.preserveAspect = true;
            }

            // Name
            var nameTxt = card.transform.Find("Name/NameText")?.GetComponent<Text>();
            if (nameTxt != null && def != null)
            {
                nameTxt.text = def.displayName;
                nameTxt.font = cnFont;
                nameTxt.fontSize = 14;
                nameTxt.color = Color.white;
            }

            // Count badge (use Cost/Value to show count)
            var costTxt = card.transform.Find("Cost/Value")?.GetComponent<Text>();
            if (costTxt != null)
            {
                costTxt.text = $"×{count}";
                costTxt.font = cnFont;
                costTxt.fontSize = 18;
                costTxt.color = count > 0 ? new Color(1f, 0.85f, 0.1f) : new Color(0.5f, 0.5f, 0.5f);
            }

            // Hide buy/bulk buttons
            var buyBtn = card.transform.Find("BuyBtn")?.GetComponent<Button>();
            if (buyBtn != null) buyBtn.gameObject.SetActive(false);
            var bulkBtn = card.transform.Find("BulkBtn")?.GetComponent<Button>();
            if (bulkBtn != null) bulkBtn.gameObject.SetActive(false);

            // Card click handler
            var cardBtn = card.GetComponent<Button>();
            if (cardBtn == null) cardBtn = card.AddComponent<Button>();
            cardBtn.interactable = count > 0;

            string id = monsterId;
            cardBtn.onClick.RemoveAllListeners();
            cardBtn.onClick.AddListener(() =>
            {
                _selectedMonsterId = id;
                Refresh();
            });

            // Highlight if selected
            var cardImg = card.GetComponent<Image>();
            if (cardImg != null)
            {
                bool isSelected = _selectedMonsterId == monsterId;
                cardImg.color = isSelected
                    ? new Color(0.4f, 0.6f, 1f, 0.95f)
                    : new Color(0.2f, 0.2f, 0.25f, 0.95f);
            }
        }

        Font AssetDatabaseLoadFont()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
#else
            return null;
#endif
        }

        void OnTeamToggle()
        {
            if (_gm != null) _gm.SwitchTeam(_gm.ActiveTeam == 0 ? 1 : 0);
            _selectedMonsterId = null;
            Refresh();
        }

        void OnAutoDeploy()
        {
            if (_gm != null)
            {
                _gm.AutoDeploy();
                _gm.StartBattle();
            }
        }

        void OnStartBattle()
        {
            if (_gm != null) _gm.StartBattle();
        }

        void UpdateMarkers()
        {
            if (markerLayer == null) return;

            // Clear old markers
            for (int i = markerLayer.childCount - 1; i >= 0; i--)
                Destroy(markerLayer.GetChild(i).gameObject);

            // Create marker for each deployed unit
            var cnFont = Resources.Load<Font>("Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
            if (cnFont == null)
            {
#if UNITY_EDITOR
                cnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
#endif
            }

            foreach (var dep in _gm.DeployedUnits)
            {
                var def = _gm.Database.GetById(dep.MonsterId);
                if (def == null) continue;

                var go = new GameObject($"Marker_{dep.MonsterId}_{dep.Team}");
                go.transform.SetParent(markerLayer, false);

                var img = go.AddComponent<Image>();
                if (def.idleSprite != null)
                {
                    img.sprite = def.idleSprite;
                    img.preserveAspect = true;
                }
                img.color = dep.Team == 0 ? new Color(0.5f, 0.7f, 1f, 0.9f) : new Color(1f, 0.5f, 0.4f, 0.9f);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                // Map world coords (0-1280, 0-720) to panel local coords
                float localX = dep.X / BattleConstants.FIELD_WIDTH;
                float localY = dep.Y / BattleConstants.FIELD_HEIGHT;
                rt.anchorMin = new Vector2(localX, localY);
                rt.anchorMax = new Vector2(localX, localY);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(40, 40);
            }
        }
    }
}
