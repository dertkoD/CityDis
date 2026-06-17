using UnityEngine;

public class House3DBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardGrid boardGrid;
    [SerializeField] private GameObject block3DPrefab;

    [Header("Placement")]
    [SerializeField] private Vector3 localOffsetOnTile = Vector3.zero;
    [SerializeField] private float horizontalScale = 0.1f;
    [SerializeField] private float verticalScale = 0.1f;
    [SerializeField] private float depth = 0.15f;
    [SerializeField] private bool use2DRotation = false;

    private void OnEnable()
    {
        CityBuildSession.HouseCompleted += BuildCompletedHouse;
    }

    private void OnDisable()
    {
        CityBuildSession.HouseCompleted -= BuildCompletedHouse;
    }

    private void Start()
    {
        BuildCompletedHouseIfNeeded();
    }

    private void BuildCompletedHouseIfNeeded()
    {
        if (!CityBuildSession.TryConsumeCompletedHouse(out HexCoord tileCoord, out HouseBuildData houseBuildData))
        {
            return;
        }

        PlacedTile placedTile = boardGrid.GetTile(tileCoord);

        if (placedTile == null)
        {
            Debug.LogWarning($"Could not build house. Tile {tileCoord} was not found.");
            return;
        }

        BuildHouse(placedTile.transform, houseBuildData);
    }

    private void BuildCompletedHouse(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        PlacedTile placedTile = boardGrid.GetTile(tileCoord);

        if (placedTile == null)
        {
            Debug.LogWarning($"Could not build house. Tile {tileCoord} was not found.");
            return;
        }

        BuildHouse(placedTile.transform, houseBuildData);
        CityBuildSession.TryConsumeCompletedHouse(out _, out _);
    }

    private void BuildHouse(Transform tileTransform, HouseBuildData houseBuildData)
    {
        if (block3DPrefab == null)
        {
            Debug.LogError("3D block prefab is not assigned.");
            return;
        }

        Transform houseRoot = new GameObject("Built House").transform;
        houseRoot.SetParent(tileTransform, false);
        houseRoot.localPosition = localOffsetOnTile;
        houseRoot.localRotation = Quaternion.identity;

        foreach (HouseBlockData blockData in houseBuildData.Blocks)
        {
            GameObject blockObject = Instantiate(block3DPrefab, houseRoot);
            blockObject.transform.localPosition = Convert2DPositionTo3D(blockData.Position);
            blockObject.transform.localRotation = use2DRotation
                ? Quaternion.Euler(0f, 0f, blockData.Angle)
                : Quaternion.identity;
            blockObject.transform.localScale = Convert2DSizeTo3D(blockData.Size);
        }
    }

    private Vector3 Convert2DPositionTo3D(Vector2 position)
    {
        return new Vector3(
            position.x * horizontalScale,
            position.y * verticalScale,
            0f
        );
    }

    private Vector3 Convert2DSizeTo3D(Vector2 size)
    {
        return new Vector3(
            size.x * horizontalScale,
            size.y * verticalScale,
            depth
        );
    }
}
