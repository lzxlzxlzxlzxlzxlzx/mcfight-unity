using UnityEngine;
using System.Collections;

namespace MCFight
{
    /// <summary>
    /// 面板动画工具：Show/Hide 时的缩放+淡入淡出。
    /// 不依赖 DOTween，用协程 Lerp 实现。
    /// </summary>
    public static class UIAnimator
    {
        /// <summary> 面板进场：Scale 0.92→1.0 + Alpha 0→1 </summary>
        public static IEnumerator PanelIn(Transform t, CanvasGroup cg, float duration = 0.22f)
        {
            if (cg == null) cg = t.GetComponent<CanvasGroup>() ?? t.gameObject.AddComponent<CanvasGroup>();
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.92f;
            Vector3 endScale = Vector3.one;
            cg.alpha = 0f;
            t.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cg.alpha = k;
                t.localScale = Vector3.Lerp(startScale, endScale, k);
                yield return null;
            }
            cg.alpha = 1f;
            t.localScale = endScale;
        }

        /// <summary> 面板退场：Scale 1.0→0.96 + Alpha 1→0 </summary>
        public static IEnumerator PanelOut(Transform t, CanvasGroup cg, float duration = 0.15f)
        {
            if (cg == null) cg = t.GetComponent<CanvasGroup>() ?? t.gameObject.AddComponent<CanvasGroup>();
            float elapsed = 0f;
            Vector3 startScale = t.localScale;
            Vector3 endScale = Vector3.one * 0.96f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cg.alpha = 1f - k;
                t.localScale = Vector3.Lerp(startScale, endScale, k);
                yield return null;
            }
            cg.alpha = 0f;
            t.localScale = endScale;
        }

        /// <summary> 弹出动画：Scale 0→1 带 overshoot </summary>
        public static IEnumerator PopIn(Transform t, float duration = 0.2f, float overshoot = 0.15f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                float scale = k < 1f
                    ? Mathf.Lerp(0f, 1f + overshoot, k) * (1f - overshoot * (1f - k))
                    : 1f;
                t.localScale = Vector3.one * scale;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        /// <summary> 数字滚动动画 </summary>
        public static IEnumerator NumberRoll(UnityEngine.UI.Text target, int from, int to, float duration = 0.3f)
        {
            if (target == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                int val = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
                target.text = val.ToString();
                yield return null;
            }
            target.text = to.ToString();
        }

        /// <summary> 金币数字弹跳动画：Scale 1→1.25→1 </summary>
        public static IEnumerator GoldBump(Transform t, float duration = 0.3f)
        {
            float elapsed = 0f;
            Vector3 original = t.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                float scale = 1f + 0.25f * Mathf.Sin(k * Mathf.PI);
                t.localScale = original * scale;
                yield return null;
            }
            t.localScale = original;
        }

        /// <summary> 卡片弹出动画：Scale 0.3→1.0 带 overshoot </summary>
        public static IEnumerator CardPop(Transform t, float duration = 0.2f)
        {
            float elapsed = 0f;
            Vector3 original = Vector3.one;
            t.localScale = Vector3.one * 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                float scale;
                if (k < 0.7f)
                    scale = Mathf.Lerp(0.3f, 1.15f, k / 0.7f);
                else
                    scale = Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
                t.localScale = original * scale;
                yield return null;
            }
            t.localScale = original;
        }
    }
}
