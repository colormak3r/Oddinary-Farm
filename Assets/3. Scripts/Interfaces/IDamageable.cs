using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Blunt,
    Pierce,
    Slash,
    Water,
}

[System.Serializable]
public enum Hostility
{
    Friendly,
    Neutral,
    Hostile,
}


public interface IDamageable
{
    public uint GetCurrentHealth();
    public Hostility GetHostility();
    public bool TakeDamage(uint damage, DamageType type, Hostility hostility, Transform attacker);
}