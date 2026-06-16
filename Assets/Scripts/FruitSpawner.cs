using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject[] fruitPrefabs;
    public int numberOfFruits = 5;

    [Header("Spawn Area")]
    public float spawnRangeX = 22f;
    public float spawnRangeZ = 22f;
    public float fixedY = 0.5f;

    [HideInInspector]
    public Transform[] spawnedFruits;

    void Start()
    {
        SpawnFruits();
    }

    public void SpawnFruits()
    {
        if (spawnedFruits != null)
        {
            foreach (Transform f in spawnedFruits)
                if (f != null) Destroy(f.gameObject);
        }

        spawnedFruits = new Transform[numberOfFruits];

        for (int i = 0; i < numberOfFruits; i++)
        {
            Vector3 randomLocalPos;
            int maxAttempts = 30;
            bool validPos;

            do
            {
                // Local position relative to TrainingArea
                randomLocalPos = new Vector3(
                    Random.Range(-spawnRangeX, spawnRangeX),
                    fixedY,
                    Random.Range(-spawnRangeZ, spawnRangeZ)
                );

                validPos = true;
                for (int j = 0; j < i; j++)
                {
                    if (Vector3.Distance(randomLocalPos, spawnedFruits[j].localPosition) < 2f)
                    {
                        validPos = false;
                        break;
                    }
                }
                maxAttempts--;
            } while (!validPos && maxAttempts > 0);

            GameObject prefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

            // Spawn as child of TrainingArea
            GameObject fruit = Instantiate(prefab, transform.parent);
            fruit.transform.localPosition = randomLocalPos;
            fruit.name = "Fruit_" + i;
            spawnedFruits[i] = fruit.transform;
        }

        // Find the ant only within this TrainingArea
        AntAgent ant = transform.parent.GetComponentInChildren<AntAgent>();
        //if (ant != null)
            //ant.fruits = spawnedFruits;
    }
}