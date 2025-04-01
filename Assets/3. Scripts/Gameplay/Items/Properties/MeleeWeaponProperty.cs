using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MeleeWeapon Property", menuName = "Scriptable Objects/Item/Melee Weapon Property")]
public class MeleeWeaponProperty : WeaponProperty
{
    [Header("Melee Weapon Settings")]
    [SerializeField]
    private float radius = 1f;
    [SerializeField]
    private uint damage = 1;
    [SerializeField]
    private DamageType damageType;
    [SerializeField]
    private Hostility hostility;
    [SerializeField]
    private float knockbackForce = 500f;
    [SerializeField]
    private LayerMask damageableLayer;
    [SerializeField]
    private bool canHarvest;

    public float Radius => radius;
    public uint Damage => damage;
    public DamageType DamageType => damageType;
    public Hostility Hostility => hostility;
    public float KnockbackForce => knockbackForce;
    public LayerMask DamageableLayer => damageableLayer;
    public bool CanHarvest => canHarvest;
}
