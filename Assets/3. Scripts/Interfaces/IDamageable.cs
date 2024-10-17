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
    public void GetDamaged(int damage, DamageType type);
}