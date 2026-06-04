using System.Collections.Generic;
using UnityEngine;

public class AvailableCellManager : MonoBehaviour
{
    private readonly HashSet<HexCoord> availableCells = new();

    public IReadOnlyCollection<HexCoord> AvailableCells => availableCells;

    public void Clear()
    {
        availableCells.Clear();
    }

    public bool IsAvailable(HexCoord coord)
    {
        return availableCells.Contains(coord);
    }

    public void AddStartCell()
    {
        availableCells.Add(new HexCoord(0, 0));
    }

    public void UpdateAfterTilePlaced(HexCoord placedCoord, BoardGrid boardGrid)
    {
        availableCells.Remove(placedCoord);

        for (int direction = 0; direction < 6; direction++)
        {
            HexCoord neighborCoord = HexGridMath.GetNeighbor(placedCoord, direction);

            if (!boardGrid.IsOccupied(neighborCoord))
            {
                availableCells.Add(neighborCoord);
            }
        }
    }
}
