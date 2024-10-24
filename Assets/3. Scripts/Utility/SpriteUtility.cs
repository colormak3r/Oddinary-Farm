using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMak3r.Utility
{
    public static class SpriteUtility
    {
        public static IEnumerator SpriteFadeInCoroutine(this GameObject obj, float duration = 0.5f)
        {
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            yield return renderers.SpriteFadeCoroutine(0, 1, duration);
        }

        public static IEnumerator SpriteFadeOutCoroutine(this GameObject obj, float duration = 0.5f)
        {
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            yield return renderers.SpriteFadeCoroutine(1, 0, duration);
        }

        public static IEnumerator SpriteFadeCoroutine(this SpriteRenderer renderer, float start, float end, float duration = 0.5f)
        {
            var startColor = renderer.color.SetAlpha(start);
            var endColor = renderer.color.SetAlpha(end);
            float lerp = 0;
            while (lerp < 1)
            {
                renderer.color = Color.Lerp(startColor, endColor, lerp);

                lerp += Time.deltaTime / duration;
                yield return null;
            }

            renderer.color = endColor;
        }

        public static IEnumerator SpriteFadeCoroutine(this SpriteRenderer[] renderers, float start, float end, float duration = 0.5f)
        {
            var startColor = renderers[0].color.SetAlpha(start);
            var endColor = renderers[0].color.SetAlpha(end);

            float lerp = 0;
            while (lerp < 1)
            {
                foreach (var r in renderers)
                {
                    r.color = Color.Lerp(startColor, endColor, lerp);
                }

                lerp += Time.deltaTime / duration;
                yield return null;
            }

            foreach (var r in renderers)
            {
                r.color = endColor;
            }
        }
    }
}


