using UnityEngine;

public class AvailableCellMarker : MonoBehaviour
{
    private HexCoord coord;
    private TilePlacementController placementController;

    public HexCoord Coord => coord;

    public void Initialize(HexCoord coord, TilePlacementController placementController)
    {
        this.coord = coord;
        this.placementController = placementController;

        gameObject.name = $"Available Cell {coord}";
    }

    public void PlaceTileHere()
    {
        placementController.TryPlaceTileAt(coord);
    }
}
