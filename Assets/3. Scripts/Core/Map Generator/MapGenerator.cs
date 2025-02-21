using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class MapGenerator : NetworkBehaviour
{
    protected Offset2DArray<float> rawMap;

    public Offset2DArray<float> RawMap { get => rawMap; }

    protected abstract IEnumerator GenerateMap(Vector2Int mapSize);
}
