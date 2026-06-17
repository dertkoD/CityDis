using UnityEngine;

public class PlacedTile : MonoBehaviour
{
    public HexCoord Coord { get; private set; }

    private TileDefinition tileDefinition;
    private int rotationSteps;

    public void Initialize(HexCoord coord, int rotationSteps)
    {
        Coord = coord;
        this.rotationSteps = NormalizeRotationSteps(rotationSteps);

        tileDefinition = GetComponent<TileDefinition>();

        if (tileDefinition == null)
        {
            Debug.LogError($"TileDefinition is missing on {gameObject.name}");
        }

        gameObject.name = $"Tile {coord}";
    }

    public TerrainType GetSideTerrain(int worldSide)
    {
        if (tileDefinition == null)
        {
            return TerrainType.Empty;
        }

        // The mesh side anchors run counter-clockwise with the index, while the
        // visual is rotated clockwise by rotationSteps * 60 degrees. Combined,
        // the terrain shown on world side w comes from local side (w + steps).
        int localSide = Mod(worldSide + rotationSteps, 6);
        return tileDefinition.GetLocalSide(localSide);
    }

    // The tile center is rotation-invariant, so it does not depend on rotationSteps.
    public TerrainType GetCenterTerrain()
    {
        if (tileDefinition == null)
        {
            return TerrainType.Empty;
        }

        return tileDefinition.GetCenter();
    }

    // Changes the center terrain at runtime. Used to turn an Empty tile into a
    // City tile once its house has been built, so it cannot be rebuilt.
    public void SetCenterTerrain(TerrainType terrainType)
    {
        if (tileDefinition != null && tileDefinition.Data != null)
        {
            tileDefinition.Data.Center = terrainType;
        }
    }

    private int NormalizeRotationSteps(int steps)
    {
        return Mod(steps, 6);
    }

    private int Mod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}
