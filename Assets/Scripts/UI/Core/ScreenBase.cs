using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MCFight
{
    /// <summary>
    /// UI 面板基类：统一 Show/Hide 逻辑 + CanvasGroup 动画。
    /// 各界面继承此类，保持 GameManager 接口不变。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ScreenBase : MonoBehaviour
    {
        protected CanvasGroup _cg;
        protected bool _animating = false;

        protected virtual void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (!_animating) StartCoroutine(ShowAnim());
        }

        public virtual void Hide()
        {
            if (!_animating) StartCoroutine(HideAnim());
            else gameObject.SetActive(false);
        }

        IEnumerator ShowAnim()
        {
            _animating = true;
            yield return UIAnimator.PanelIn(transform, _cg, 0.22f);
            _animating = false;
        }

        IEnumerator HideAnim()
        {
            _animating = true;
            yield return UIAnimator.PanelOut(transform, _cg, 0.15f);
            _animating = false;
            gameObject.SetActive(false);
        }
    }
}
