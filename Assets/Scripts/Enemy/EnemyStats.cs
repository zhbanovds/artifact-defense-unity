using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Artifact Defense/Enemy Stats")]
public sealed class EnemyStats : ScriptableObject
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private int attackDamage = 5;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Counter Reaction")]
    [Range(0f, 1f)]
    [SerializeField] private float counterChance = 0.1f;
    [SerializeField] private float counterTriggerCooldown = 2f;
    [SerializeField] private float counterFocusDuration = 1.5f;
    [SerializeField] private float counterAttackRange = 1.3f;
    [SerializeField] private float counterKnockbackForce = 8f;
    [SerializeField] private float counterStunDuration = 0.45f;

    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float AttackRange => attackRange;
    public int AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public float CounterChance => counterChance;
    public float CounterTriggerCooldown => counterTriggerCooldown;
    public float CounterFocusDuration => counterFocusDuration;
    public float CounterAttackRange => counterAttackRange;
    public float CounterKnockbackForce => counterKnockbackForce;
    public float CounterStunDuration => counterStunDuration;
}
