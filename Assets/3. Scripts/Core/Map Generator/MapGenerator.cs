using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class MapGenerator : NetworkBehaviour
{
    protected float[,] map;

    public float[,] Map { get => map; }

    public abstract void GenerateMap(Vector2Int mapSize);
}
