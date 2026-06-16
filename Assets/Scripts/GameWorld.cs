using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class GameWorld : MonoBehaviour
{
    public static GameWorld Instance { get; private set; }

    [Header("Ants")]
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private int antCount = 10;
    [SerializeField] private Transform[] antSpawnPoints;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Timer")]
    [SerializeField] private float matchDuration = 300f;

    private List<GameObject> activeAnts = new List<GameObject>();
    private int antsKilled = 0;
    private float timeRemaining;
    private bool matchActive = false;

    public static event System.Action<int, int> OnAntCountChanged;
    public static event System.Action<float> OnTimerUpdated;
    public static event System.Action OnPlayerWin;
    public static event System.Action OnPlayerLose;
    public static event System.Action OnTrackerFired;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartMatch();
    }

    private void Update()
    {
        if (!matchActive) return;

        timeRemaining -= Time.deltaTime;
        OnTimerUpdated?.Invoke(Mathf.Max(0f, timeRemaining));

        if (timeRemaining <= 0f)
            EndMatch(playerWon: false);
    }

    private void StartMatch()
    {
        antsKilled = 0;
        timeRemaining = matchDuration;
        activeAnts.Clear();

        SpawnPlayer();
        SpawnAnts();

        matchActive = true;
    }

    private void SpawnPlayer()
    {
        if (playerPrefab != null && playerSpawnPoint != null)
            Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
    }

    private void SpawnAnts()
    {
        if (antSpawnPoints == null || antSpawnPoints.Length == 0)
        {
            Debug.LogError("GameWorld: No ant spawn points assigned in Inspector!");
            return;
        }

        if (antPrefab == null)
        {
            Debug.LogError("GameWorld: No ant prefab assigned in Inspector!");
            return;
        }

        List<Transform> shuffled = new List<Transform>(antSpawnPoints);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < antCount; i++)
        {
            Transform point = shuffled[i % shuffled.Count];
            GameObject ant = Instantiate(antPrefab, point.position, Quaternion.identity);
            activeAnts.Add(ant);
        }

        OnAntCountChanged?.Invoke(antsKilled, antCount);
    }

    public void OnAntKilled()
    {
        antsKilled++;
        OnAntCountChanged?.Invoke(antsKilled, antCount);

        if (antsKilled >= antCount)
            EndMatch(playerWon: true);
    }

    public void OnAllFoodConsumed()
    {
        EndMatch(playerWon: false);
    }

    public void OnTrackerActivated()
    {
        OnTrackerFired?.Invoke();
    }

    private void EndMatch(bool playerWon)
    {
        if (!matchActive) return;
        matchActive = false;

        if (playerWon)
            OnPlayerWin?.Invoke();
        else
            OnPlayerLose?.Invoke();
    }

    public float TimeRemaining => timeRemaining;
    public int AntsKilled => antsKilled;
    public int AntsTotal => antCount;
    public bool MatchActive => matchActive;
}