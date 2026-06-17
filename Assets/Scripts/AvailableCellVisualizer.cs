using System.Collections.Generic;
using UnityEngine;

public class AvailableCellVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    private readonly List<AvailableCellMarker> activeMarkers = new();

    public void RefreshMarkers(
        IEnumerable<HexCoord> availableCells,
        TilePlacementController placementController)
    {
        ClearMarkers();

        foreach (HexCoord coord in availableCells)
        {
            if (!placementController.CanPlaceCurrentTileAt(coord))
            {
                continue;
            }

            Vector3 position = HexGridMath.HexToWorld(
                coord,
                HexGridLayout.ResolveSize(hexSize),
                HexGridLayout.ResolveOrientation(orientation));

            GameObject markerObject = Instantiate(markerPrefab, markerParent);
            markerObject.transform.localPosition = position;
            markerObject.transform.localRotation = Quaternion.identity;

            AvailableCellMarker marker = markerObject.GetComponent<AvailableCellMarker>();

            if (marker == null)
            {
                // Add the AvailableCellMarker component to the marker prefab in the inspector.
                Debug.LogError(
                    $"'{markerPrefab.name}' is missing an AvailableCellMarker component. " +
                    "Add it to the marker prefab.");
                Destroy(markerObject);
                continue;
            }

            marker.Initialize(coord, placementController);

            activeMarkers.Add(marker);
        }
    }

    public void ClearMarkers()
    {
        foreach (AvailableCellMarker marker in activeMarkers)
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }

        activeMarkers.Clear();
    }
}
