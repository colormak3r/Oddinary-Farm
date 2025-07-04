using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Blunt,
    Pierce,
    Slash,
    Water,
    Laser,
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
    public uint CurrentHealth { get; }
    public Hostility Hostility { get; }
    public bool TakeDamage(uint damage, DamageType type, Hostility attackerHostility, Transform attacker);
}