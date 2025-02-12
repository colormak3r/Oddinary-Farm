using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class MapGenerator : NetworkBehaviour
{
    protected float[,] map;

    public float[,] Map { get => map; }

    protected abstract IEnumerator GenerateMap(Vector2Int mapSize);
}
