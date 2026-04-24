using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerSpecialAbility : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private ArtifactEnergy energy;
    [SerializeField] private Key activationKey = Key.E;

    [Header("Damage")]
    [SerializeField] private int damage = 5;
    [SerializeField] private float radius = 6f;
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Visual Effect")]
    [SerializeField] private GameObject specialEffectPrefab;
    [SerializeField] private Vector3 specialEffectOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float specialEffectLifetime = 2f;

    private readonly Collider[] hits = new Collider[64];
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        if (energy == null)
        {
            energy = FindAnyObjectByType<ArtifactEnergy>();
        }

        if (energy == null)
        {
            Debug.LogWarning($"{name} player special ability has no ArtifactEnergy assigned.", this);
        }
    }

    private void Update()
    {
        if (energy == null || !energy.IsFull || !WasActivationPressed())
        {
            return;
        }

        Activate();
    }

    private bool WasActivationPressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null
            && keyboard[activationKey].wasPressedThisFrame;
    }

    private void Activate()
    {
        SpawnSpecialEffect();
        damagedTargets.Clear();

        int colliderCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            hits,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliderCount; i++)
        {
            if (hits[i] == null || hits[i].transform.root == transform.root)
            {
                continue;
            }

            if (CombatTargeting.TryGetTarget(hits[i], out IDamageable damageable)
                && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        energy.Clear();
        Debug.Log($"{name} player special activated. Damaged targets: {damagedTargets.Count}.", this);
    }

    private void SpawnSpecialEffect()
    {
        if (specialEffectPrefab == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(
            specialEffectPrefab,
            transform.position + specialEffectOffset,
            Quaternion.identity);

        Destroy(effectInstance, specialEffectLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
