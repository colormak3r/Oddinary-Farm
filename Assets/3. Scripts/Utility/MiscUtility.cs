using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ColorMak3r.Utility
{
    public static class Helper
    {
        /// <summary>
        /// Snaps the given position to a grid based on the specified size.
        /// </summary>
        /// <param name="position">The original position to be snapped.</param>
        /// <param name="size">The grid size to snap to. Defaults to 1.</param>
        /// <param name="rigid">
        /// If true, the position will be snapped rigidly to the nearest multiple of the grid size.
        /// If false, the position will be snapped to the nearest integer values or adjusted based on specific rules for size 2.
        /// Defaults to false.
        /// </param>
        /// <returns>A new Vector2 representing the snapped position.</returns>
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

        public static Vector2 RandomPointInRange(this Vector2 origin, float range)
        {
            var randomPoint = new Vector2(
                origin.x + Random.Range(-range, range),
                origin.y + Random.Range(-range, range)
            );

            return randomPoint;
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
                return default(T);

            int count = collection.Count();
            return collection.ElementAt(Random.Range(0, count));
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
    }
}
