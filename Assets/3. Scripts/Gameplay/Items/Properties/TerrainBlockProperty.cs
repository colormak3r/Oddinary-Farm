using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Terrain Block Property", menuName = "Scriptable Objects/Item/Terrain Block")]
public class TerrainBlockProperty : ItemProperty
{
    [Header("Terrain Block Settings")]
    [SerializeField]
    private LayerMask placeableLayer;
    [SerializeField]
    private TerrainUnitProperty unitProperty;

    public LayerMask PlaceableLayer => placeableLayer;
    public TerrainUnitProperty UnitProperty => unitProperty;
}
