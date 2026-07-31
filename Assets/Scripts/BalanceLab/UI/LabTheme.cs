using UnityEngine;
using UnityEngine.UI;

namespace MCFight.BalanceLab
{
    /// <summary> Lab UI 主题助手：统一加载字体/按钮精灵/面板精灵 </summary>
    public static class LabTheme
    {
        static Font _font;
        static UITheme _theme;
        static Sprite _labBg;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<Font>("Sprites/UI/Kenney/Font/MaokenAssortedSans.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        public static UITheme Theme
        {
            get
            {
                if (_theme == null)
                    _theme = UITheme.Instance;
                return _theme;
            }
        }

        public static Sprite LabBackground
        {
            get
            {
                if (_labBg == null)
                    _labBg = Resources.Load<Sprite>("Sprites/UI/lab_background");
                return _labBg;
            }
        }

        public static Sprite ButtonSprite(UIButtonStyled.Style style)
        {
            var t = Theme;
            if (t == null) return null;
            return style switch
            {
                UIButtonStyled.Style.Primary => t.BtnBlueNormal,
                UIButtonStyled.Style.Danger => t.BtnRedNormal,
                UIButtonStyled.Style.Success => t.BtnGreenNormal,
                UIButtonStyled.Style.Warning => t.BtnYellowNormal,
                _ => t.BtnGreyNormal,
            };
        }

        /// <summary> 创建主题化按钮 </summary>
        public static Button CreateButton(string name, string label, float xMin, float yMin, float xMax, float yMax,
            Transform parent, UIButtonStyled.Style style = UIButtonStyled.Style.Secondary, int fontSize = 14)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(5f, 2f);
            rt.offsetMax = new Vector2(-5f, -2f);

            var img = go.GetComponent<Image>();
            var sprite = ButtonSprite(style);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
                img.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            var btn = go.GetComponent<Button>();
            var btnStyled = go.AddComponent<UIButtonStyled>();
            btnStyled.style = style;

            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var txt = tgo.GetComponent<Text>();
            txt.font = Font; txt.fontSize = fontSize; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter; txt.text = label;

            return btn;
        }

        /// <summary> 创建主题化面板 </summary>
        public static GameObject CreatePanel(string name, float xMin, float yMin, float xMax, float yMax, Transform parent, float alpha = 0.92f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            var panelSprite = Theme?.PanelSprite;
            if (panelSprite != null)
            {
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.12f, 0.12f, 0.16f, alpha);
            }
            else
                img.color = new Color(0.08f, 0.08f, 0.12f, alpha);

            return go;
        }

        /// <summary> 创建主题化文本 </summary>
        public static Text CreateText(string name, string text, float xMin, float yMin, float xMax, float yMax,
            Transform parent, int fontSize = 14, Color color = default, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(5f, 2f);
            rt.offsetMax = new Vector2(-5f, -2f);
            var txt = go.GetComponent<Text>();
            txt.font = Font;
            txt.fontSize = fontSize;
            txt.color = color == default ? Color.white : color;
            txt.alignment = alignment;
            txt.supportRichText = true;
            txt.text = text;
            return txt;
        }

        /// <summary> 创建全屏实验室背景 </summary>
        public static GameObject CreateLabBackground(Transform parent)
        {
            var go = new GameObject("LabBackground", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (LabBackground != null)
            {
                img.sprite = LabBackground;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            else
                img.color = new Color(0.05f, 0.05f, 0.08f, 1f);
            return go;
        }
    }
}
