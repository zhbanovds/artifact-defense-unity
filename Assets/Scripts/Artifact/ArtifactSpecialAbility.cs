using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ArtifactSpecialAbility : MonoBehaviour
{
    [SerializeField] private ArtifactEnergy energy;
    [SerializeField] private Key activationKey = Key.None;
    [SerializeField] private int damage = 5;
    [SerializeField] private float radius = 6f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly Collider[] hits = new Collider[64];
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        if (energy == null)
        {
            energy = GetComponent<ArtifactEnergy>();
        }

        if (energy == null)
        {
            energy = FindAnyObjectByType<ArtifactEnergy>();
        }

        if (energy == null)
        {
            Debug.LogWarning($"{name} special ability has no ArtifactEnergy assigned.", this);
        }
    }

    private void Update()
    {
        if (activationKey == Key.None || energy == null || !energy.IsFull || !WasActivationPressed())
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
        damagedTargets.Clear();

        int colliderCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            hits,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliderCount; i++)
        {
            if (CombatTargeting.TryGetTarget(hits[i], out IDamageable damageable)
                && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        energy.Clear();
        Debug.Log($"{name} special activated. Damaged targets: {damagedTargets.Count}.", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
