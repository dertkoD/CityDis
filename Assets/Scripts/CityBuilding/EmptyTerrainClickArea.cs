using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class EmptyTerrainClickArea : MonoBehaviour
{
    private static readonly List<EmptyTerrainClickArea> ActiveAreas = new();

    [SerializeField] private PlacedTile placedTileOverride;
    [SerializeField] private float planarPadding = 0.01f;

    private Collider clickCollider;
    private bool isEmptyTerrain;

    private void Awake()
    {
        clickCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (!ActiveAreas.Contains(this))
        {
            ActiveAreas.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveAreas.Remove(this);
    }

    public void SetTerrain(TerrainType terrainType)
    {
        if (clickCollider == null)
        {
            clickCollider = GetComponent<Collider>();
        }

        isEmptyTerrain = terrainType == TerrainType.Empty;
        clickCollider.enabled = isEmptyTerrain;
    }

    public void RefreshTerrainFromPlacedTile()
    {
        if (TryGetPlacedTile(out PlacedTile placedTile))
        {
            SetTerrain(placedTile.GetCenterTerrain());
            return;
        }

        SetTerrain(TerrainType.Plain);
    }

    public static void RefreshAllTerrainStates()
    {
        foreach (EmptyTerrainClickArea area in ActiveAreas)
        {
            if (area != null && area.isActiveAndEnabled)
            {
                area.RefreshTerrainFromPlacedTile();
            }
        }
    }

    public static bool TryGetEmptyAreaAtWorldPoint(Vector3 worldPoint, out EmptyTerrainClickArea emptyTerrainClickArea)
    {
        foreach (EmptyTerrainClickArea area in ActiveAreas)
        {
            if (area == null || !area.isActiveAndEnabled || !area.isEmptyTerrain)
            {
                continue;
            }

            if (area.ContainsWorldPointOnMapPlane(worldPoint))
            {
                emptyTerrainClickArea = area;
                return true;
            }
        }

        emptyTerrainClickArea = null;
        return false;
    }

    public static bool TryRaycastEmptyArea(
        Ray ray,
        float maxDistance,
        out EmptyTerrainClickArea emptyTerrainClickArea,
        out Vector3 hitPoint)
    {
        float closestDistance = maxDistance;
        emptyTerrainClickArea = null;
        hitPoint = Vector3.zero;

        foreach (EmptyTerrainClickArea area in ActiveAreas)
        {
            if (area == null ||
                !area.isActiveAndEnabled ||
                !area.isEmptyTerrain ||
                area.clickCollider == null ||
                !area.clickCollider.enabled)
            {
                continue;
            }

            if (!area.clickCollider.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                continue;
            }

            if (hit.distance <= closestDistance)
            {
                closestDistance = hit.distance;
                emptyTerrainClickArea = area;
                hitPoint = hit.point;
            }
        }

        return emptyTerrainClickArea != null;
    }

    public bool TryGetPlacedTile(out PlacedTile placedTile)
    {
        placedTile = placedTileOverride != null
            ? placedTileOverride
            : GetComponentInParent<PlacedTile>();

        return placedTile != null;
    }

    public bool ContainsWorldPointOnMapPlane(Vector3 worldPoint)
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint) - boxCollider.center;
            Vector3 halfSize = boxCollider.size * 0.5f;

            return Mathf.Abs(localPoint.x) <= halfSize.x + planarPadding &&
                   Mathf.Abs(localPoint.z) <= halfSize.z + planarPadding;
        }

        Bounds bounds = clickCollider.bounds;

        return worldPoint.x >= bounds.min.x - planarPadding &&
               worldPoint.x <= bounds.max.x + planarPadding &&
               worldPoint.z >= bounds.min.z - planarPadding &&
               worldPoint.z <= bounds.max.z + planarPadding;
    }
}
