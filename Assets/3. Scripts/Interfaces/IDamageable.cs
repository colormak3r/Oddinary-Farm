using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Blunt,
    Pierce,
    Slash
}

public interface IDamageable
{
    public uint GetCurrentHealth();
    public void GetDamaged(uint damage, DamageType type);
}