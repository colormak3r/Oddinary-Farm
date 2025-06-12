using UnityEngine;

[CreateAssetMenu(fileName = "Laser Property", menuName = "Scriptable Objects/Item/Laser")]
public class LaserWeaponProperty : WeaponProperty
{
    [Header("Laser Settings")]
    [SerializeField]
    private uint damage = 1;
    public uint Damage => damage;
    [SerializeField]
    private float radius = 0.1f;
    public float Radius => radius;
    [SerializeField]
    private float width = 1f;
    public float Width => width;
    [SerializeField]
    private float maxDuration = 1f;
    public float MaxDuration => maxDuration;
    [SerializeField]
    private float frequency = 0.5f;
    public float Frequency => frequency;
    [SerializeField]
    private Hostility hostility;
    public Hostility Hostility => hostility;

    [Header("Sound Effect")]
    [SerializeField]
    private AudioClip chargeStartSfx;
    public AudioClip ChargeStartSfx => chargeStartSfx;
    [SerializeField]
    private AudioClip prematureShotSfx;
    public AudioClip PrematureShotSfx => prematureShotSfx;
    [SerializeField]
    private AudioClip chargingUpSfx;
    public AudioClip ChargingUpSfx => chargingUpSfx;
    [SerializeField]
    private AudioClip fullChargeShotSfx;
    public AudioClip FullChargeShotSfx => fullChargeShotSfx;
    [SerializeField]
    private AudioClip chargeTickSfx;
    public AudioClip ChargeTickSfx => chargeTickSfx;
}
