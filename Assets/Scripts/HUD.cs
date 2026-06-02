using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUD : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private Text timerText;

    [Header("Ants")]
    [SerializeField] private Text antCountText;

    [Header("Food")]
    [SerializeField] private Text foodCountText;

    [Header("Utilities")]
    [SerializeField] private Text gelTrapsText;
    [SerializeField] private Text boraxText;
    [SerializeField] private Text trackerCDText;

    [Header("Tracker")]
    [SerializeField] private GameObject trackerActiveIndicator;

    [Header("End Screen")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [Header("Player")]
    [SerializeField] private PlayerControl player;

    private void OnEnable()
    {
        GameWorld.OnTimerUpdated += UpdateTimer;
        GameWorld.OnAntCountChanged += UpdateAntCount;
        GameWorld.OnTrackerFired += ShowTrackerIndicator;
        GameWorld.OnPlayerWin += ShowWinScreen;
        GameWorld.OnPlayerLose += ShowLoseScreen;
        FoodSpawning.OnFoodCountChanged += UpdateFoodCount;
    }

    private void OnDisable()
    {
        GameWorld.OnTimerUpdated -= UpdateTimer;
        GameWorld.OnAntCountChanged -= UpdateAntCount;
        GameWorld.OnTrackerFired -= ShowTrackerIndicator;
        GameWorld.OnPlayerWin -= ShowWinScreen;
        GameWorld.OnPlayerLose -= ShowLoseScreen;
        FoodSpawning.OnFoodCountChanged -= UpdateFoodCount;
    }

    private void Start()
    {
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        if (trackerActiveIndicator != null) trackerActiveIndicator.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;

        if (gelTrapsText != null) gelTrapsText.text = $"Gel Traps: {player.GelTrapsLeft}";
        if (boraxText != null) boraxText.text = $"Borax: {player.BoraxUsesLeft}";
        if (trackerCDText != null)
        {
            float cd = player.TrackerCD;
            trackerCDText.text = cd > 0f ? $"Tracker: {cd:F0}s" : "Tracker: Ready";
        }
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateAntCount(int killed, int total)
    {
        if (antCountText != null)
            antCountText.text = $"Ants: {killed}/{total}";
    }

    private void UpdateFoodCount(int eaten, int total)
    {
        if (foodCountText != null)
            foodCountText.text = $"Food: {eaten}/{total}";
    }

    private void ShowTrackerIndicator()
    {
        StartCoroutine(TrackerIndicatorRoutine());
    }

    private IEnumerator TrackerIndicatorRoutine()
    {
        if (trackerActiveIndicator != null) trackerActiveIndicator.SetActive(true);
        yield return new WaitForSeconds(8f);
        if (trackerActiveIndicator != null) trackerActiveIndicator.SetActive(false);
    }

    private void ShowWinScreen()
    {
        if (winScreen != null) winScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowLoseScreen()
    {
        if (loseScreen != null) loseScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }
}