using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArtifactEnergy : MonoBehaviour
{
    [SerializeField] private int maxEnergy = 100;

    public int CurrentEnergy { get; private set; }
    public int MaxEnergy => maxEnergy;
    public bool IsFull => CurrentEnergy >= maxEnergy;

    public void AddEnergy(int amount)
    {
        if (amount <= 0 || IsFull)
        {
            return;
        }

        CurrentEnergy = Mathf.Min(CurrentEnergy + amount, maxEnergy);
        Debug.Log($"{name} energy: {CurrentEnergy}/{maxEnergy}", this);
    }

    public void Clear()
    {
        CurrentEnergy = 0;
    }
}
