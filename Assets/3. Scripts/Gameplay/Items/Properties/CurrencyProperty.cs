using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CurrencyType : int
{
    Copper = 1,
    Silver = 10,
    Gold = 100,
    Platinum = 1000,
    Cosmic = 10000
}

[System.Serializable]
public struct CurrencyTypeProperty
{
    public CurrencyType currencyType;
    public CurrencyProperty currencyProperty;
}


[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Currency")]
public class CurrencyProperty : ItemProperty
{
    [Header("Currency Settings")]
    [SerializeField]
    private uint value;

    public uint Value => value;
}
