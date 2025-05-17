using UnityEngine;

[System.Serializable]       // Serializable in the insprctor as long as the object inherits the interface
public enum DamageType
{
    Blunt,
    Pierce,
    Slash,
    Water,
}

[System.Serializable]       // Serializable in the insprctor as long as the object inherits the interface
public enum Hostility
{
    Friendly,
    Neutral,
    Hostile,
}

// Every damagable entity must include this interface
public interface IDamageable
{
    public uint GetCurrentHealth();
    public Hostility GetHostility();
    public bool TakeDamage(uint damage, DamageType type, Hostility hostility, Transform attacker);
}