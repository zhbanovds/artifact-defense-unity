using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WaveManager : MonoBehaviour
{
    private const float MinimumFirstWaveDelay = 10f;
    private const float MinimumWaveCompleteDelay = 10f;

    public event Action AllWavesCompleted;

    [System.Serializable]
    private sealed class Wave
    {
        public WaveSpawn[] spawnGroups;
        public BasicEnemy enemyPrefab;
        public int enemyCount = 3;
        public float spawnInterval = 1f;
    }

    [System.Serializable]
    private sealed class WaveSpawn
    {
        public BasicEnemy enemyPrefab;
        public int enemyCount = 3;
        public float spawnInterval = 1f;
    }

    [SerializeField] private ArtifactHealth targetArtifact;
    [SerializeField] private ArtifactEnergy targetEnergy;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Wave[] waves;
    [SerializeField] private float firstWaveDelay = 10f;
    [SerializeField] private float waveCompleteDelay = 10f;
    [SerializeField] private float finalVictoryDelay = 5f;
    [SerializeField] private bool startOnAwake = true;

    private int currentWaveIndex;
    private int aliveEnemies;
    private readonly List<BasicEnemy> spawnedEnemies = new List<BasicEnemy>();
    private bool isRunning;
    private bool isBetweenWaves;
    private bool isFinalVictoryPending;
    private float nextWaveCountdown;
    private bool artifactDestroyed;
    private Coroutine wavesRoutine;

    public int CurrentWaveNumber => currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0;
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int AliveEnemies => aliveEnemies;
    public bool IsRunning => isRunning;
    public bool IsBetweenWaves => isBetweenWaves;
    public float NextWaveCountdown => nextWaveCountdown;
    public bool IsFinalVictoryPending => isFinalVictoryPending;

    private void Start()
    {
        if (startOnAwake)
        {
            StartWaves();
        }
    }

    private void OnEnable()
    {
        if (targetArtifact != null)
        {
            targetArtifact.Destroyed += OnArtifactDestroyed;
        }
    }

    private void OnDisable()
    {
        if (targetArtifact != null)
        {
            targetArtifact.Destroyed -= OnArtifactDestroyed;
        }

        if (wavesRoutine != null)
        {
            StopCoroutine(wavesRoutine);
            wavesRoutine = null;
        }

        ClearCountdownState();
    }

    public void StartWaves()
    {
        if (isRunning || wavesRoutine != null)
        {
            return;
        }

        if (targetArtifact == null || spawnPoints == null || spawnPoints.Length == 0 || waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveManager is not configured.", this);
            return;
        }

        if (targetEnergy == null)
        {
            Debug.LogWarning("WaveManager Target Energy is not assigned. Enemies can spawn, but kills will not charge energy unless rewards find ArtifactEnergy in the scene.", this);
        }

        artifactDestroyed = targetArtifact.IsDestroyed;
        spawnedEnemies.Clear();
        wavesRoutine = StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        isRunning = true;
        currentWaveIndex = -1;

        yield return WaitForCountdown(GetFirstWaveDelay(), betweenWaves: true, finalVictory: false);

        if (artifactDestroyed || targetArtifact == null || targetArtifact.IsDestroyed)
        {
            FinishWavesRoutine();
            yield break;
        }

        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} started.", this);
            yield return SpawnWave(waves[currentWaveIndex]);

            while (aliveEnemies > 0)
            {
                yield return null;
            }

            yield return WaitForDeadEnemyObjectsToBeDestroyed();

            Debug.Log($"Wave {currentWaveIndex + 1} completed.", this);

            if (currentWaveIndex < waves.Length - 1)
            {
                yield return WaitForCountdown(GetWaveCompleteDelay(), betweenWaves: true, finalVictory: false);

                if (artifactDestroyed || targetArtifact == null || targetArtifact.IsDestroyed)
                {
                    FinishWavesRoutine();
                    yield break;
                }
            }
        }

        isRunning = false;
        yield return WaitForCountdown(finalVictoryDelay, betweenWaves: false, finalVictory: true);

        if (artifactDestroyed || targetArtifact == null || targetArtifact.IsDestroyed)
        {
            Debug.Log("All waves completed, but the artifact was destroyed before victory.", this);
            FinishWavesRoutine();
            yield break;
        }

        Debug.Log("All waves completed.", this);
        AllWavesCompleted?.Invoke();
        FinishWavesRoutine();
    }

    private IEnumerator WaitForCountdown(float duration, bool betweenWaves, bool finalVictory)
    {
        float delay = Mathf.Max(duration, 0f);
        if (delay <= 0f)
        {
            yield break;
        }

        isBetweenWaves = betweenWaves;
        isFinalVictoryPending = finalVictory;
        nextWaveCountdown = delay;
        Debug.Log($"WaveManager countdown started: {delay:0.0}s.", this);

        float elapsed = 0f;
        while (elapsed < delay)
        {
            nextWaveCountdown = Mathf.Max(delay - elapsed, 0f);

            if (artifactDestroyed || targetArtifact == null || targetArtifact.IsDestroyed)
            {
                yield break;
            }

            if (Time.timeScale > 0f)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        ClearCountdownState();
    }

    private float GetFirstWaveDelay()
    {
        return Mathf.Max(firstWaveDelay, MinimumFirstWaveDelay);
    }

    private float GetWaveCompleteDelay()
    {
        return Mathf.Max(waveCompleteDelay, MinimumWaveCompleteDelay);
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        if (wave == null)
        {
            yield break;
        }

        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
        {
            for (int i = 0; i < wave.spawnGroups.Length; i++)
            {
                yield return SpawnGroup(wave.spawnGroups[i]);
            }

            yield break;
        }

        yield return SpawnGroup(wave.enemyPrefab, wave.enemyCount, wave.spawnInterval);
    }

    private IEnumerator SpawnGroup(WaveSpawn spawnGroup)
    {
        if (spawnGroup == null)
        {
            yield break;
        }

        yield return SpawnGroup(spawnGroup.enemyPrefab, spawnGroup.enemyCount, spawnGroup.spawnInterval);
    }

    private IEnumerator SpawnGroup(BasicEnemy enemyPrefab, int enemyCount, float spawnInterval)
    {
        if (enemyPrefab == null)
        {
            yield break;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            if (spawnPoint != null)
            {
                SpawnEnemy(enemyPrefab, spawnPoint);
            }

            yield return new WaitForSeconds(Mathf.Max(spawnInterval, 0f));
        }
    }

    private void SpawnEnemy(BasicEnemy enemyPrefab, Transform spawnPoint)
    {
        BasicEnemy enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.Initialize(targetArtifact);
        enemy.Died += () => OnEnemyDied(enemy);

        if (enemy.TryGetComponent(out EnergyRewardOnDeath energyReward))
        {
            energyReward.Initialize(targetEnergy);
        }

        spawnedEnemies.Add(enemy);
        aliveEnemies++;
    }

    private void OnEnemyDied(BasicEnemy enemy)
    {
        aliveEnemies = Mathf.Max(aliveEnemies - 1, 0);
    }

    private IEnumerator WaitForDeadEnemyObjectsToBeDestroyed()
    {
        while (HasEnemyObjectsInScene())
        {
            if (artifactDestroyed || targetArtifact == null || targetArtifact.IsDestroyed)
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool HasEnemyObjectsInScene()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }

            return true;
        }

        return false;
    }

    private void OnArtifactDestroyed()
    {
        artifactDestroyed = true;
    }

    private void ClearCountdownState()
    {
        isBetweenWaves = false;
        isFinalVictoryPending = false;
        nextWaveCountdown = 0f;
    }

    private void FinishWavesRoutine()
    {
        isRunning = false;
        ClearCountdownState();
        wavesRoutine = null;
    }
}
