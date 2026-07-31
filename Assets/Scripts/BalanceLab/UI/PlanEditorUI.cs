using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    /// <summary>
    /// P1 计划编辑器 UI：展示/增删/编辑测试用例，导入导出 JSON，启动测试。
    /// </summary>
    public class PlanEditorUI : MonoBehaviour
    {
        private TestPlan _plan;
        private MonsterDatabase _db;

        // UI elements
        private GameObject _panel;
        private Text _titleText;
        private Text _summaryText;
        private Text _validationText;
        private ScrollRect _caseScroll;
        private RectTransform _caseContent;
        private Button _startBtn;
        private Button _importBtn;
        private Button _exportBtn;
        private Button _addCaseBtn;
        private Button _closeBtn;

        // Case editor
        private CaseEditorPanel _caseEditor;

        private bool _uiCreated = false;
        private Font _font;

        public void Initialize(TestPlan plan, MonsterDatabase db)
        {
            _plan = plan;
            _db = db;
            if (!_uiCreated) CreateUI();
            Refresh();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        void LoadFont()
        {
            if (_font == null)
            {
                _font = Resources.Load<Font>("Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        void CreateUI()
        {
            LoadFont();
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogError("[PlanEditorUI] No Canvas"); return; }

            // Main panel — covers most of the screen
            _panel = new GameObject("PlanEditorPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var prt = _panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(80, 40);
            prt.offsetMax = new Vector2(-80, -40);
            var pimg = _panel.GetComponent<Image>();
            pimg.color = new Color(0.06f, 0.06f, 0.10f, 0.97f);

            // Title bar
            var titleGo = CreateText("Title", _panel.transform, "", 20, Color.white);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.offsetMin = new Vector2(10, -40); titleRt.offsetMax = new Vector2(-10, -5);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.alignment = TextAnchor.MiddleCenter;

            // Summary
            var sumGo = CreateText("Summary", _panel.transform, "", 16, new Color(0.7f, 0.8f, 1f));
            var sumRt = sumGo.GetComponent<RectTransform>();
            sumRt.anchorMin = new Vector2(0, 1); sumRt.anchorMax = new Vector2(1, 1);
            sumRt.pivot = new Vector2(0.5f, 1);
            sumRt.offsetMin = new Vector2(10, -65); sumRt.offsetMax = new Vector2(-10, -45);
            _summaryText = sumGo.GetComponent<Text>();
            _summaryText.alignment = TextAnchor.MiddleCenter;

            // Validation
            var valGo = CreateText("Validation", _panel.transform, "", 14, new Color(0.9f, 0.6f, 0.3f));
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0, 1); valRt.anchorMax = new Vector2(1, 1);
            valRt.pivot = new Vector2(0.5f, 1);
            valRt.offsetMin = new Vector2(10, -90); valRt.offsetMax = new Vector2(-10, -70);
            _validationText = valGo.GetComponent<Text>();
            _validationText.alignment = TextAnchor.MiddleCenter;

            // Case list (scroll view)
            var scrollGo = new GameObject("CaseScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(_panel.transform, false);
            var sr = scrollGo.GetComponent<ScrollRect>();
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0); sRt.anchorMax = new Vector2(1, 1);
            sRt.offsetMin = new Vector2(10, 50); sRt.offsetMax = new Vector2(-10, -100);
            scrollGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

            // Viewport
            var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = new Vector2(-20, 0);
            vpGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            sr.viewport = vpRt;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(vpGo.transform, false);
            _caseContent = contentGo.GetComponent<RectTransform>();
            _caseContent.anchorMin = new Vector2(0, 1); _caseContent.anchorMax = new Vector2(1, 1);
            _caseContent.pivot = new Vector2(0.5f, 1); _caseContent.offsetMin = Vector2.zero; _caseContent.offsetMax = Vector2.zero;
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = _caseContent;
            _caseScroll = sr;

            // Bottom buttons
            _importBtn = CreateButton("ImportBtn", "导入JSON", -280, _panel.transform);
            _importBtn.onClick.AddListener(OnImport);
            _exportBtn = CreateButton("ExportBtn", "导出JSON", -140, _panel.transform);
            _exportBtn.onClick.AddListener(OnExport);
            _addCaseBtn = CreateButton("AddBtn", "+ 添加用例", 0, _panel.transform);
            _addCaseBtn.onClick.AddListener(OnAddCase);
            _startBtn = CreateButton("StartBtn", "▶ 开始测试", 140, _panel.transform);
            _startBtn.onClick.AddListener(OnStart);
            _closeBtn = CreateButton("CloseBtn", "关闭", 280, _panel.transform);
            _closeBtn.onClick.AddListener(Hide);

            // Case editor (hidden by default)
            _caseEditor = new CaseEditorPanel(this, _font);

            _uiCreated = true;
        }

        public void Refresh()
        {
            if (_plan == null) return;
            _titleText.text = _plan.Title;
            _summaryText.text = $"{_plan.TotalCases} 个用例 · {_plan.TotalMatches} 场战斗";

            // Validate
            var result = TestPlanLoader.Validate(_plan, _db);
            if (result.IsValid && result.Warnings.Count == 0)
            {
                _validationText.text = "✅ 校验通过";
                _validationText.color = new Color(0.3f, 0.9f, 0.4f);
                _startBtn.interactable = true;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var e in result.Errors) sb.AppendLine($"❌ {e}");
                foreach (var w in result.Warnings) sb.AppendLine($"⚠️ {w}");
                _validationText.text = sb.ToString().TrimEnd();
                _validationText.color = result.IsValid ? new Color(0.9f, 0.7f, 0.2f) : new Color(1f, 0.4f, 0.3f);
                _startBtn.interactable = result.IsValid;
            }

            // Rebuild case list
            for (int i = _caseContent.childCount - 1; i >= 0; i--)
                Destroy(_caseContent.GetChild(i).gameObject);

            for (int i = 0; i < _plan.Cases.Count; i++)
            {
                CreateCaseRow(i, _plan.Cases[i]);
            }
        }

        void CreateCaseRow(int index, TestPlanCase tc)
        {
            var rowGo = new GameObject($"Case_{index}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(_caseContent, false);
            var img = rowGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.20f, 0.9f);
            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Info text
            var infoGo = CreateText("Info", rowGo.transform, "", 14, Color.white);
            var infoLe = infoGo.AddComponent<LayoutElement>();
            infoLe.preferredWidth = 420; infoLe.flexibleWidth = 1;
            var infoText = infoGo.GetComponent<Text>();
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.text = $"#{index + 1}  {tc.Label}\n  {tc.GetSummary(_db)}";

            // Edit button
            var editBtn = CreateButton("EditBtn", "编辑", 0, rowGo.transform, 60, 28);
            editBtn.onClick.AddListener(() => { _caseEditor.Show(_plan, index, _db, () => Refresh()); });

            // Delete button
            var delBtn = CreateButton("DelBtn", "删除", 0, rowGo.transform, 60, 28);
            delBtn.onClick.AddListener(() => { _plan.Cases.RemoveAt(index); _plan.IsModified = true; Refresh(); });
        }

        void OnImport()
        {
            var files = TestPlanLoader.GetAvailableTestFiles();
            if (files.Count == 0)
            {
                Debug.LogWarning("[PlanEditorUI] No .balancetest.json files found in " + TestPlanLoader.TEST_DIR);
                return;
            }
            // P1: load first available file
            var plan = TestPlanLoader.LoadFromFile(files[0]);
            if (plan != null)
            {
                _plan = plan;
                Refresh();
            }
        }

        void OnExport()
        {
            if (_plan == null) return;
            var path = TestPlanLoader.TEST_DIR + $"export_{System.DateTime.Now:yyyyMMdd_HHmmss}.balancetest.json";
            TestPlanLoader.SaveToFile(_plan, path);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        void OnAddCase()
        {
            var tc = new TestPlanCase
            {
                Id = $"case_{_plan.Cases.Count + 1}",
                Label = "新用例",
                RepeatCount = 1
            };
            _plan.Cases.Add(tc);
            _plan.IsModified = true;
            Refresh();
            // Immediately open editor for new case
            _caseEditor.Show(_plan, _plan.Cases.Count - 1, _db, () => Refresh());
        }

        void OnStart()
        {
            if (_plan == null) return;
            var result = TestPlanLoader.Validate(_plan, _db);
            if (!result.IsValid)
            {
                Debug.LogError("[PlanEditorUI] Cannot start: validation failed");
                return;
            }
            Hide();
            var labCases = _plan.ToLabTestCases();
            var controller = UnityEngine.Object.FindObjectOfType<LabSessionController>();
            var ui = controller?.GetComponent<LabUI>();
            if (ui != null) ui.ShowUI();
            controller.StartSession(labCases);
        }

        // ===== Helpers =====

        GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.color = color;
            txt.text = text;
            txt.raycastTarget = false;
            return go;
        }

        Button CreateButton(string name, string label, float x, Transform parent, float w = 110, float h = 32)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            var btn = go.GetComponent<Button>();

            // Auto-layout: if parent has HorizontalLayoutGroup, use LayoutElement
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = h;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.GetComponent<Text>();
            txt.font = _font; txt.fontSize = 14; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter; txt.text = label;
            return btn;
        }
    }

    // ===== Case Editor Panel =====

    public class CaseEditorPanel
    {
        private PlanEditorUI _owner;
        private Font _font;
        private GameObject _panel;
        private InputField _labelInput;
        private InputField _descInput;
        private InputField _repeatInput;
        private InputField _blueInput;
        private InputField _redInput;
        private Text _blueCostText;
        private Text _redCostText;
        private Text _errorText;
        private TestPlan _plan;
        private int _caseIndex;
        private MonsterDatabase _db;
        private System.Action _onClose;

        public CaseEditorPanel(PlanEditorUI owner, Font font)
        {
            _owner = owner;
            _font = font;
        }

        public void Show(TestPlan plan, int caseIndex, MonsterDatabase db, System.Action onClose)
        {
            _plan = plan;
            _caseIndex = caseIndex;
            _db = db;
            _onClose = onClose;

            if (_panel == null) CreatePanel();
            _panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _onClose?.Invoke();
        }

        void CreatePanel()
        {
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            _panel = new GameObject("CaseEditorPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var prt = _panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(500, 420);
            _panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

            // Title
            var titleGo = CreateText("Title", _panel.transform, "编辑用例", 20, Color.white);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1); titleRt.offsetMin = new Vector2(10, -35); titleRt.offsetMax = new Vector2(-10, -5);

            float y = -50f;

            // Label input
            _labelInput = CreateInputField("LabelInput", _panel.transform, "用例名称", 10, y);
            y -= 40;

            // Description input
            _descInput = CreateInputField("DescInput", _panel.transform, "描述（可选）", 10, y);
            y -= 40;

            // Repeat input
            _repeatInput = CreateInputField("RepeatInput", _panel.transform, "重复次数", 10, y, 80);
            y -= 40;

            // Blue team input (format: monsterId:count,monsterId:count)
            _blueInput = CreateInputField("BlueInput", _panel.transform, "蓝方 (格式: monsterId:数量, ...)", 10, y);
            y -= 30;
            _blueCostText = CreateText("BlueCost", _panel.transform, "", 13, new Color(0.5f, 0.7f, 1f)).GetComponent<Text>();
            SetRect(_blueCostText.gameObject, 10, y, -10, y + 18);
            y -= 40;

            // Red team input
            _redInput = CreateInputField("RedInput", _panel.transform, "红方 (格式: monsterId:数量, ...)", 10, y);
            y -= 30;
            _redCostText = CreateText("RedCost", _panel.transform, "", 13, new Color(1f, 0.5f, 0.4f)).GetComponent<Text>();
            SetRect(_redCostText.gameObject, 10, y, -10, y + 18);
            y -= 35;

            // Error text
            _errorText = CreateText("Error", _panel.transform, "", 13, new Color(1f, 0.4f, 0.3f)).GetComponent<Text>();
            SetRect(_errorText.gameObject, 10, y, -10, y + 30);
            y -= 35;

            // Buttons
            var saveBtn = CreateBtn("SaveBtn", "保存", -100, y, _panel.transform);
            saveBtn.onClick.AddListener(OnSave);
            var cancelBtn = CreateBtn("CancelBtn", "取消", 100, y, _panel.transform);
            cancelBtn.onClick.AddListener(Hide);
        }

        void Refresh()
        {
            if (_caseIndex < 0 || _caseIndex >= _plan.Cases.Count) { Hide(); return; }
            var tc = _plan.Cases[_caseIndex];
            _labelInput.text = tc.Label;
            _descInput.text = tc.Description;
            _repeatInput.text = tc.RepeatCount.ToString();
            _blueInput.text = TeamToString(tc.TeamBlue);
            _redInput.text = TeamToString(tc.TeamRed);
            _blueCostText.text = $"蓝方总花费: {tc.BlueCost(_db)}G";
            _redCostText.text = $"红方总花费: {tc.RedCost(_db)}G";
            _errorText.text = "";
        }

        void OnSave()
        {
            var tc = _plan.Cases[_caseIndex];
            tc.Label = string.IsNullOrEmpty(_labelInput.text) ? "未命名" : _labelInput.text;
            tc.Description = _descInput.text;
            if (int.TryParse(_repeatInput.text, out int rep) && rep >= 1)
                tc.RepeatCount = rep;
            else
            {
                _errorText.text = "❌ 重复次数必须是 ≥1 的整数";
                return;
            }

            // Parse team inputs
            var blue = ParseTeam(_blueInput.text, _db, _errorText);
            if (blue == null) return;
            var red = ParseTeam(_redInput.text, _db, _errorText);
            if (red == null) return;

            if (blue.Count == 0 && red.Count == 0)
            {
                _errorText.text = "❌ 双方阵容不能都为空";
                return;
            }

            tc.TeamBlue = blue;
            tc.TeamRed = red;
            _plan.IsModified = true;
            Hide();
        }

        string TeamToString(List<TestPlanMonster> team)
        {
            return string.Join(",", team.Select(m => $"{m.MonsterId}:{m.Count}"));
        }

        List<TestPlanMonster> ParseTeam(string text, MonsterDatabase db, Text errorText)
        {
            var result = new List<TestPlanMonster>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            var parts = text.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Trim().Split(':');
                if (kv.Length < 1) continue;
                string id = kv[0].Trim();
                int count = 1;
                if (kv.Length >= 2 && !int.TryParse(kv[1].Trim(), out count)) count = 1;
                if (count < 1) count = 1;
                var def = db.GetById(id);
                if (def == null)
                {
                    errorText.text = $"❌ 单位 '{id}' 不存在";
                    return null;
                }
                result.Add(new TestPlanMonster(id, count));
            }
            return result;
        }

        // ===== UI helpers =====

        InputField CreateInputField(string name, Transform parent, string placeholder, float x, float y, float w = 480)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            SetRect(go, x, y, x + w, y + 30);
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            var input = go.GetComponent<InputField>();

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(go.transform, false);
            SetRectFill(phGo);
            var ph = phGo.GetComponent<Text>();
            ph.font = _font; ph.fontSize = 13; ph.color = new Color(0.5f, 0.5f, 0.5f);
            ph.text = placeholder; ph.alignment = TextAnchor.MiddleLeft;
            input.placeholder = ph;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            SetRectFill(txtGo);
            var txt = txtGo.GetComponent<Text>();
            txt.font = _font; txt.fontSize = 14; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft; txt.supportRichText = false;
            input.textComponent = txt;
            return input;
        }

        GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.color = color; txt.text = text;
            return go;
        }

        Button CreateBtn(string name, string label, float x, float y, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetRect(go, x, y, x + 110, y + 32);
            go.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            var btn = go.GetComponent<Button>();
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            SetRectFill(txtGo);
            var txt = txtGo.GetComponent<Text>();
            txt.font = _font; txt.fontSize = 14; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter; txt.text = label;
            return btn;
        }

        static void SetRect(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(xMin, yMin); rt.offsetMax = new Vector2(xMax, yMax);
        }

        static void SetRectFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6, 2); rt.offsetMax = new Vector2(-6, -2);
        }
    }
}
