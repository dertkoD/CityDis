using UnityEngine;

public class TileTerrainSlot : MonoBehaviour
{
    [SerializeField] private bool isCenter;
    [SerializeField] private HexSide side;
    [SerializeField] private Renderer targetRenderer;

    public void Apply(TileData tileData, TerrainMaterialDatabase materialDatabase)
    {
        if (tileData == null)
        {
            Debug.LogError("TileData is null.");
            return;
        }

        if (materialDatabase == null)
        {
            Debug.LogError("TerrainMaterialDatabase is null.");
            return;
        }

        if (targetRenderer == null)
        {
            Debug.LogError($"Renderer is missing on {gameObject.name}");
            return;
        }

        TerrainType terrainType = isCenter
            ? tileData.Center
            : tileData.GetSide((int)side);

        Material material = materialDatabase.GetMaterial(terrainType);

        if (material != null)
        {
            targetRenderer.sharedMaterial = material;
        }
    }

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }
}
