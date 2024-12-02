using System.Collections;
using UnityEngine;

namespace ColorMak3r.Utility
{
    public static class UIUtility
    {
        public static IEnumerator UIMoveCoroutine(this RectTransform rect, Vector2 start, Vector2 end, float duration = 0.5f)
        {
            float t = 0f;
            while (t < 1)
            {
                rect.anchoredPosition = Vector2.Lerp(start, end, t);
                t += Time.deltaTime / duration;
                yield return null;
            }

            rect.anchoredPosition = end;
        }

        public static IEnumerator UIFadeCoroutine(this Transform transform, float start, float end, float duration = 0.5f)
        {
            CanvasRenderer[] renderers = transform.GetComponentsInChildren<CanvasRenderer>();

            yield return UIFadeCoroutine(renderers, start, end, duration);
        }

        public static IEnumerator UIFadeCoroutine(this GameObject gameObject, float start, float end, float duration = 0.5f)
        {
            CanvasRenderer[] renderers = gameObject.GetComponentsInChildren<CanvasRenderer>();

            yield return UIFadeCoroutine(renderers, start, end, duration);
        }

        public static IEnumerator UIFadeCoroutine(this CanvasRenderer[] renderers, float start, float end, float duration = 0.5f)
        {
            //if (renderers == null || renderers.Length == 0) yield break;            

            foreach (CanvasRenderer renderer in renderers)
            {
                if (renderer != null) renderer.SetAlpha(start);
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float alpha = Mathf.Lerp(start, end, elapsedTime / duration);

                foreach (CanvasRenderer renderer in renderers)
                {
                    if (renderer != null) renderer.SetAlpha(alpha);
                }

                yield return null;
            }

            foreach (CanvasRenderer renderer in renderers)
            {
                if (renderer != null) renderer.SetAlpha(end);
            }
        }

        public static IEnumerator UIPopCoroutine(this CanvasRenderer renderer, Vector3 originalSize, Vector3 targetSize, float duration = 0.25f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                renderer.transform.localScale = Vector3.Lerp(originalSize, targetSize, elapsedTime / duration);

                yield return null;
            }

            elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                renderer.transform.localScale = Vector3.Lerp(targetSize, originalSize, elapsedTime / duration);

                yield return null;
            }

            renderer.transform.localScale = originalSize;
        }
    }
}
