using System.Collections.Generic;
using UnityEngine;

// Dorfromantik-style edge highlight.
//
// While the player hovers an available cell (where the current tile preview is
// shown), this highlights every shared border where the current tile's side has
// the SAME terrain as the already-placed neighbor it would snap to.
//
// No lights and no per-tile prefab editing are needed: it pools a single highlight
// prefab and just positions/rotates one instance per matching border. Make the
// highlight prefab a flat glowing bar lying in its local XZ plane, extending along
// its local X axis (an unlit/emissive material reads best).
//
// Wiring:
//   1. Create one highlight prefab (a thin glowing quad / bar).
//   2. Add this component to a manager GameObject, assign the highlight prefab, a
//      parent transform, the BoardGrid and the CurrentTileController, and match
//      hexSize / orientation to the TilePlacementController.
//   3. Reference this from the PlayerInputController so it follows the hover.
public class EdgeHighlightVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Transform highlightParent;
    [SerializeField] private BoardGrid boardGrid;
    [SerializeField] private CurrentTileController tileController;

    [Header("Grid (match TilePlacementController)")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Placement")]
    [Tooltip("Height above the board where the highlight sits.")]
    [SerializeField] private float heightOffset = 0.02f;
    [Tooltip("Local scale applied to each highlight instance.")]
    [SerializeField] private Vector3 highlightScale = Vector3.one;
    [Tooltip("If on, only highlight matches that also form a scoring group " +
             "(e.g. river<->water). If off, any identical terrain highlights.")]
    [SerializeField] private bool onlyGroupMatches = false;

    private readonly List<GameObject> pool = new List<GameObject>();
    private readonly ConnectionRules connectionRules = new ConnectionRules();

    public void Hide()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].activeSelf)
            {
                pool[i].SetActive(false);
            }
        }
    }

    public void ShowForCoord(HexCoord coord)
    {
        if (highlightPrefab == null || boardGrid == null || tileController == null)
        {
            return;
        }

        if (!tileController.HasCurrentTile)
        {
            Hide();
            return;
        }

        int used = 0;
        Vector3 tileCenter = HexGridMath.HexToWorld(coord, hexSize, orientation);

        for (int side = 0; side < 6; side++)
        {
            HexCoord neighborCoord = HexGridMath.GetNeighbor(coord, side);
            PlacedTile neighbor = boardGrid.GetTile(neighborCoord);

            if (neighbor == null)
            {
                continue;
            }

            TerrainType mine = tileController.GetCurrentWorldSideTerrain(side);
            TerrainType theirs = neighbor.GetSideTerrain(HexGridMath.GetOppositeDirection(side));

            if (!IsMatch(mine, theirs))
            {
                continue;
            }

            GameObject highlight = GetOrCreate(used);

            if (highlight == null)
            {
                continue;
            }

            used++;

            Vector3 neighborCenter = HexGridMath.HexToWorld(neighborCoord, hexSize, orientation);
            PlaceHighlight(highlight, tileCenter, neighborCenter);
        }

        for (int i = used; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].activeSelf)
            {
                pool[i].SetActive(false);
            }
        }
    }

    private bool IsMatch(TerrainType mine, TerrainType theirs)
    {
        if (onlyGroupMatches)
        {
            return connectionRules.CanCreateGroup(mine, theirs);
        }

        return mine == theirs;
    }

    private void PlaceHighlight(GameObject highlight, Vector3 tileCenter, Vector3 neighborCenter)
    {
        Vector3 midpoint = (tileCenter + neighborCenter) * 0.5f;
        midpoint.y += heightOffset;

        Vector3 direction = neighborCenter - tileCenter;
        direction.y = 0f;

        Transform t = highlight.transform;
        t.localPosition = midpoint;
        t.localRotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
        t.localScale = highlightScale;

        if (!highlight.activeSelf)
        {
            highlight.SetActive(true);
        }
    }

    private GameObject GetOrCreate(int index)
    {
        if (index < pool.Count)
        {
            return pool[index];
        }

        Transform parent = highlightParent != null ? highlightParent : transform;
        GameObject instance = Instantiate(highlightPrefab, parent);
        instance.name = $"Edge Highlight {index}";

        DisableColliders(instance);

        pool.Add(instance);
        return instance;
    }

    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
