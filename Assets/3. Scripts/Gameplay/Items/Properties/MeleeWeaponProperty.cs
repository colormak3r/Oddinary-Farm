using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private float radius = 1f;
    [SerializeField]
    private uint damage = 1;
    [SerializeField]
    private DamageType damageType;
    [SerializeField]
    private float knockbackForce = 500f;
    [SerializeField]
    private LayerMask damageableLayer;

    public float Radius => radius;
    public uint Damage => damage;
    public DamageType DamageType => damageType;
    public float KnockbackForce => knockbackForce;
    public LayerMask DamageableLayer => damageableLayer;
}
