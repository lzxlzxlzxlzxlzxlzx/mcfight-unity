using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MCFight
{
    /// <summary>
    /// 统一按钮样式：hover/press 反馈 + 点击音效。
    /// 挂在带 Image+Button 的 GameObject 上。
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class UIButtonStyled : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum Style { Primary, Secondary, Danger, Success, Warning }

        public Style style = Style.Primary;
        public bool playSound = true;

        private Image _image;
        private Button _button;
        private Vector3 _originalScale;
        private bool _hovering;

        void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!_button.interactable) return;
            _hovering = true;
            transform.localScale = _originalScale * 1.04f;
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovering = false;
            transform.localScale = _originalScale;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!_button.interactable) return;
            transform.localScale = _originalScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            transform.localScale = _hovering ? _originalScale * 1.04f : _originalScale;
        }

        /// <summary> 手动触发点击音效（在 Button.onClick 中调用） </summary>
        public void PlayClickSound()
        {
            if (!playSound) return;
            var player = UISoundPlayer.Instance;
            if (player != null)
            {
                switch (style)
                {
                    case Style.Secondary: player.PlayCancel(); break;
                    case Style.Success:
                    case Style.Primary: player.PlayConfirm(); break;
                    default: player.PlayClick(); break;
                }
            }
        }
    }
}
