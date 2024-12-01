using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMak3r.Utility
{
    public static class TransformUtility
    {
        public static Vector3 HALF_UNIT_Y_V3 = new Vector3(0, 0.5f, 0);
        public static Vector2 HALF_UNIT_Y_V2 = new Vector2(0, 0.5f);

        public static IEnumerator ShakeCoroutine(this Transform transform, Vector3 originalPos,
        float maxX = 0.1f, float maxY = 0.1f, float duration = 0.05f)
        {
            transform.position = transform.position + new Vector3(Random.value > 0.5f ? -maxX : maxX, Random.value > 0.5f ? -maxY : maxY);
            yield return new WaitForSeconds(duration);
            transform.position = originalPos;
        }

        public static IEnumerator SmoothMoveCoroutine(this Transform transform, Vector2 start, Vector2 end, float duration = 0.5f)
        {
            transform.position = start;
            Vector2 velocity = Vector2.zero;
            float remainingTime = duration;

            while (remainingTime > 0)
            {
                float deltaTime = Time.fixedDeltaTime;
                transform.position = Vector2.SmoothDamp(transform.position, end, ref velocity, remainingTime);
                remainingTime -= deltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = end;
        }

        /// <summary>
        /// Smoothly moves a Transform from a start position to an end position over a specified duration.
        /// </summary>
        /// <param name="transform">The Transform to move.</param>
        /// <param name="start">Starting position.</param>
        /// <param name="end">Ending position.</param>
        /// <param name="duration">Duration of the movement in seconds.</param>
        /// <returns>Coroutine IEnumerator.</returns>
        public static IEnumerator LerpMoveCoroutine(this Transform transform, Vector3 start, Vector3 end, float duration = 0.5f)
        {
            // Set the initial position
            transform.position = start;

            float elapsedTime = 0f;

            // Continue until the elapsed time exceeds the duration
            while (elapsedTime < duration)
            {
                // Calculate the interpolation factor
                float t = elapsedTime / duration;

                // Interpolate the position
                transform.position = Vector3.Lerp(start, end, t);

                // Increment the elapsed time
                elapsedTime += Time.deltaTime;

                // Wait for the next frame
                yield return null;
            }

            // Ensure the final position is set
            transform.position = end;
        }

        public static IEnumerator RotateCoroutine(this Transform transform, int rotation, float duration = 0.5f)
        {
            float timeElapsed = 0;
            float startRotation = transform.eulerAngles.z;
            float endRotation = startRotation + rotation;

            while (timeElapsed < duration)
            {
                // Calculating the new rotation for this frame
                float zRotation = Mathf.Lerp(startRotation, endRotation, timeElapsed / duration);
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, zRotation);

                // Increasing the elapsed time
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // Ensuring the object has reached the final rotation
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, endRotation);
        }


        #region Pop In/Out
        public static IEnumerator PopInCoroutine(this Transform transform, float duration = 0.5f)
        {
            yield return transform.PopCoroutine(Vector3.zero, Vector3.one, duration);
        }

        public static IEnumerator PopOutCoroutine(this Transform transform, float duration = 0.5f)
        {
            yield return transform.PopCoroutine(Vector3.one, Vector3.zero, duration);
        }

        public static IEnumerator PopCoroutine(this Transform transform, float start, float end, float duration = 0.5f)
        {
            var original = new Vector3(start, start, start);
            var target = new Vector3(end, end, end);
            yield return PopCoroutine(transform, original, target, duration);
        }

        public static IEnumerator PopCoroutine(this Transform transform, Vector3 original, Vector3 target, float duration = 0.5f)
        {
            float t = 0;
            while (t < 1)
            {
                transform.localScale = Vector3.Lerp(original, target, t);
                t += Time.deltaTime / duration;
                yield return null;
            }

            transform.localScale = target;
        }
        #endregion
    }
}
