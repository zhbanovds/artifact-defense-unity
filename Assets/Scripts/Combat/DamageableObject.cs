using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageableObject : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;

    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}", this);
    }
}
