using UnityEngine;
using System.Collections.Generic;

public class FoodSpawning : MonoBehaviour
{
    [Header("Food")]
    [SerializeField] private GameObject[] foodPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int totalFoodCount = 50;

    private List<GameObject> activeFood = new List<GameObject>();
    private int foodEaten = 0;

    public static event System.Action<int, int> OnFoodCountChanged;
    public static event System.Action OnAllFoodEaten;

    private void Start()
    {
        SpawnFood();
    }

    public void SpawnFood()
    {
        foreach (var f in activeFood)
            if (f != null) Destroy(f);
        activeFood.Clear();
        foodEaten = 0;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[FoodSpawning] No spawn points assigned.");
            return;
        }

        List<Transform> shuffled = new List<Transform>(spawnPoints);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < totalFoodCount; i++)
        {
            Transform point = shuffled[i % shuffled.Count];
            GameObject prefab = foodPrefabs[Random.Range(0, foodPrefabs.Length)];
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));

            GameObject food = Instantiate(prefab, point.position + offset, Quaternion.identity, transform);
            food.tag = "Food";
            activeFood.Add(food);
        }

        OnFoodCountChanged?.Invoke(0, totalFoodCount);
    }

    public void OnFoodEaten(GameObject foodObject)
    {
        if (!activeFood.Contains(foodObject)) return;

        activeFood.Remove(foodObject);
        foodObject.SetActive(false);
        foodEaten++;

        OnFoodCountChanged?.Invoke(foodEaten, totalFoodCount);

        if (activeFood.Count == 0)
        {
            OnAllFoodEaten?.Invoke();
            GameWorld.Instance?.OnAllFoodConsumed();
        }
    }

    public int RemainingFood => activeFood.Count;
    public int EatenFood => foodEaten;
    public bool AllEaten => activeFood.Count == 0;

    public List<Vector3> GetActiveFoodPositions()
    {
        var positions = new List<Vector3>();
        foreach (var f in activeFood)
            if (f != null && f.activeInHierarchy)
                positions.Add(f.transform.position);
        return positions;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.7f);
        foreach (var p in spawnPoints)
            if (p != null) Gizmos.DrawSphere(p.position, 0.15f);
    }
#endif
}
