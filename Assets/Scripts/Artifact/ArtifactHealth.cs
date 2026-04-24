using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArtifactHealth : MonoBehaviour, IDamageable
{
    public event Action Damaged;
    public event Action Destroyed;

    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDestroyed)
        {
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        Debug.Log($"{name} artifact HP: {CurrentHealth}/{maxHealth}", this);
        Damaged?.Invoke();

        if (IsDestroyed)
        {
            Destroyed?.Invoke();
        }
    }
}
