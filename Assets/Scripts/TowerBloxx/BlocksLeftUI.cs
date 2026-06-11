using TMPro;
using UnityEngine;

public class BlocksLeftUI : MonoBehaviour
{
    [SerializeField] private TowerBloxxGameController gameController;
    [SerializeField] private TMP_Text blocksLeftText;

    private void OnEnable()
    {
        if (gameController) gameController.BlocksLeftChanged += UpdateBlocksLeftText;
    }

    private void OnDisable()
    {
        if (gameController) gameController.BlocksLeftChanged -= UpdateBlocksLeftText;
    }

    private void Start()
    {
        if (gameController) UpdateBlocksLeftText(gameController.BlocksLeft);
    }

    private void UpdateBlocksLeftText(int blocksLeft)
    {
        if (!blocksLeftText) return;

        blocksLeftText.text = $"Blocks left: {blocksLeft}";
    }
}