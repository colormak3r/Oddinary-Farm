using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FolliageGenerator : PerlinNoiseGenerator
{
    [Header("Folliage Settings")]
    [SerializeField]
    private GameObject[] folliagePrefab;


    public IEnumerator GenerateFolliageCoroutine(Vector2 position)
    {
        yield return null;
    }
}
