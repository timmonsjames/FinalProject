using UnityEngine;
using Unity.MLAgents;

public class AntBulliers : MonoBehaviour
{
    [Header("Training Zone")]
    [SerializeField] private Transform[] antSpawnPoints;
    [SerializeField] private Transform[] foodSpawnPoints;
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private int antsPerZone = 2;
    [SerializeField] private int foodPerZone = 10;

    [Header("Bounds")]
    [SerializeField] private Vector3 zoneSize = new Vector3(10f, 3f, 10f);

    private AntAI[] spawnedAnts;
    private FoodSpawning foodSpawning;

    private void Start()
    {
        foodSpawning = GetComponentInChildren<FoodSpawning>();
        SpawnAnts();
    }

    public void ResetZone()
    {
        if (spawnedAnts != null)
            foreach (var ant in spawnedAnts)
                if (ant != null) ant.gameObject.SetActive(false);

        foodSpawning?.SpawnFood();
        SpawnAnts();
    }

    private void SpawnAnts()
    {
        spawnedAnts = new AntAI[antsPerZone];

        for (int i = 0; i < antsPerZone; i++)
        {
            Transform point = antSpawnPoints[i % antSpawnPoints.Length];
            GameObject go = Instantiate(antPrefab, point.position, Quaternion.identity, transform);
            spawnedAnts[i] = go.GetComponent<AntAI>();
        }
    }

    public void OnAntEpisodeEnd()
    {
        bool allDone = true;
        foreach (var ant in spawnedAnts)
            if (ant != null && ant.gameObject.activeInHierarchy) { allDone = false; break; }

        if (allDone) ResetZone();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawCube(transform.position, zoneSize);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireCube(transform.position, zoneSize);
    }
#endif
}