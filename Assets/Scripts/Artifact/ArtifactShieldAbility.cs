using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ArtifactShieldAbility : MonoBehaviour
{
    [SerializeField] private ArtifactHealth artifactHealth;
    [SerializeField] private Key activationKey = Key.Q;
    [SerializeField] private float radius = 5f;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
    [SerializeField] private float duration = 4f;
    [FormerlySerializedAs("activationWindowDuration")]
    [SerializeField] private float requiredSustainedDamageDuration = 5f;
    [SerializeField] private float damageContinuityGrace = 1.5f;
    [SerializeField] private float cooldown = 20f;
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Visual Effect")]
    [SerializeField] private GameObject shieldEffectPrefab;
    [SerializeField] private Vector3 shieldEffectOffset = Vector3.zero;
    [SerializeField] private Vector3 shieldEffectRotation;
    [SerializeField] private Vector3 shieldEffectScale = Vector3.one;

    private readonly Collider[] hits = new Collider[64];
    private readonly HashSet<ISlowable> slowedTargets = new HashSet<ISlowable>();
    private float activeUntil;
    private float damagePressureStartedAt = -1f;
    private float lastDamageTime = -1f;
    private float nextActivationTime;
    private bool shieldUnlocked;
    private GameObject activeShieldEffectInstance;

    public bool IsActive => Time.time < activeUntil;
    public bool IsUnderDamagePressure => !shieldUnlocked
        && !IsOnCooldown
        && lastDamageTime >= 0f
        && Time.time - lastDamageTime <= damageContinuityGrace;
    public bool IsReady => !IsOnCooldown && shieldUnlocked;
    public bool IsOnCooldown => Time.time < nextActivationTime;
    public float CooldownRemaining => Mathf.Max(nextActivationTime - Time.time, 0f);
    public float SustainedDamageRemaining => IsUnderDamagePressure
        ? Mathf.Max(requiredSustainedDamageDuration - (Time.time - damagePressureStartedAt), 0f)
        : requiredSustainedDamageDuration;

    private void Awake()
    {
        if (artifactHealth == null)
        {
            artifactHealth = GetComponent<ArtifactHealth>();
        }
    }

    private void OnEnable()
    {
        if (artifactHealth != null)
        {
            artifactHealth.Damaged += RegisterDamagePressure;
        }
    }

    private void OnDisable()
    {
        if (artifactHealth != null)
        {
            artifactHealth.Damaged -= RegisterDamagePressure;
        }
    }

    private void Update()
    {
        if (WasActivationPressed())
        {
            TryActivate();
        }

        UpdateDamagePressure();

        if (IsActive)
        {
            ApplyShieldEffect();
        }
    }

    private bool WasActivationPressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null
            && keyboard[activationKey].wasPressedThisFrame;
    }

    private void TryActivate()
    {
        if (!IsReady)
        {
            return;
        }

        activeUntil = Time.time + duration;
        nextActivationTime = Time.time + cooldown;
        shieldUnlocked = false;
        ResetDamagePressure();
        SpawnShieldEffect();
        Debug.Log($"{name} shield activated.", this);
    }

    private void RegisterDamagePressure()
    {
        if (IsOnCooldown)
        {
            return;
        }

        if (lastDamageTime < 0f || Time.time - lastDamageTime > damageContinuityGrace)
        {
            damagePressureStartedAt = Time.time;
        }

        lastDamageTime = Time.time;
    }

    private void UpdateDamagePressure()
    {
        if (shieldUnlocked || IsOnCooldown)
        {
            return;
        }

        if (lastDamageTime < 0f)
        {
            return;
        }

        if (Time.time - lastDamageTime > damageContinuityGrace)
        {
            ResetDamagePressure();
            return;
        }

        if (Time.time - damagePressureStartedAt >= requiredSustainedDamageDuration)
        {
            shieldUnlocked = true;
            Debug.Log($"{name} shield is ready.", this);
        }
    }

    private void ResetDamagePressure()
    {
        damagePressureStartedAt = -1f;
        lastDamageTime = -1f;
    }

    private void ApplyShieldEffect()
    {
        slowedTargets.Clear();

        int colliderCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            hits,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliderCount; i++)
        {
            if (CombatTargeting.TryGetTarget(hits[i], out ISlowable slowable)
                && slowedTargets.Add(slowable))
            {
                slowable.ApplySlow(slowMultiplier, Time.deltaTime + 0.1f);
            }
        }
    }

    private void SpawnShieldEffect()
    {
        if (shieldEffectPrefab == null)
        {
            return;
        }

        if (activeShieldEffectInstance != null)
        {
            Destroy(activeShieldEffectInstance);
        }

        activeShieldEffectInstance = Instantiate(
            shieldEffectPrefab,
            transform.position + shieldEffectOffset,
            Quaternion.Euler(shieldEffectRotation));

        activeShieldEffectInstance.transform.localScale = shieldEffectScale;
        Destroy(activeShieldEffectInstance, duration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
