/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ColorMak3r.Utility
{
    public static class MiscUtility
    {
        /// <summary>
        /// Snaps the given <paramref name="position"/> to the nearest grid cell using an integer grid size. 
        /// <para/>
        /// - If <paramref name="size"/> is even, a half-cell offset is applied (+/- 0.5) in both the X and Y directions.  
        /// - If <paramref name="size"/> is odd and <paramref name="rigid"/> is true, the position snaps to multiples of <paramref name="size"/>.  
        /// - Otherwise, it simply rounds each coordinate to the nearest integer.
        /// </summary>
        /// <param name="position">The position in world space to snap.</param>
        /// <param name="size">The integer grid size (defaults to 1).</param>
        /// <param name="rigid">
        /// If <c>true</c>, the snapping is performed to multiples of <paramref name="size"/> (rigid snapping). 
        /// If <c>false</c>, it simply rounds to the nearest integer.
        /// </param>
        /// <returns>
        /// The <see cref="Vector2"/> representing the snapped position.
        /// </returns>
        public static Vector2 SnapToGrid(this Vector2 position, int size = 1, bool rigid = false)
        {
            // Special handling when grid size is 2
            if (size % 2 == 0)
            {
                // Round the x and y positions to the nearest integer
                var snapX = Mathf.RoundToInt(position.x);
                var snapY = Mathf.RoundToInt(position.y);

                // Determine the offset for x based on whether the original position is less than the snapped value
                var offsetX = position.x < snapX ? -0.5f : 0.5f;

                // Determine the offset for y based on whether the original position is less than the snapped value
                var offsetY = position.y < snapY ? -0.5f : 0.5f;

                // Return the new snapped position with the calculated offsets
                return new Vector2(snapX + offsetX, snapY + offsetY);
            }
            else
            {
                if (rigid)
                {
                    // If rigid snapping is enabled, snap to the nearest multiple of the grid size
                    return new Vector2(
                        size * Mathf.RoundToInt(position.x / size),
                        size * Mathf.RoundToInt(position.y / size));
                }
                else
                {
                    // If rigid snapping is not enabled, simply round the position to the nearest integer
                    return new Vector2(
                        Mathf.RoundToInt(position.x),
                        Mathf.RoundToInt(position.y));
                }
            }
        }

        public static Vector2 SnapToGrid(this Vector3 position, int size = 1, bool rigid = false)
        {
            return SnapToGrid((Vector2)position, size, rigid);
        }

        /// <summary>
        /// Snaps the given <paramref name="position"/> to a grid defined by a <see cref="Vector2"/> <paramref name="gridSize"/>, 
        /// allowing different horizontal and vertical grid sizes. 
        /// <para/>
        /// - Each axis (X/Y) checks whether its grid size is even. If it is, a half-cell offset is applied (+/- 0.5) on that axis.  
        /// - If the grid size on a particular axis is odd and <paramref name="rigid"/> is true, then that axis snaps to multiples of its grid size.  
        /// - Otherwise, it simply rounds that axis to the nearest integer.
        /// </summary>
        /// <param name="position">The position in world space to snap.</param>
        /// <param name="gridSize">The grid size in each axis as a <see cref="Vector2"/>.</param>
        /// <param name="rigid">
        /// If <c>true</c>, each axis snaps to multiples of its respective size from <paramref name="gridSize"/>. 
        /// If <c>false</c>, it simply rounds on each axis.
        /// </param>
        /// <returns>
        /// The <see cref="Vector2"/> representing the snapped position.
        /// </returns>
        public static Vector3 SnapToGrid(this Vector2 position, Vector2 gridSize, bool rigid = false)
        {
            // Convert the floating-point gridSize to integer values 
            // to determine even/odd behavior.
            int sx = Mathf.RoundToInt(gridSize.x);
            int sy = Mathf.RoundToInt(gridSize.y);

            // We'll snap each axis independently according to the same logic:
            // 1) If size is even, apply half-cell offset.
            // 2) Otherwise if rigid, snap to multiples of that size.
            // 3) Otherwise just round to the nearest integer.

            // -- Snap the X axis --
            float x;
            if (sx % 2 == 0)
            {
                int snapX = Mathf.RoundToInt(position.x);
                float offsetX = position.x < snapX ? -0.5f : 0.5f;
                x = snapX + offsetX;
            }
            else
            {
                if (rigid)
                {
                    // Snap to multiples of sx
                    x = sx * Mathf.RoundToInt(position.x / sx);
                }
                else
                {
                    // Round to int
                    x = Mathf.RoundToInt(position.x);
                }
            }

            // -- Snap the Y axis --
            float y;
            if (sy % 2 == 0)
            {
                int snapY = Mathf.RoundToInt(position.y);
                float offsetY = position.y < snapY ? -0.5f : 0.5f;
                y = snapY + offsetY;
            }
            else
            {
                if (rigid)
                {
                    y = sy * Mathf.RoundToInt(position.y / sy);
                }
                else
                {
                    y = Mathf.RoundToInt(position.y);
                }
            }

            return new Vector2(x, y);
        }

        public static Vector2Int ToInt(this Vector2 vector)
        {
            return new Vector2Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
        }

        public static float Snap(this float value, float size = 0.1f)
        {
            return size * Mathf.RoundToInt(value / size);
        }

        public static Vector3 Add(this Vector3 vector, float x = 0, float y = 0, float z = 0)
        {
            return new Vector3(vector.x + x, vector.y + y, vector.z + z);
        }

        public static Vector2 RandomPointInRange(float range)
        {
            return new Vector2(Random.Range(-range, range), Random.Range(-range, range));
        }

        public static Vector2 RandomPointInRange(float minRadius, float maxRadius, float minAngle = 0f, float maxAngle = Mathf.PI * 2)
        {
            // Ensure valid radius
            if (minRadius > maxRadius)
            {
                float temp = minRadius;
                minRadius = maxRadius;
                maxRadius = temp;
            }

            float angle = Random.Range(minAngle, maxAngle);
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius)); // uniform distribution

            return new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            List<T> shuffledList = new List<T>(list);
            int n = shuffledList.Count;

            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = shuffledList[k];
                shuffledList[k] = shuffledList[n];
                shuffledList[n] = value;
            }

            return shuffledList;
        }

        public static float SumUpTo(this float[] array, int element)
        {
            float total = 0f;
            int i = 0;
            while (i < element)
            {
                total += array[i];
                i++;
            }
            return total;
        }

        public static T GetRandomElement<T>(this IEnumerable<T> collection)
        {
            if (collection == null || !collection.Any())
                return default;

            int count = collection.Count();
            return collection.ElementAt(Random.Range(0, count));
        }

        public static T GetRandomElementNot<T>(this IEnumerable<T> collection, T notElement)
        {
            if (collection == null || !collection.Any())
                return default;

            T selected = default;
            int count = 0;

            var comparer = EqualityComparer<T>.Default;

            foreach (var item in collection)
            {
                if (comparer.Equals(item, notElement))
                    continue;

                count++;
                if (Random.Range(0, count) == 0)
                    selected = item;
            }

            return count == 0 ? default : selected;
        }

        public static double RoundToNextPowerOf10(this double x)
        {
            if (x < 0)
            {
                Debug.Log("Input must be a non-negative double.");
                return x;
            }

            double exponent = Math.Ceiling(Math.Log10(x));
            return Math.Pow(10, exponent);
        }

        public static float RoundToNextPowerOf10(this float x)
        {
            if (x < 0)
            {
                Debug.Log("Input must be a non-negative float.");
                return x;
            }


            float exponent = Mathf.Ceil(Mathf.Log10(x));
            return Mathf.Pow(10f, exponent);
        }


        public static int RoundToNextPowerOf10(this int x)
        {
            if (x < 0)
            {
                Debug.Log("Input must be a non-negative integer.");
                return x;
            }

            if (x == 0)
                return 1;

            int exponent = (int)Math.Ceiling(Math.Log10(x));
            return (int)Math.Pow(10, exponent);
        }


        #region Color Utility
        public static Color SetAlpha(this Color rgbColor, float alpha)
        {
            return new Color(rgbColor.r, rgbColor.g, rgbColor.b, alpha);
        }

        public static Color RandomColor(this Color color)
        {
            return new Color(Random.value, Random.value, Random.value);
        }
        #endregion

        public static string FormatTimeHHMMSS(double timeLeft)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeLeft);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        public static IEnumerator WaitCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
    }
}
