using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    [RequireComponent(typeof(LabSessionController))]
    public class RequirementChatUI : MonoBehaviour
    {
        private LabSessionController _controller;
        private GameObject _panel;
        private Text _chatLog;
        private InputField _input;
        private Button _sendBtn;
        private Button _confirmBtn;
        private Button _cancelBtn;
        private ScrollRect _scroll;
        private BalanceTestPlanFile _pendingPlan;
        private bool _uiCreated = false;
        private bool _waitingLLM = false;
        private LLMClient _llm;
        private const string API_KEY = "sk-230dec004557436fbced6a2c5760f595";
        private const string API_URL = "https://api.deepseek.com";

        void Start()
        {
            _controller = GetComponent<LabSessionController>();
            _llm = new LLMClient(API_KEY, API_URL);
        }

        public void Show()
        {
            if (!_uiCreated) CreateUI();
            _panel.SetActive(true);
            _input.Select(); _input.ActivateInputField();
        }

        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        void CreateUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogError("[ChatUI] No Canvas"); return; }

            _panel = LabTheme.CreatePanel("ChatPanel", 0f, 0f, 0.35f, 1f, canvas.transform, 0.95f);

            // Chat log scroll
            var scrollGo = new GameObject("ChatScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_panel.transform, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.15f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(5f, 5f); srt.offsetMax = new Vector2(-5f, -5f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            var sbgSprite = LabTheme.Theme?.PanelSprite;
            if (sbgSprite != null) { scrollBg.sprite = sbgSprite; scrollBg.type = Image.Type.Sliced; }
            _scroll = scrollGo.GetComponent<ScrollRect>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f); crt.sizeDelta = Vector2.zero;
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = crt;

            _chatLog = LabTheme.CreateText("LogText", "<color=#5AAAFF>🤖 平衡实验室 (DeepSeek)</color>\n输入测试需求:\n• 测试所有20G近战1v1跑3次\n• warden vs blaze 跑10次\n\n",
                0f, 0f, 1f, 0f, contentGo.transform, 14, Color.white, TextAnchor.UpperLeft);

            // Input field
            var inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(_panel.transform, false);
            var irt = inputGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.02f); irt.anchorMax = new Vector2(0.7f, 0.10f);
            irt.offsetMin = new Vector2(5f, 5f); irt.offsetMax = new Vector2(-2f, 0f);
            var inputImg = inputGo.GetComponent<Image>();
            var inputSprite = LabTheme.Theme?.PanelSprite;
            if (inputSprite != null) { inputImg.sprite = inputSprite; inputImg.type = Image.Type.Sliced; inputImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f); }
            else inputImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            _input = inputGo.GetComponent<InputField>();

            var itxt = LabTheme.CreateText("InputText", "", 0f, 0f, 1f, 1f, inputGo.transform, 14, Color.white, TextAnchor.MiddleLeft);
            itxt.supportRichText = false;
            _input.textComponent = itxt;
            _input.placeholder = itxt;

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(inputGo.transform, false);
            var phrt = phGo.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one; phrt.offsetMin = new Vector2(8, 2); phrt.offsetMax = new Vector2(-8, -2);
            var phtxt = phGo.GetComponent<Text>();
            phtxt.font = LabTheme.Font; phtxt.fontSize = 14; phtxt.color = new Color(0.5f, 0.5f, 0.5f);
            phtxt.text = "输入测试需求... (回车发送)";
            _input.placeholder = phtxt;

            // Send button
            _sendBtn = LabTheme.CreateButton("SendBtn", "发送", 0.7f, 0.02f, 1f, 0.10f, _panel.transform, UIButtonStyled.Style.Primary);
            _sendBtn.onClick.AddListener(OnSend);
            _input.onSubmit.AddListener(_ => OnSend());

            // Confirm/Cancel
            _confirmBtn = LabTheme.CreateButton("ConfirmBtn", "确认执行", 0f, 0.12f, 0.5f, 0.18f, _panel.transform, UIButtonStyled.Style.Success);
            _confirmBtn.gameObject.SetActive(false);
            _confirmBtn.onClick.AddListener(OnConfirm);

            _cancelBtn = LabTheme.CreateButton("CancelBtn", "取消", 0.5f, 0.12f, 1f, 0.18f, _panel.transform, UIButtonStyled.Style.Secondary);
            _cancelBtn.gameObject.SetActive(false);
            _cancelBtn.onClick.AddListener(() => { _confirmBtn.gameObject.SetActive(false); _cancelBtn.gameObject.SetActive(false); });

            _uiCreated = true;
        }

        void OnSend()
        {
            if (string.IsNullOrWhiteSpace(_input.text)) return;
            if (_waitingLLM) return;
            var userInput = _input.text.Trim();
            _input.text = "";
            AppendLog($"<color=#FFD700>👤 {userInput}</color>");

            var db = GameManager.Instance.Database;
            var index = KnowledgePersistence.LoadIndex();
            var references = SessionReferenceDetector.Detect(userInput, index);
            string refContext = null;
            if (references.Count > 0)
            {
                foreach (var r in references)
                    AppendLog($"<color=#FF8844>📎 引用: {r.title} ({r.completed_matches}场)</color>");
                refContext = SessionReferenceDetector.BuildReferenceContext(references);
            }

            var knowledgeSummary = KnowledgeBase.GetStrategySummary();
            if (knowledgeSummary != "尚无历史测试数据")
                AppendLog($"<color=#5599FF>📊 知识库: {knowledgeSummary}</color>");

            var intent = IntentParser.Parse(userInput, db);
            AppendLog($"<color=#88FF88>🤖 解析: {intent.Summary}</color>");
            var snap = MonsterCatalog.BuildSnapshot(db);
            var filteredUnits = MonsterCatalog.FilterByIntent(snap, intent);
            if (filteredUnits.Count == 0)
                filteredUnits = snap.Units.FindAll(u => u.Price > 0);
            if (filteredUnits.Count > 30)
            {
                AppendLog($"<color=#888888>🤖 单位过多({filteredUnits.Count})，截取前30个</color>");
                filteredUnits = filteredUnits.GetRange(0, 30);
            }

            AppendLog("<color=#5AAAFF>🤖 正在请求 DeepSeek 生成计划...</color>");
            _waitingLLM = true;
            string userPrompt = PromptTemplates.BuildUserPrompt(userInput, filteredUnits, knowledgeSummary, refContext);
            StartCoroutine(_llm.SendChat(PromptTemplates.SystemPrompt, userPrompt,
                (response) => OnLLMSuccess(response, db),
                (error) => OnLLMError(error, intent, snap, db)));
        }

        void OnLLMSuccess(string llmResponse, MonsterDatabase db)
        {
            _waitingLLM = false;
            var plan = TestPlanIO.LoadFromJson(llmResponse);
            List<string> llmErrors = null;
            bool valid = plan != null && TestPlanIO.Validate(plan, db, out llmErrors, out _);
            if (valid)
            {
                _pendingPlan = plan;
                int total = TestPlanIO.CountTotalMatches(plan);
                AppendLog($"<color=#88FF88>🤖 LLM 计划已生成: {plan.tests.Count} 个测试, 共 {total} 场</color>");
                if (plan.metadata != null && !string.IsNullOrEmpty(plan.metadata.title))
                    AppendLog($"<color=#5AAAFF>📋 {plan.metadata.title}</color>");
                _confirmBtn.gameObject.SetActive(true);
                _cancelBtn.gameObject.SetActive(true);
            }
            else
            {
                AppendLog("<color=#FF8844>🤖 LLM JSON 解析/校验失败，回退到本地规划器</color>");
                FallbackToLocal(db);
            }
            ScrollToBottom();
        }

        void OnLLMError(string error, IntentPreview intent, MonsterCatalogSnapshot snap, MonsterDatabase db)
        {
            _waitingLLM = false;
            AppendLog($"<color=#FF4444>🤖 LLM 请求失败: {error}</color>");
            AppendLog("<color=#FF8844>  回退到本地规划器...</color>");
            FallbackToLocal(intent, snap, db);
        }

        void FallbackToLocal(MonsterDatabase db)
        {
            var intent = IntentParser.Parse("", db);
            FallbackToLocal(intent, MonsterCatalog.BuildSnapshot(db), db);
        }

        void FallbackToLocal(IntentPreview intent, MonsterCatalogSnapshot snap, MonsterDatabase db)
        {
            var planResult = LocalPlanner.GeneratePlan(intent, snap);
            _pendingPlan = planResult.File;
            if (TestPlanIO.Validate(_pendingPlan, db, out var fallbackErrors, out _))
            {
                AppendLog($"<color=#88FF88>🤖 本地计划已生成:\n{planResult.Summary}</color>");
                _confirmBtn.gameObject.SetActive(true);
                _cancelBtn.gameObject.SetActive(true);
            }
            else
                AppendLog($"<color=#FF4444>🤖 本地规划也失败: {string.Join("; ", fallbackErrors)}</color>");
            ScrollToBottom();
        }

        void OnConfirm()
        {
            if (_pendingPlan == null) return;
            _confirmBtn.gameObject.SetActive(false);
            _cancelBtn.gameObject.SetActive(false);
            Hide();
            var testCases = TestPlanIO.ToLabTestCases(_pendingPlan);
            AppendLog($"<color=#88FF88>🤖 开始执行 {testCases.Count} 个测试...</color>");
            _controller.StartSession(testCases, _pendingPlan?.metadata?.title);
        }

        void AppendLog(string msg)
        {
            _chatLog.text += msg + "\n";
            if (_uiCreated) ScrollToBottom();
        }

        void ScrollToBottom() { Canvas.ForceUpdateCanvases(); _scroll.verticalNormalizedPosition = 0f; }
    }
}
