using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs in the 2D TowerBloxxScene. When the player finishes the mini game it
// turns the stacked blocks into scale-independent HouseBuildData and hands it
// back through CityBuildSession.
//
// When the scene was opened from the 3D map, the map's EmptyTerrainSceneLoader
// owns the transition back (unload + fade), so this script only reports the
// result. When the 2D scene is run on its own, it falls back to loading the
// map scene directly.
public class TowerBloxxBuildResultSaver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerBloxxGameController gameController;
    [SerializeField] private TowerStack towerStack;

    [Header("Scene")]
    [Tooltip("Loaded only when the 2D scene was started on its own (no active " +
             "build session coming from the 3D map).")]
    [SerializeField] private string tileSceneName = "Tile3DScene";

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
        bool startedFromMap = CityBuildSession.HasPendingTile;

        CityBuildSession.CompleteBuilding(BuildHouseData());

        if (!startedFromMap)
        {
            // Standalone test run: there is no 3D loader to bring us back.
            SceneManager.LoadScene(tileSceneName);
        }
    }

    private HouseBuildData BuildHouseData()
    {
        List<HouseBlockData> blocks = new();

        IReadOnlyList<TowerBlock> successfulBlocks = towerStack.SuccessfulBlocks;

        if (successfulBlocks.Count == 0)
        {
            return new HouseBuildData(blocks);
        }

        // Use the first (bottom) block as the reference for the whole tower so
        // the data is independent of where the tower happened to sit in world
        // space and how big a single 2D block is.
        TowerBlock baseBlock = successfulBlocks[0];

        float baseX = baseBlock.transform.position.x;
        float baseBottomY = baseBlock.BottomY;
        float unitWidth = Mathf.Max(GetBlockWidth(baseBlock), 0.0001f);
        float unitHeight = Mathf.Max(baseBlock.TopY - baseBlock.BottomY, 0.0001f);

        foreach (TowerBlock block in successfulBlocks)
        {
            float horizontalOffset = (block.transform.position.x - baseX) / unitWidth;
            float verticalOffset = (block.BottomY - baseBottomY) / unitHeight;
            float angle = Mathf.DeltaAngle(0f, block.transform.eulerAngles.z);

            blocks.Add(new HouseBlockData(horizontalOffset, verticalOffset, angle));
        }

        return new HouseBuildData(blocks);
    }

    private float GetBlockWidth(TowerBlock block)
    {
        SpriteRenderer spriteRenderer = block.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.size.x;
        }

        BoxCollider2D boxCollider = block.GetComponentInChildren<BoxCollider2D>();

        if (boxCollider != null)
        {
            return boxCollider.bounds.size.x;
        }

        return 1f;
    }
}
