using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class PlayerAttack : MonoBehaviour
{
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    [System.Serializable]
    private sealed class AttackSettings
    {
        public int damage = 1;
        public float range = 1.5f;
        public float radius = 0.75f;
        public float cooldown = 0.45f;
        public float movementLockDuration = 0.8f;
        public GameObject effectPrefab;
        public Vector3 effectOffset = new Vector3(0f, 0.1f, 1.5f);
        public Vector3 effectRotation;
        public Vector3 effectScale = Vector3.one;
        public float effectLifetime = 1.5f;
    }

    [Header("Attacks")]
    [SerializeField] private AttackSettings primaryAttack = new AttackSettings();
    [SerializeField] private AttackSettings secondaryAttack = new AttackSettings
    {
        damage = 2,
        range = 1.1f,
        radius = 1.2f,
        cooldown = 0.9f,
        movementLockDuration = 1f
    };

    [Header("Targets")]
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private readonly Collider[] hits = new Collider[16];
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private static float inputBlockedUntilRealtime;
    private float nextPrimaryAttackTime;
    private float nextSecondaryAttackTime;
    private float movementLockedUntil;

    public bool IsMovementLocked => Time.time < movementLockedUntil;

    public static void BlockInputForSeconds(float seconds)
    {
        inputBlockedUntilRealtime = Mathf.Max(
            inputBlockedUntilRealtime,
            Time.unscaledTime + Mathf.Max(seconds, 0f));
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || Time.unscaledTime < inputBlockedUntilRealtime || IsMovementLocked)
        {
            return;
        }

        if (CanAttack(nextPrimaryAttackTime) && IsPrimaryAttackPressed())
        {
            PerformAttack(primaryAttack);
            PlayAttackAnimation(Attack1Hash);
            LockMovement(primaryAttack);
            nextPrimaryAttackTime = Time.time + primaryAttack.cooldown;
            return;
        }

        if (CanAttack(nextSecondaryAttackTime) && IsSecondaryAttackPressed())
        {
            PerformAttack(secondaryAttack);
            PlayAttackAnimation(Attack2Hash);
            LockMovement(secondaryAttack);
            nextSecondaryAttackTime = Time.time + secondaryAttack.cooldown;
        }
    }

    private static bool CanAttack(float nextAttackTime)
    {
        return Time.time >= nextAttackTime;
    }

    private static bool IsPrimaryAttackPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    private static bool IsSecondaryAttackPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (keyboard != null && keyboard.rightShiftKey.wasPressedThisFrame)
            || (mouse != null && mouse.rightButton.wasPressedThisFrame);
    }

    private void PerformAttack(AttackSettings attack)
    {
        damagedTargets.Clear();
        SpawnAttackEffect(attack);

        Vector3 attackCenter = transform.position + transform.forward * attack.range;
        int hitCount = Physics.OverlapSphereNonAlloc(
            attackCenter,
            attack.radius,
            hits,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].transform.root == transform.root)
            {
                continue;
            }

            if (CombatTargeting.TryGetTarget(hits[i], out IDamageable damageable)
                && damageable is not ArtifactHealth
                && damagedTargets.Add(damageable))
            {
                if (damageable is IContextualDamageable contextualDamageable)
                {
                    contextualDamageable.TakeDamage(
                        attack.damage,
                        new DamageContext(DamageSourceType.PlayerNormalAttack, transform));
                }
                else
                {
                    damageable.TakeDamage(attack.damage);
                }
            }
        }
    }

    private void SpawnAttackEffect(AttackSettings attack)
    {
        if (attack.effectPrefab == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(
            attack.effectPrefab,
            transform.position
                + transform.right * attack.effectOffset.x
                + transform.up * attack.effectOffset.y
                + transform.forward * attack.effectOffset.z,
            transform.rotation * Quaternion.Euler(attack.effectRotation));

        effectInstance.transform.localScale = attack.effectScale;
        Destroy(effectInstance, attack.effectLifetime);
    }

    private void PlayAttackAnimation(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(triggerHash);
    }

    private void LockMovement(AttackSettings attack)
    {
        float lockDuration = attack.movementLockDuration > 0f
            ? attack.movementLockDuration
            : attack.cooldown;

        movementLockedUntil = Mathf.Max(
            movementLockedUntil,
            Time.time + Mathf.Max(lockDuration, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * primaryAttack.range,
            primaryAttack.radius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * secondaryAttack.range,
            secondaryAttack.radius);
    }
}
