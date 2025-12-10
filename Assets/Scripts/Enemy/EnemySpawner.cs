using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups; //List of groups of enemies to spawn in this wave
        public int waveQuota; //Number of enemies
        public float spawnInterval; //Spawn interval
        public int spawnCount; //Number of already spawned Enemies

    }

    [System.Serializable]
    public class EnemyGroup
    {
        public string enemyName;
        public int enemyCount;
        public int spawnCount;
        public GameObject enemyPrefab;
    }

    public List<Wave> waves; //List of all waves in game
    public int currentWaveCount; //Current wave index

    [Header("Boss Settings")]
    public GameObject bossPrefab; //Assign your boss prefab in the Inspector
    public bool bossActive = false; //Track whether a boss is alive
    public int bossWaveInterval = 10; //Every 10th wave will be a boss wave


    [Header("spawner Attributes")]
    float SpawnTimer; //Timer used to spawn the next enemy
    public int enemiesAlive;
    public int maxEnemiesAllowed;
    public bool maxEnemiesReached = false;
    public float waveInterval; //between each wave

    [Header("Spawn Positions")]
    public List<Transform> relativeSpawnPoints;

    Transform player;

    void PreloadEnemies()
    {
        //Pre-instantiate a few enemies off-screen to compile prefabs
        if (waves.Count > 0 && waves[0].enemyGroups.Count > 0)
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject dummy = Instantiate(waves[0].enemyGroups[0].enemyPrefab, Vector3.one * 9999f, Quaternion.identity);
                dummy.SetActive(false);
            }
        }
    }

    //Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<PlayerStats>().transform;

        Physics2D.Simulate(0.1f);

        foreach (Animator a in FindObjectsOfType<Animator>())
            a.Update(0f);

    #if TMP_PRESENT
        TMPro.TMP_Text[] allTMP = FindObjectsOfType<TMPro.TMP_Text>();
        foreach (var t in allTMP) t.ForceMeshUpdate();
    #endif

        PreloadEnemies();

        CalculateWaveQuota();
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        //GiveS Unity time to load assets & compile shaders
        yield return new WaitForSeconds(2f);

        //Starts the first wave normally
        StartCoroutine(SpawnWarmup());
    }

    IEnumerator SpawnWarmup()
{
    //Spawn enemies slowly for the first wave instead of all at once
    for (int i = 0; i < 10; i++) //just a few enemies to start
    {
        SpawnEnemies();
        yield return new WaitForSeconds(0.2f); //0.2 second between spawns
    }
}

    bool waveInProgress = false;

    //Update is called once per frame
    void Update()
    {
        if (!waveInProgress)
        {
            if (!bossActive && currentWaveCount < waves.Count && waves[currentWaveCount].spawnCount == 0)
            {
                waveInProgress = true;
                if ((currentWaveCount + 1) % bossWaveInterval == 0)
                    SpawnBoss();
                else
                    StartCoroutine(BeginNextWave());
            }
        }
        SpawnTimer += Time.deltaTime;
        
        //Check if enemy can be spawned
        if(SpawnTimer >= waves[currentWaveCount].spawnInterval)
        {
            SpawnTimer = 0f;
            SpawnEnemies();
        }
    }

    IEnumerator BeginNextWave()
    {
        yield return new WaitForSeconds(waveInterval);

        if(currentWaveCount < waves.Count - 1)
        {
            currentWaveCount++;
            foreach (var group in waves[currentWaveCount].enemyGroups)
                group.spawnCount = 0;
            waves[currentWaveCount].spawnCount = 0;
            CalculateWaveQuota();
            Debug.Log($"➡️ Starting Wave {currentWaveCount + 1}, quota: {waves[currentWaveCount].waveQuota}");
        }
        else
        {
            Debug.Log("✅ All waves complete!");
        }

         waveInProgress = false;
    }

    void CalculateWaveQuota()
    {
        int currentWaveQuota = 0;
        foreach(var enemyGroup in waves[currentWaveCount].enemyGroups)
        {
            currentWaveQuota += enemyGroup.enemyCount;
        }

        waves[currentWaveCount].waveQuota = currentWaveQuota;
        Debug.LogWarning(currentWaveQuota);
    }


    /*void firstWave()
    {
        if(currentWaveCount < waves.Count && waves[currentWaveCount].spawnCount == 0)
        {
            StartCoroutine(BeginNextWave());
        }

        SpawnTimer = 0f;
        SpawnEnemies();
    }
    */

    void SpawnEnemies()
    {
        //Prevent regular enemies from spawning while boss is active
        //if (bossActive)
        //return;

        //Check if if minimum number of enemies have been spawned
        if(waves[currentWaveCount].spawnCount < waves[currentWaveCount].waveQuota && !maxEnemiesReached)
        {
            //Spawn each type of enemy till quota is hit
            foreach(var enemyGroup in waves[currentWaveCount].enemyGroups)
            {
                //check if min num of enemies of this type have been spawned
                if(enemyGroup.spawnCount < enemyGroup.enemyCount)
                {
                    if(enemiesAlive >= maxEnemiesAllowed)
                    {
                        maxEnemiesReached = true;
                        return;
                    }

                    int randomIndex = Random.Range(0, relativeSpawnPoints.Count);
                    UnityEngine.Vector3 spawnOffset = relativeSpawnPoints[randomIndex].localPosition; // Use localPosition for relative offset
                    UnityEngine.Vector3 spawnPosition = player.position + spawnOffset; // Add offset to player's position

                    Instantiate(enemyGroup.enemyPrefab, spawnPosition, UnityEngine.Quaternion.identity);
                    //Spawn enemies at random positions close to the player
                    //Instantiate(enemyGroup.enemyPrefab, player.position + relativeSpawnPoints[Random.Range(1, relativeSpawnPoints.Count)].position, UnityEngine.Quaternion.identity);

                    enemyGroup.spawnCount++;
                    waves[currentWaveCount].spawnCount++;
                    enemiesAlive++;
                }
            }
        }

        if(enemiesAlive < maxEnemiesAllowed)
        {
            maxEnemiesReached = false;
        }
    }

    public void OnEnemyKilled()
    {
        enemiesAlive--;
    }

    void SpawnBoss()
    {
        bossActive = true;

        //Pick a random spawn point around the player
        int randomIndex = Random.Range(0, relativeSpawnPoints.Count);
        Vector3 spawnOffset = relativeSpawnPoints[randomIndex].localPosition;
        Vector3 spawnPosition = player.position + spawnOffset;

        //Spawn the boss
        Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Boss spawned on Wave {currentWaveCount + 1}");
    }

    public void BossDefeated()
    {
        bossActive = false;
        Debug.Log("Boss defeated! Next waves will resume normally.");
        //Automatically start the next wave after boss defeat
        StartCoroutine(BeginNextWave());
    }

}
