using System.Collections.Generic;
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    private readonly Dictionary<HexCoord, PlacedTile> placedTiles = new();

    public bool IsOccupied(HexCoord coord)
    {
        return placedTiles.ContainsKey(coord);
    }

    public void AddTile(HexCoord coord, PlacedTile tile)
    {
        if (placedTiles.ContainsKey(coord))
        {
            Debug.LogError($"Tile already exists at {coord}");
            return;
        }

        placedTiles.Add(coord, tile);
    }

    public PlacedTile GetTile(HexCoord coord)
    {
        placedTiles.TryGetValue(coord, out PlacedTile tile);
        return tile;
    }

    public IReadOnlyDictionary<HexCoord, PlacedTile> GetAllTiles()
    {
        return placedTiles;
    }
}
