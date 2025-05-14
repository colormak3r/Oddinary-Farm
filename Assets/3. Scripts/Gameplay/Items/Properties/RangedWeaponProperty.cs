using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeaponProperty : WeaponProperty
{
    [SerializeField]
    private ProjectileProperty projectileProperty;
    [SerializeField]
    private int projectileCount = 1;
    [SerializeField]
    private float projectileSpread = 30;
    [SerializeField]
    private bool spreadEvenly;

    public ProjectileProperty ProjectileProperty => projectileProperty;
    public int ProjectileCount => projectileCount;
    public float ProjectileSpread => projectileSpread;
    public bool SpreadEvenly => spreadEvenly;
}