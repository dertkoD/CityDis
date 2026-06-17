using UnityEngine;

// Runs in the 3D Tile3DScene. Takes the scale-independent HouseBuildData that
// was produced by the 2D mini game and rebuilds the very same house (with the
// same crookedness) out of 3D blocks on top of the chosen tile.
//
// The block size below is expressed in REAL WORLD UNITS, not in tile-local
// units. The builder neutralizes the tile's own scale, so the numbers you type
// here are exactly how big the blocks will be in the scene, no matter how the
// tile prefab is scaled.
public class House3DBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardGrid boardGrid;
    [SerializeField] private GameObject block3DPrefab;

    [Tooltip("Optional. Parent for the spawned houses. If left empty the house " +
             "is parented to the tile it belongs to.")]
    [SerializeField] private Transform houseParent;

    [Header("Placement (world units)")]
    [Tooltip("World-space offset of the house base from the tile origin. Raise Y " +
             "so the bottom block sits on the tile surface.")]
    [SerializeField] private Vector3 worldBaseOffset = Vector3.zero;

    [Tooltip("Extra world-space rotation of the whole house, e.g. to face the camera.")]
    [SerializeField] private Vector3 houseRotationEuler = Vector3.zero;

    [Header("Block size (world units)")]
    [SerializeField] private float blockWidth = 0.025f;
    [SerializeField] private float blockHeight = 0.025f;
    [SerializeField] private float blockDepth = 0.025f;

    [Tooltip("Apply the tilt angle recorded in the 2D scene. Tower Bloxx places " +
             "blocks upright, so this is usually off and the crookedness comes " +
             "from the horizontal offset only.")]
    [SerializeField] private bool applyAngle = false;

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
        // Covers the case where the map scene was fully reloaded (not additive):
        // the completed house is waiting in the session and is consumed here.
        if (CityBuildSession.TryConsumeCompletedHouse(out HexCoord tileCoord, out HouseBuildData houseBuildData))
        {
            TryBuild(tileCoord, houseBuildData);
        }
    }

    // Covers the additive flow: this scene stayed alive while the 2D scene was
    // loaded on top, so we get the event directly.
    private void BuildCompletedHouse(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        if (TryBuild(tileCoord, houseBuildData))
        {
            CityBuildSession.TryConsumeCompletedHouse(out _, out _);
        }
    }

    private bool TryBuild(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        if (houseBuildData == null)
        {
            return false;
        }

        if (boardGrid == null)
        {
            Debug.LogError("House3DBuilder: BoardGrid is not assigned.", this);
            return false;
        }

        if (block3DPrefab == null)
        {
            Debug.LogError("House3DBuilder: 3D block prefab is not assigned.", this);
            return false;
        }

        PlacedTile placedTile = boardGrid.GetTile(tileCoord);

        if (placedTile == null)
        {
            Debug.LogWarning($"House3DBuilder: tile {tileCoord} was not found, cannot build house.", this);
            return false;
        }

        BuildHouse(placedTile.transform, houseBuildData);

        // Turn the Empty tile into a City tile so the house can only be built
        // once and clicking it again will not reopen the mini game.
        placedTile.SetCenterTerrain(TerrainType.City);

        return true;
    }

    private void BuildHouse(Transform tileTransform, HouseBuildData houseBuildData)
    {
        Transform parent = houseParent != null ? houseParent : tileTransform;

        Transform houseRoot = new GameObject("Built House").transform;
        houseRoot.SetParent(parent, true);

        // Cancel out the parent's scale so block sizes/positions stay in real
        // world units regardless of how the tile prefab is scaled.
        Vector3 parentLossy = parent.lossyScale;
        houseRoot.localScale = new Vector3(
            SafeInverse(parentLossy.x),
            SafeInverse(parentLossy.y),
            SafeInverse(parentLossy.z));

        houseRoot.position = tileTransform.position + worldBaseOffset;
        houseRoot.rotation = Quaternion.Euler(houseRotationEuler);

        foreach (HouseBlockData blockData in houseBuildData.Blocks)
        {
            GameObject blockObject = Instantiate(block3DPrefab, houseRoot);

            float centerX = blockData.HorizontalOffset * blockWidth;
            float centerY = blockData.VerticalOffset * blockHeight + blockHeight * 0.5f;

            blockObject.transform.localPosition = new Vector3(centerX, centerY, 0f);
            blockObject.transform.localRotation = applyAngle
                ? Quaternion.Euler(0f, 0f, blockData.Angle)
                : Quaternion.identity;
            blockObject.transform.localScale = new Vector3(blockWidth, blockHeight, blockDepth);
        }
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > Mathf.Epsilon ? 1f / value : 1f;
    }
}
