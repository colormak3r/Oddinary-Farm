using UnityEngine.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Property", menuName = "Scriptable Objects/Projectile Property")]
public class ProjectileProperty : ScriptableObject
{
    [Header("Projectile Property")]
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float speed;
    [SerializeField]
    private uint damage = 1;
    [SerializeField]
    private float lifeTime = 5;
    [SerializeField]
    private DamageType damageType = DamageType.Pierce;
    [SerializeField]
    private Hostility hostility;

    public Sprite Sprite => sprite;
    public Animator Animator => animator;
    public float Speed => speed;
    public uint Damage => damage;
    public float LifeTime => lifeTime;
    public DamageType DamageType => damageType;
    public Hostility Hostility => hostility;
}
