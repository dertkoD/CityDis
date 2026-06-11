using TMPro;
using UnityEngine;

public class DropFeedbackUI : MonoBehaviour
{
    [SerializeField] private TowerBloxxGameController gameController;
    [SerializeField] private TMP_Text feedbackText;

    private void OnEnable()
    {
        if (gameController) gameController.DropFeedbackChanged += UpdateFeedbackText;
    }

    private void OnDisable()
    {
        if (gameController) gameController.DropFeedbackChanged -= UpdateFeedbackText;
    }

    private void Start()
    {
        UpdateFeedbackText(string.Empty);
    }

    private void UpdateFeedbackText(string feedback)
    {
        if (!feedbackText) return;

        feedbackText.text = feedback;
    }
}