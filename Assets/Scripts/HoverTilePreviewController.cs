using UnityEngine;

public class HoverTilePreviewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CurrentTileController currentTileController;

    [Header("Grid Settings")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Preview Settings")]
    [SerializeField] private Transform previewParent;
    [SerializeField] private float previewHeight = 0.01f;

    private GameObject previewInstance;
    private TileData currentPreviewData;
    private HexCoord currentCoord;
    private bool hasPreview;

    public void ShowPreviewAt(HexCoord coord)
    {
        GameObject prefab = currentTileController.BaseTilePrefab;
        TileData tileData = currentTileController.CurrentTileData;

        if (prefab == null || tileData == null)
        {
            HidePreview();
            return;
        }

        if (previewInstance == null || currentPreviewData != tileData)
        {
            RebuildPreview(prefab, tileData);
        }

        currentCoord = coord;
        hasPreview = true;

        Vector3 position = HexGridMath.HexToWorld(
            coord,
            HexGridLayout.ResolveSize(hexSize),
            HexGridLayout.ResolveOrientation(orientation));

        previewInstance.transform.localRotation = currentTileController.CurrentRotation;
        previewInstance.SetActive(true);

        // Match the placement logic: centre the rendered mesh on the cell (with the
        // preview floating slightly above the board).
        HexGridMath.AlignTileVisualToCell(previewInstance, position, previewHeight);
    }

    public void RefreshRotation()
    {
        if (previewInstance == null || !hasPreview)
        {
            return;
        }

        previewInstance.transform.localRotation = currentTileController.CurrentRotation;
    }

    public void HidePreview()
    {
        hasPreview = false;

        if (previewInstance != null)
        {
            previewInstance.SetActive(false);
        }
    }

    private void RebuildPreview(GameObject prefab, TileData tileData)
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        Transform parent = previewParent != null ? previewParent : transform;

        previewInstance = Instantiate(prefab, parent);
        previewInstance.name = "Hover Tile Preview";

        currentPreviewData = tileData;

        TileObjectSetup.ApplyData(previewInstance, tileData);
        DisableColliders(previewInstance);
    }

    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
