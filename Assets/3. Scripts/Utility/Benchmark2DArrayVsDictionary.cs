using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using Debug = UnityEngine.Debug;

public class Benchmark2DArrayVsDictionary : MonoBehaviour
{
    // Set the dimensions of the dataset.
    private const int width = 200;
    private const int height = 200;

    // A simple custom type for testing.
    struct CustomType
    {
        public int value;
    }

    [ContextMenu("Start")]
    void Start()
    {
        BenchmarkAdd();
        BenchmarkDelete();
        BenchmarkRandomAccess();
    }

    // Benchmark the "adding" operation.
    void BenchmarkAdd()
    {
        Stopwatch stopwatch = new Stopwatch();

        // 2D Array "adding": simply assigning a value to each cell.
        stopwatch.Start();
        CustomType[,] array = new CustomType[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                array[x, y] = new CustomType { value = x * y };
            }
        }
        stopwatch.Stop();
        Debug.Log("2D Array assignment time: " + stopwatch.ElapsedMilliseconds + " ms");

        // Dictionary "adding": inserting key-value pairs.
        stopwatch.Reset();
        stopwatch.Start();
        Dictionary<Vector2Int, CustomType> dict = new Dictionary<Vector2Int, CustomType>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dict.Add(new Vector2Int(x, y), new CustomType { value = x * y });
            }
        }
        stopwatch.Stop();
        Debug.Log("Dictionary add time: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    // Benchmark the "delete" operation.
    void BenchmarkDelete()
    {
        Stopwatch stopwatch = new Stopwatch();

        // Prepopulate the data structures.
        CustomType[,] array = new CustomType[width, height];
        Dictionary<Vector2Int, CustomType> dict = new Dictionary<Vector2Int, CustomType>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CustomType ct = new CustomType { value = x * y };
                array[x, y] = ct;
                dict.Add(new Vector2Int(x, y), ct);
            }
        }

        // "Delete" in 2D array: simulate by setting each cell to default.
        stopwatch.Start();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                array[x, y] = default(CustomType);
            }
        }
        stopwatch.Stop();
        Debug.Log("2D Array 'delete' (reset to default) time: " + stopwatch.ElapsedMilliseconds + " ms");

        // Delete from Dictionary: remove each key.
        stopwatch.Reset();
        stopwatch.Start();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dict.Remove(new Vector2Int(x, y));
            }
        }
        stopwatch.Stop();
        Debug.Log("Dictionary remove time: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    // Benchmark random access: lookup elements using random indices.
    void BenchmarkRandomAccess()
    {
        // Prepopulate the data structures.
        CustomType[,] array = new CustomType[width, height];
        Dictionary<Vector2Int, CustomType> dict = new Dictionary<Vector2Int, CustomType>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CustomType ct = new CustomType { value = x * y };
                array[x, y] = ct;
                dict.Add(new Vector2Int(x, y), ct);
            }
        }

        // Generate a list of random indices.
        int totalAccesses = width * height; // Total random lookups.
        Vector2Int[] randomIndices = new Vector2Int[totalAccesses];
        System.Random rnd = new System.Random();
        for (int i = 0; i < totalAccesses; i++)
        {
            int x = rnd.Next(0, width);
            int y = rnd.Next(0, height);
            randomIndices[i] = new Vector2Int(x, y);
        }

        Stopwatch stopwatch = new Stopwatch();
        long sum = 0;

        // Random access for 2D Array.
        stopwatch.Start();
        for (int i = 0; i < totalAccesses; i++)
        {
            Vector2Int idx = randomIndices[i];
            sum += array[idx.x, idx.y].value;
        }
        stopwatch.Stop();
        Debug.Log("2D Array random access time: " + stopwatch.ElapsedMilliseconds + " ms, sum: " + sum);

        // Random access for Dictionary.
        sum = 0;
        stopwatch.Reset();
        stopwatch.Start();
        for (int i = 0; i < totalAccesses; i++)
        {
            Vector2Int idx = randomIndices[i];
            sum += dict[idx].value;
        }
        stopwatch.Stop();
        Debug.Log("Dictionary random access time: " + stopwatch.ElapsedMilliseconds + " ms, sum: " + sum);
    }
}
