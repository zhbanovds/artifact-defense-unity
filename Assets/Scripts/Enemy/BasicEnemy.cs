using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class BasicEnemy : MonoBehaviour, IContextualDamageable, IDeathNotifier, ISlowable
{
    private static float nextGlobalCounterTime;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    public event Action Died;

    [Header("Stats")]
    [SerializeField] private EnemyStats stats;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Movement")]
    [SerializeField] private ArtifactHealth targetArtifact;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 540f;

    [Header("Player Avoidance")]
    [SerializeField] private float playerAvoidanceRadius = 1.3f;
    [SerializeField] private float playerAvoidanceLookAhead = 2.2f;
    [SerializeField] private float playerAvoidanceStrength = 1.2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private int attackDamage = 5;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackAnimationDuration = 0.75f;

    [Header("Counter Reaction")]
    [Range(0f, 1f)]
    [SerializeField] private float counterChance = 0.1f;
    [SerializeField] private float counterTriggerCooldown = 2f;
    [SerializeField] private float counterFocusDuration = 1.5f;
    [SerializeField] private float counterAttackRange = 1.3f;
    [SerializeField] private float counterKnockbackForce = 8f;
    [SerializeField] private float counterStunDuration = 0.45f;
    [SerializeField] private float counterAttackAnimationDuration = 1.35f;
    [SerializeField] private float counterImpactDelay = 0.45f;
    [SerializeField] private float counterGlobalCooldown = 1.25f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathDestroyDelay = 1.2f;

    [Header("Attack Effect")]
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private Vector3 attackEffectOffset = new Vector3(0f, 0.1f, 1f);
    [SerializeField] private Vector3 attackEffectRotation;
    [SerializeField] private Vector3 attackEffectScale = Vector3.one;
    [SerializeField] private float attackEffectLifetime = 1.5f;

    private CharacterController characterController;
    private int currentHealth;
    private float verticalVelocity;
    private float nextAttackTime;
    private float slowMultiplier = 1f;
    private float slowedUntil;
    private Collider targetArtifactCollider;
    private bool isDead;
    private bool isAttackingArtifact;
    private float artifactAttackEndsAt;
    private Transform counterTarget;
    private PlayerImpactReceiver counterTargetImpactReceiver;
    private float counterFocusUntil;
    private float nextCounterTriggerTime;
    private float counterAttackEndsAt;
    private float counterImpactTime;
    private bool isCountering;
    private bool isCounterAttackPlaying;
    private bool counterImpactApplied;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGlobalCounterLimiter()
    {
        nextGlobalCounterTime = 0f;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        ResolvePlayerTransform();
        ApplyStats();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            SetMoving(false);
            return;
        }

        if (isCountering)
        {
            UpdateCounterReaction();
            return;
        }

        if (isAttackingArtifact)
        {
            UpdateArtifactAttack();
            return;
        }

        if (targetArtifact == null || targetArtifact.IsDestroyed)
        {
            SetMoving(false);
            ApplyGravity(Vector3.zero);
            return;
        }

        Vector3 directionToArtifact = GetDirectionToArtifact();
        directionToArtifact.y = 0f;

        float distanceToArtifact = GetDistanceToArtifact();

        if (distanceToArtifact > attackRange)
        {
            Vector3 moveDirection = GetMoveDirectionToArtifact(directionToArtifact);
            SetMoving(true);
            Move(moveDirection);
            RotateTowards(moveDirection);
            return;
        }

        SetMoving(false);
        ApplyGravity(Vector3.zero);
        AttackArtifact();
    }

    public void Initialize(ArtifactHealth artifact)
    {
        targetArtifact = artifact;
        targetArtifactCollider = targetArtifact != null
            ? targetArtifact.GetComponentInChildren<Collider>()
            : null;
    }

    private void ApplyStats()
    {
        if (stats == null)
        {
            return;
        }

        maxHealth = stats.MaxHealth;
        moveSpeed = stats.MoveSpeed;
        rotationSpeed = stats.RotationSpeed;
        attackRange = stats.AttackRange;
        attackDamage = stats.AttackDamage;
        attackCooldown = stats.AttackCooldown;
        counterChance = stats.CounterChance;
        counterTriggerCooldown = stats.CounterTriggerCooldown;
        counterFocusDuration = stats.CounterFocusDuration;
        counterAttackRange = stats.CounterAttackRange;
        counterKnockbackForce = stats.CounterKnockbackForce;
        counterStunDuration = stats.CounterStunDuration;
    }

    public void TakeDamage(int damage)
    {
        ApplyDamage(damage);
    }

    public void TakeDamage(int damage, DamageContext context)
    {
        if (!ApplyDamage(damage))
        {
            return;
        }

        TryStartCounterReaction(context);
    }

    private bool ApplyDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return false;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"{name} enemy HP: {currentHealth}/{maxHealth}", this);

        if (currentHealth == 0)
        {
            Die();
            return false;
        }

        return true;
    }

    public void ApplySlow(float speedMultiplier, float duration)
    {
        if (isDead || duration <= 0f)
        {
            return;
        }

        slowMultiplier = Mathf.Clamp(speedMultiplier, 0.1f, 1f);
        slowedUntil = Mathf.Max(slowedUntil, Time.time + duration);
    }

    private void Move(Vector3 moveDirection)
    {
        ApplyGravity(moveDirection * GetCurrentMoveSpeed());
    }

    private Vector3 GetMoveDirectionToArtifact(Vector3 directionToArtifact)
    {
        Vector3 baseDirection = directionToArtifact.normalized;
        if (baseDirection == Vector3.zero)
        {
            return Vector3.zero;
        }

        ResolvePlayerTransform();
        if (playerTransform == null || playerAvoidanceRadius <= 0f || playerAvoidanceStrength <= 0f)
        {
            return baseDirection;
        }

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        float forwardDistance = Vector3.Dot(toPlayer, baseDirection);
        if (forwardDistance < -0.25f || forwardDistance > playerAvoidanceLookAhead)
        {
            return baseDirection;
        }

        Vector3 closestPointOnPath = baseDirection * forwardDistance;
        Vector3 offsetFromPath = toPlayer - closestPointOnPath;
        if (offsetFromPath.magnitude > playerAvoidanceRadius)
        {
            return baseDirection;
        }

        Vector3 right = Vector3.Cross(Vector3.up, baseDirection).normalized;
        float side = Vector3.Dot(toPlayer, right) >= 0f ? -1f : 1f;
        Vector3 awayFromPlayer = toPlayer.sqrMagnitude > 0.001f
            ? -toPlayer.normalized
            : right * side;

        Vector3 avoidance = right * side + awayFromPlayer * 0.35f;
        return (baseDirection + avoidance.normalized * playerAvoidanceStrength).normalized;
    }

    private float GetCurrentMoveSpeed()
    {
        if (Time.time >= slowedUntil)
        {
            slowMultiplier = 1f;
        }

        return moveSpeed * slowMultiplier;
    }

    private void ApplyGravity(Vector3 horizontalVelocity)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetDirectionToArtifact()
    {
        return targetArtifact.transform.position - transform.position;
    }

    private float GetDistanceToArtifact()
    {
        if (targetArtifactCollider == null)
        {
            targetArtifactCollider = targetArtifact.GetComponentInChildren<Collider>();
        }

        Vector3 targetPoint = targetArtifactCollider != null
            ? targetArtifactCollider.ClosestPoint(transform.position)
            : targetArtifact.transform.position;

        Vector3 offset = targetPoint - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void AttackArtifact()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        targetArtifact.TakeDamage(attackDamage);
        SpawnAttackEffect();
        PlayAttackAnimation();
        nextAttackTime = Time.time + attackCooldown;
        isAttackingArtifact = true;
        artifactAttackEndsAt = Time.time + Mathf.Max(attackAnimationDuration, 0f);
    }

    private void UpdateArtifactAttack()
    {
        SetMoving(false);
        ApplyGravity(Vector3.zero);

        if (Time.time < artifactAttackEndsAt)
        {
            return;
        }

        isAttackingArtifact = false;
    }

    private void TryStartCounterReaction(DamageContext context)
    {
        if (isDead
            || isAttackingArtifact
            || isCountering
            || Time.timeScale <= 0f
            || context.SourceType != DamageSourceType.PlayerNormalAttack
            || context.SourceTransform == null
            || counterChance <= 0f
            || Time.time < nextCounterTriggerTime
            || Time.time < nextGlobalCounterTime
            || UnityEngine.Random.value > counterChance)
        {
            return;
        }

        counterTarget = context.SourceTransform;
        counterTargetImpactReceiver = counterTarget.GetComponentInParent<PlayerImpactReceiver>();
        counterFocusUntil = Time.time + Mathf.Max(counterFocusDuration, 0f);
        nextCounterTriggerTime = Time.time + Mathf.Max(counterTriggerCooldown, 0f);
        nextGlobalCounterTime = Time.time + Mathf.Max(counterGlobalCooldown, 0f);
        isCountering = true;
    }

    private void UpdateCounterReaction()
    {
        if (isCounterAttackPlaying)
        {
            ApplyGravity(Vector3.zero);

            if (counterTarget != null)
            {
                Vector3 lookDirection = counterTarget.position - transform.position;
                lookDirection.y = 0f;
                RotateTowards(lookDirection);
            }

            if (!counterImpactApplied && Time.time >= counterImpactTime)
            {
                ApplyCounterImpact();
            }

            if (Time.time >= counterAttackEndsAt)
            {
                EndCounterReaction();
            }

            return;
        }

        if (counterTarget == null || Time.time >= counterFocusUntil)
        {
            EndCounterReaction();
            return;
        }

        Vector3 directionToPlayer = counterTarget.position - transform.position;
        directionToPlayer.y = 0f;

        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer > counterAttackRange)
        {
            Vector3 moveDirection = directionToPlayer.normalized;
            SetMoving(true);
            Move(moveDirection);
            RotateTowards(moveDirection);
            return;
        }

        SetMoving(false);
        ApplyGravity(Vector3.zero);
        RotateTowards(directionToPlayer);
        StartCounterAttackPlayer();
    }

    private void StartCounterAttackPlayer()
    {
        SpawnAttackEffect();
        PlayAttackAnimation();
        isCounterAttackPlaying = true;
        counterImpactApplied = false;

        float attackAnimationDuration = counterAttackAnimationDuration > 0f
            ? counterAttackAnimationDuration
            : 1.35f;

        counterAttackEndsAt = Time.time + attackAnimationDuration;
        counterImpactTime = Time.time + Mathf.Clamp(counterImpactDelay, 0f, attackAnimationDuration);
    }

    private void ApplyCounterImpact()
    {
        counterImpactApplied = true;
        if (counterTargetImpactReceiver == null)
        {
            return;
        }

        counterTargetImpactReceiver.ReceiveImpact(
            transform.position,
            counterKnockbackForce,
            counterStunDuration);
    }

    private void EndCounterReaction()
    {
        isCountering = false;
        isCounterAttackPlaying = false;
        counterImpactApplied = false;
        counterTarget = null;
        counterTargetImpactReceiver = null;
        SetMoving(false);
    }

    private void Die()
    {
        isDead = true;
        isAttackingArtifact = false;
        isCountering = false;
        isCounterAttackPlaying = false;
        counterImpactApplied = false;
        counterTarget = null;
        counterTargetImpactReceiver = null;
        SetMoving(false);
        PlayDeathAnimation();
        characterController.enabled = false;
        Died?.Invoke();
        Destroy(gameObject, deathDestroyDelay);
    }

    private void SetMoving(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, isMoving);
    }

    private void PlayAttackAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(AttackHash);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(DieHash);
    }

    private void SpawnAttackEffect()
    {
        if (attackEffectPrefab == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(
            attackEffectPrefab,
            transform.position
                + transform.right * attackEffectOffset.x
                + transform.up * attackEffectOffset.y
                + transform.forward * attackEffectOffset.z,
            transform.rotation * Quaternion.Euler(attackEffectRotation));

        effectInstance.transform.localScale = attackEffectScale;
        Destroy(effectInstance, attackEffectLifetime);
    }

    private void ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return;
        }

        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, counterAttackRange);
    }
}
