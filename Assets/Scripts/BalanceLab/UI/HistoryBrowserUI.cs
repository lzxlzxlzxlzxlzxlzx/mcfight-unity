using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    public class HistoryBrowserUI : MonoBehaviour
    {
        private GameObject _panel;
        private Text _listText;
        private InputField _searchInput;
        private ScrollRect _scroll;
        private bool _uiCreated = false;
        private List<ArchiveEntry> _currentList = new();

        void Start() { }

        public void Show() { if (!_uiCreated) CreateUI(); RefreshList(); _panel.SetActive(true); }
        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        void CreateUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogError("[HistoryUI] No Canvas"); return; }

            _panel = LabTheme.CreatePanel("HistoryPanel", 0.15f, 0.08f, 0.85f, 0.92f, canvas.transform, 0.96f);
            _panel.SetActive(false);

            // Title
            LabTheme.CreateText("Title", "📋 测试历史", 0f, 0.94f, 0.88f, 1f, _panel.transform, 20, new Color(0.9f, 0.85f, 0.3f), TextAnchor.MiddleLeft);

            // Close
            var closeBtn = LabTheme.CreateButton("Close", "✕", 0.92f, 0.94f, 1f, 1f, _panel.transform, UIButtonStyled.Style.Danger, 16);
            closeBtn.onClick.AddListener(Hide);

            // Search input
            var searchGo = new GameObject("Search", typeof(RectTransform), typeof(Image), typeof(InputField));
            searchGo.transform.SetParent(_panel.transform, false);
            var srt = searchGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.88f); srt.anchorMax = new Vector2(0.87f, 0.94f);
            srt.offsetMin = new Vector2(0f, 2f); srt.offsetMax = new Vector2(0f, -2f);
            var sImg = searchGo.GetComponent<Image>();
            var sSp = LabTheme.Theme?.PanelSprite;
            if (sSp != null) { sImg.sprite = sSp; sImg.type = Image.Type.Sliced; }
            sImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            _searchInput = searchGo.GetComponent<InputField>();

            var sTxt = LabTheme.CreateText("SText", "", 0f, 0f, 1f, 1f, searchGo.transform, 14, Color.white, TextAnchor.MiddleLeft);
            sTxt.supportRichText = false;
            _searchInput.textComponent = sTxt;
            _searchInput.placeholder = sTxt;

            var phGo = new GameObject("Ph", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(searchGo.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one; phRt.offsetMin = new Vector2(8, 2); phRt.offsetMax = new Vector2(-8, -2);
            var phtxt = phGo.GetComponent<Text>();
            phtxt.font = LabTheme.Font; phtxt.fontSize = 14; phtxt.color = new Color(0.5f, 0.5f, 0.5f);
            phtxt.text = "搜索标题或发现...";
            _searchInput.placeholder = phtxt;
            _searchInput.onValueChanged.AddListener(_ => RefreshList());

            // Refresh button
            var refreshBtn = LabTheme.CreateButton("Refresh", "刷新", 0f, 0.15f, 0.88f, 0.94f, _panel.transform, UIButtonStyled.Style.Secondary, 13);
            var rrt = refreshBtn.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.88f, 0.88f); rrt.anchorMax = new Vector2(1f, 0.94f);
            refreshBtn.onClick.AddListener(RefreshList);

            // Scroll list
            var scrollGo = new GameObject("ListScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_panel.transform, false);
            var lrt = scrollGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.05f); lrt.anchorMax = new Vector2(1f, 0.88f);
            lrt.offsetMin = new Vector2(5f, 5f); lrt.offsetMax = new Vector2(-5f, -5f);
            var lImg = scrollGo.GetComponent<Image>();
            lImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            if (sSp != null) { lImg.sprite = sSp; lImg.type = Image.Type.Sliced; }
            _scroll = scrollGo.GetComponent<ScrollRect>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = crt;

            _listText = LabTheme.CreateText("List", "", 0f, 0f, 1f, 0f, contentGo.transform, 14, Color.white, TextAnchor.UpperLeft);

            LabTheme.CreateText("Hint", "点击数字 1-9 查看报告 | ESC 关闭", 0f, 0f, 1f, 0.05f, _panel.transform, 12, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter);

            _uiCreated = true;
        }

        void RefreshList()
        {
            var index = KnowledgePersistence.LoadIndex();
            _currentList = new List<ArchiveEntry>(index.sessions ?? new List<ArchiveEntry>());
            string search = _searchInput?.text?.Trim();
            if (!string.IsNullOrEmpty(search))
                _currentList = _currentList.FindAll(s => (s.title?.Contains(search) == true) || (s.key_finding?.Contains(search) == true) || (s.session_id?.Contains(search) == true));

            var sb = new StringBuilder();
            if (_currentList.Count == 0) sb.AppendLine("<color=#888888>暂无历史记录</color>");
            else
            {
                for (int i = 0; i < _currentList.Count; i++)
                {
                    var s = _currentList[i];
                    string status = s.status == "Completed" ? "<color=#88FF88>✅ 完成</color>" : "<color=#FF8844>⚠️ 中止</color>";
                    sb.AppendLine($"<color=#FFD700>[{i + 1}]</color> <color=#FFFFFF>{s.title}</color>");
                    sb.AppendLine($"  <color=#AAAAAA>{s.created_at} | {s.completed_matches}/{s.total_matches}场 | {status}</color>");
                    if (!string.IsNullOrEmpty(s.key_finding)) sb.AppendLine($"  <color=#88CCFF>发现: {s.key_finding}</color>");
                    sb.AppendLine();
                }
            }
            _listText.text = sb.ToString();
        }

        void Update()
        {
            if (!_uiCreated || !_panel.activeSelf) return;
            for (int i = 0; i < 9 && i < _currentList.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    var data = KnowledgePersistence.LoadSession(_currentList[i].session_id);
                    if (data != null) GetComponent<ReportViewerUI>()?.ShowFromArchive(data);
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Hide();
        }
    }
}
