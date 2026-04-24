using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnergyRewardOnDeath : MonoBehaviour
{
    [SerializeField] private int energyReward = 10;
    [SerializeField] private ArtifactEnergy targetEnergy;

    private IDeathNotifier deathNotifier;

    public int EnergyReward => energyReward;

    private void Awake()
    {
        deathNotifier = GetComponent<IDeathNotifier>();

        if (deathNotifier != null)
        {
            deathNotifier.Died += GiveReward;
        }
        else
        {
            Debug.LogWarning($"{name} has EnergyRewardOnDeath, but no IDeathNotifier component.", this);
        }

        if (targetEnergy == null)
        {
            targetEnergy = FindAnyObjectByType<ArtifactEnergy>();
        }
    }

    private void OnDestroy()
    {
        if (deathNotifier != null)
        {
            deathNotifier.Died -= GiveReward;
        }
    }

    public void Initialize(ArtifactEnergy energy)
    {
        if (energy == null)
        {
            return;
        }

        targetEnergy = energy;
    }

    private void GiveReward()
    {
        if (targetEnergy == null)
        {
            Debug.LogWarning($"{name} cannot give energy reward because Target Energy is not assigned.", this);
            return;
        }

        targetEnergy.AddEnergy(energyReward);
    }
}
