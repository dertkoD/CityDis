using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TowerBloxxBuildResultSaver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerBloxxGameController gameController;
    [SerializeField] private TowerStack towerStack;

    [Header("Scene")]
    [SerializeField] private string tileSceneName = "Tile3DScene";
    [SerializeField] private bool unloadCurrentSceneWhenDone = true;

    private void OnEnable()
    {
        if (gameController != null)
        {
            gameController.HouseCompleted += SaveHouseAndReturnToMap;
        }
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.HouseCompleted -= SaveHouseAndReturnToMap;
        }
    }

    private void SaveHouseAndReturnToMap()
    {
        List<HouseBlockData> blocks = new();

        foreach (TowerBlock block in towerStack.SuccessfulBlocks)
        {
            Vector2 position = block.transform.position;
            float angle = block.transform.eulerAngles.z;
            Vector2 size = GetBlockSize(block);

            blocks.Add(new HouseBlockData(position, angle, size));
        }

        bool shouldUnloadCurrentScene = unloadCurrentSceneWhenDone && CityBuildSession.HasPendingTile;

        CityBuildSession.CompleteBuilding(new HouseBuildData(blocks));

        if (shouldUnloadCurrentScene)
        {
            SceneManager.UnloadSceneAsync(gameObject.scene);
            return;
        }

        SceneManager.LoadScene(tileSceneName);
    }

    private Vector2 GetBlockSize(TowerBlock block)
    {
        SpriteRenderer spriteRenderer = block.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.size;
        }

        BoxCollider2D boxCollider = block.GetComponentInChildren<BoxCollider2D>();

        if (boxCollider != null)
        {
            return boxCollider.bounds.size;
        }

        return Vector2.one;
    }
}
