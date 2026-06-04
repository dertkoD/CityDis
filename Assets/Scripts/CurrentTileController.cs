using UnityEngine;

public class CurrentTileController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject baseTilePrefab;
    [SerializeField] private TileDataGenerator tileDataGenerator;
    [SerializeField] private Transform previewAnchor;

    [Header("Preview")]
    [SerializeField] private Vector3 previewScale = Vector3.one;

    private TileData currentTileData;
    private GameObject previewInstance;
    private int rotationSteps;

    public GameObject BaseTilePrefab => baseTilePrefab;
    public TileData CurrentTileData => currentTileData;
    public int RotationSteps => rotationSteps;
    public Quaternion CurrentRotation => Quaternion.Euler(0f, rotationSteps * 60f, 0f);

    public TileData GenerateRandomTileData()
    {
        return tileDataGenerator.GenerateTile();
    }

    public void GenerateNextTile()
    {
        currentTileData = tileDataGenerator.GenerateTile();

        rotationSteps = 0;

        RebuildPreview();
    }

    public void RotateRight()
    {
        rotationSteps++;

        if (rotationSteps > 5)
        {
            rotationSteps = 0;
        }

        UpdatePreviewRotation();
    }

    public void RotateLeft()
    {
        rotationSteps--;

        if (rotationSteps < 0)
        {
            rotationSteps = 5;
        }

        UpdatePreviewRotation();
    }

    private void RebuildPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        if (baseTilePrefab == null)
        {
            Debug.LogError("Base tile prefab is not assigned.");
            return;
        }

        if (currentTileData == null)
        {
            Debug.LogError("Current TileData is null.");
            return;
        }

        previewInstance = Instantiate(baseTilePrefab, previewAnchor);
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = CurrentRotation;
        previewInstance.transform.localScale = previewScale;

        TileObjectSetup.ApplyData(previewInstance, currentTileData);
        DisablePreviewColliders();
    }

    private void UpdatePreviewRotation()
    {
        if (previewInstance != null)
        {
            previewInstance.transform.localRotation = CurrentRotation;
        }
    }

    private void DisablePreviewColliders()
    {
        Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}