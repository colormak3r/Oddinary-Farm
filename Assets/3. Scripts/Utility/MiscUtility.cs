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
        public static Vector2 SnapToGrid(this Vector2 position, int size = 1)
        {
            return new Vector2(
                size * Mathf.RoundToInt(position.x / size),
                size * Mathf.RoundToInt(position.y / size));
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
    }
}
