using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        if (scoreCounter) scoreCounter.ScoreChanged += UpdateScoreText;
    }

    private void OnDisable()
    {
        if (scoreCounter) scoreCounter.ScoreChanged -= UpdateScoreText;
    }

    private void Start()
    {
        UpdateScoreText(scoreCounter != null ? scoreCounter.Score : 0);
    }

    private void UpdateScoreText(int score)
    {
        if (!scoreText) return;

        scoreText.text = $"Score: {score}";
    }
}