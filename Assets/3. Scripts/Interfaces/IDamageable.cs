using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Blunt,
    Pierce,
    Slash
}

[System.Serializable]
public enum Hostility
{
    Friendly,
    Neutral,
    Enemy,
}


public interface IDamageable
{
    public uint GetCurrentHealth();
    public Hostility GetHostility();
    public bool GetDamaged(uint damage, DamageType type, Hostility hostility, Transform attacker);
}