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
    public void GetDamaged(uint damage, DamageType type);
}