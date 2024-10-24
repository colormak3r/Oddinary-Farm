using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private float range = 2f;
    [SerializeField]
    private float radius = 1f;
    [SerializeField]
    private int damage = 1;
    [SerializeField]
    private DamageType damageType;
    [SerializeField]
    private float knockbackForce = 500f;
    [SerializeField]
    private LayerMask damageableLayer;

    public float Range => range;
    public float Radius => radius;
    public int Damage => damage;
    public DamageType DamageType => damageType;
    public float KnockbackForce => knockbackForce;
    public LayerMask DamageableLayer => damageableLayer;
}
