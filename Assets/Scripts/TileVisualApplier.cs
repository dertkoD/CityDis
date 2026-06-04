using UnityEngine;

public class TileVisualApplier : MonoBehaviour
{
    [SerializeField] private TerrainMaterialDatabase materialDatabase;
    [SerializeField] private TileTerrainSlot[] terrainSlots;

    public void Apply(TileData tileData)
    {
        if (tileData == null)
        {
            Debug.LogError("TileData is null.");
            return;
        }

        foreach (TileTerrainSlot slot in terrainSlots)
        {
            if (slot != null)
            {
                slot.Apply(tileData, materialDatabase);
            }
        }
    }

    private void Reset()
    {
        terrainSlots = GetComponentsInChildren<TileTerrainSlot>();
    }
}
