using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
}

public interface IContextualDamageable : IDamageable
{
    void TakeDamage(int damage, DamageContext context);
}

public enum DamageSourceType
{
    Unknown,
    PlayerNormalAttack,
    PlayerSpecial,
    ArtifactSpecial,
    ArtifactShield
}

public readonly struct DamageContext
{
    public DamageContext(DamageSourceType sourceType, Transform sourceTransform)
    {
        SourceType = sourceType;
        SourceTransform = sourceTransform;
    }

    public DamageSourceType SourceType { get; }
    public Transform SourceTransform { get; }
}
