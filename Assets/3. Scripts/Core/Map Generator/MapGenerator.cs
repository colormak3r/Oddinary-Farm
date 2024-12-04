using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField]
    private Vector2 origin = new Vector2(1264, 234);
    [SerializeField]
    private Vector2Int elevationDimension;
    [SerializeField]
    private float elevationScale = 1.0f;
    [SerializeField]
    private int elevationOctaves = 3;
    [SerializeField]
    private float elevationPersistence = 0.5f;
    [SerializeField]
    private float elevationFrequencyBase = 2f;
    [SerializeField]
    private float elevationExp = 1f;
}
