using TMPro;
using UnityEngine;

public class HouseCompletedUI : MonoBehaviour
{
    [SerializeField] private TowerBloxxGameController gameController;
    [SerializeField] private TMP_Text completionText;

    private void OnEnable()
    {
        if (gameController) gameController.HouseCompleted += ShowCompletionText;
    }

    private void OnDisable()
    {
        if (gameController) gameController.HouseCompleted -= ShowCompletionText;
    }

    private void Start()
    {
        if (completionText) completionText.text = string.Empty;
    }

    private void ShowCompletionText()
    {
        if (!completionText) return;

        completionText.text = "House completed";
    }
}