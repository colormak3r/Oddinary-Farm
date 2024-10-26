using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Coin")]
public class CoinProperty : SpawnerProperty
{
    [Header("Coin Settings")]
    [SerializeField]
    private uint value;

    public uint Value => value;
}
