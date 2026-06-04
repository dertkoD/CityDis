using UnityEngine;

public class TilePlacementValidator : MonoBehaviour
{
    private readonly ConnectionRules connectionRules = new ConnectionRules();

    public bool CanPlaceTile(
        TileData tileData,
        int rotationSteps,
        HexCoord coord,
        BoardGrid boardGrid)
    {
        if (tileData == null)
        {
            Debug.LogError("TileData is null.");
            return false;
        }

        for (int side = 0; side < 6; side++)
        {
            HexCoord neighborCoord = HexGridMath.GetNeighbor(coord, side);
            PlacedTile neighborTile = boardGrid.GetTile(neighborCoord);

            if (neighborTile == null)
            {
                continue;
            }

            int oppositeSide = HexGridMath.GetOppositeDirection(side);

            TerrainType newTileSide = GetRotatedDataSide(tileData, side, rotationSteps);
            TerrainType neighborSide = neighborTile.GetSideTerrain(oppositeSide);

            bool compatible = connectionRules.CanBeAdjacent(newTileSide, neighborSide);

            if (!compatible)
            {
                Debug.Log(
                    $"Cannot place tile at {coord}. " +
                    $"Side {side} is {newTileSide}, " +
                    $"neighbor side {oppositeSide} is {neighborSide}."
                );

                return false;
            }
        }

        return true;
    }

    private TerrainType GetRotatedDataSide(
        TileData tileData,
        int worldSide,
        int rotationSteps)
    {
        int localSide = Mod(worldSide + rotationSteps, 6);
        return tileData.GetSide(localSide);
    }

    private int Mod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}
