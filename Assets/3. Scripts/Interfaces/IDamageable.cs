using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Blunt,
    Pierce,
    Slash,
    Water,
    Laser,
    Absolute,
}

[System.Serializable]
public enum Hostility
{
    Friendly,
    Neutral,
    Hostile,
    Absolute // Used for damage that ignores all hostility checks. Ignore combat music trigger. Use case: trap, debug, hunger stimulus, etc.
}

public interface IDamageable
{
    public uint CurrentHealthValue { get; }
    public Hostility Hostility { get; }
    public bool TakeDamage(uint damage, DamageType damageType, Hostility attackerHostility, Transform attacker);
}