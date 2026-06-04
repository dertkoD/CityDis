using UnityEngine;

[CreateAssetMenu(menuName = "Tiles/Terrain Material Database")]
public class TerrainMaterialDatabase : ScriptableObject
{
    [SerializeField] private TerrainMaterialEntry[] entries;

    public Material GetMaterial(TerrainType terrainType)
    {
        foreach (TerrainMaterialEntry entry in entries)
        {
            if (entry.terrainType == terrainType)
            {
                return entry.material;
            }
        }

        Debug.LogError($"No material found for terrain type: {terrainType}");
        return null;
    }
}
