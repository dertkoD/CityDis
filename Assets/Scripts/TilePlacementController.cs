using UnityEngine;

public class TilePlacementController : MonoBehaviour
{
    // Frame on which the most recent tile was placed. Used by other systems
    // (e.g. EmptyTerrainSceneLoader) to ignore the click that placed a tile so
    // it does not also trigger an unrelated action on the same click.
    public static int LastPlacementFrame { get; private set; } = -1;

    [Header("References")]
    [SerializeField] private BoardGrid boardGrid;
    [SerializeField] private AvailableCellManager availableCellManager;
    [SerializeField] private AvailableCellVisualizer availableCellVisualizer;
    [SerializeField] private CurrentTileController currentTileController;
    [SerializeField] private TilePlacementValidator tilePlacementValidator;

    [Tooltip("Optional. If assigned, terrain groups are recomputed after each placement.")]
    [SerializeField] private GroupTracker groupTracker;

    [Tooltip("Optional. If assigned, placements are scored.")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Start Tile")]
    [SerializeField] private GameObject startTilePrefab;

    [Header("Grid Settings")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Parents")]
    [SerializeField] private Transform tileParent;

    private void Start()
    {
        StartPrototypeGame();
    }

    private void StartPrototypeGame()
    {
        availableCellManager.Clear();

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        currentTileController.ResetDeck();

        HexCoord startCoord = new HexCoord(0, 0);

        TileData startTileData = CreatePlainTileData();

        PlaceTile(
            currentTileController.BaseTilePrefab,
            startTileData,
            startCoord,
            Quaternion.identity,
            0
        );

        if (groupTracker != null)
        {
            groupTracker.Recompute();
        }

        if (scoreManager != null)
        {
            scoreManager.OnTilePlaced(startCoord);
        }

        availableCellManager.UpdateAfterTilePlaced(startCoord, boardGrid);

        RefreshAvailableMarkers();
    }

    public bool CanPlaceCurrentTileAt(HexCoord coord)
    {
        if (!currentTileController.HasCurrentTile)
        {
            return false;
        }

        if (!availableCellManager.IsAvailable(coord))
        {
            return false;
        }

        if (boardGrid.IsOccupied(coord))
        {
            return false;
        }

        TileData tileData = currentTileController.CurrentTileData;

        if (tileData == null)
        {
            return false;
        }

        return tilePlacementValidator.CanPlaceTile(
            tileData,
            currentTileController.RotationSteps,
            coord,
            boardGrid
        );
    }

    public void TryPlaceTileAt(HexCoord coord)
    {
        if (!CanPlaceCurrentTileAt(coord))
        {
            Debug.Log($"Cannot place current tile at {coord}");
            return;
        }

        TileData tileData = currentTileController.CurrentTileData;
        int rotationSteps = currentTileController.RotationSteps;

        PlaceTile(
            currentTileController.BaseTilePrefab,
            tileData,
            coord,
            currentTileController.CurrentRotation,
            rotationSteps
        );

        if (groupTracker != null)
        {
            groupTracker.Recompute();
        }

        if (scoreManager != null)
        {
            scoreManager.OnTilePlaced(coord);
        }

        availableCellManager.UpdateAfterTilePlaced(coord, boardGrid);

        currentTileController.AdvanceToNextTile();

        RefreshAvailableMarkers();
    }

    public void RefreshAvailableMarkers()
    {
        availableCellVisualizer.RefreshMarkers(
            availableCellManager.AvailableCells,
            this
        );
    }

    // The starting tile in the middle of the board is always fully Plain
    // (center and all six sides), instead of a random tile.
    private TileData CreatePlainTileData()
    {
        TileData tileData = new TileData();
        tileData.Center = TerrainType.Plain;

        for (int side = 0; side < 6; side++)
        {
            tileData.SetSide(side, TerrainType.Plain);
        }

        return tileData;
    }

    private void PlaceTile(
        GameObject tilePrefab,
        TileData tileData,
        HexCoord coord,
        Quaternion rotation,
        int rotationSteps)
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned.");
            return;
        }

        if (tileData == null)
        {
            Debug.LogError("TileData is null.");
            return;
        }

        Vector3 position = HexGridMath.HexToWorld(coord, hexSize, orientation);

        GameObject tileObject = Instantiate(tilePrefab, tileParent);
        tileObject.transform.localPosition = position;
        tileObject.transform.localRotation = rotation;

        TileObjectSetup.ApplyData(tileObject, tileData);

        PlacedTile placedTile = tileObject.GetComponent<PlacedTile>();

        if (placedTile == null)
        {
            // Add the PlacedTile component to the tile prefab in the inspector.
            Debug.LogError(
                $"'{tilePrefab.name}' is missing a PlacedTile component. " +
                "Add it to the tile prefab.");
            Destroy(tileObject);
            return;
        }

        placedTile.Initialize(coord, rotationSteps);

        boardGrid.AddTile(coord, placedTile);

        LastPlacementFrame = Time.frameCount;
    }
}
