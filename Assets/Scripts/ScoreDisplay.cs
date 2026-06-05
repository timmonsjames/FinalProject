using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    private AntAgent antAgent;
    private TextMeshPro scoreText;

    void Start()
    {
        scoreText = GetComponent<TextMeshPro>();
        antAgent = transform.parent.GetComponentInChildren<AntAgent>();

        if (antAgent == null)
            Debug.LogError("ScoreDisplay: AntAgent not found in parent!");
        if (scoreText == null)
            Debug.LogError("ScoreDisplay: TextMeshPro not found!");
    }

    void Update()
    {
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - Camera.main.transform.position
            );
        }

        if (antAgent != null && scoreText != null)
        {
            scoreText.text = "Fruits: " + antAgent.fruitsCollected +
                             "\nReward: " + antAgent.totalReward.ToString("F2") +
                             "\nEpisode: " + antAgent.episodeCount;
        }
    }
}