using UnityEngine;

public static class HexGridMath
{
    /*
     Side index -> neighbor offset. The order MUST match the physical layout of
     the tile mesh's side anchors (DefaultTile prefab), which run counter-
     clockwise with the side index:

       side 0 = Side_Right        (world ~30 deg)
       side 1 = Side_TopRight     (world ~90 deg)
       side 2 = Side_TopLeft      (world ~150 deg)
       side 3 = Side_Left         (world ~210 deg)
       side 4 = Side_BottomLeft   (world ~270 deg)
       side 5 = Side_BottomRight  (world ~330 deg)

     Each entry below is the axial offset of the neighbor that sits at the same
     world angle as the matching mesh anchor, so the terrain painted on a tile
     edge is checked against the neighbor that is physically on that edge.
    */

    public static readonly HexCoord[] Directions =
    {
        new HexCoord(1, 0),    // side 0  ~30 deg
        new HexCoord(0, 1),    // side 1  ~90 deg
        new HexCoord(-1, 1),   // side 2  ~150 deg
        new HexCoord(-1, 0),   // side 3  ~210 deg
        new HexCoord(0, -1),   // side 4  ~270 deg
        new HexCoord(1, -1),   // side 5  ~330 deg
    };

    public static HexCoord GetNeighbor(HexCoord coord, int direction)
    {
        if (direction < 0 || direction >= 6)
        {
            Debug.LogError($"Invalid hex direction: {direction}");
            return coord;
        }

        return coord + Directions[direction];
    }

    public static int GetOppositeDirection(int direction)
    {
        return (direction + 3) % 6;
    }

    public static Vector3 HexToWorld(HexCoord coord, float hexSize, HexOrientation orientation)
    {
        if (orientation == HexOrientation.PointyTop)
        {
            float x = hexSize * Mathf.Sqrt(3f) * (coord.q + coord.r * 0.5f);
            float z = hexSize * 1.5f * coord.r;

            return new Vector3(x, 0f, z);
        }
        else
        {
            float x = hexSize * 1.5f * coord.q;
            float z = hexSize * Mathf.Sqrt(3f) * (coord.r + coord.q * 0.5f);

            return new Vector3(x, 0f, z);
        }
    }

    // Measures the hex "size" (circumradius, i.e. centre-to-corner distance) of an
    // ACTUAL tile so the layout spacing matches the model exactly, instead of
    // relying on a hand-tuned number that has to be kept in sync with the mesh.
    //
    // For a regular hexagon laid flat on the XZ plane the corner-to-corner span is
    // 2 * size and the flat-to-flat span is sqrt(3) * size, so the larger of the
    // two horizontal extents is always the corner-to-corner one; halving it gives
    // the size that HexToWorld needs for neighbours to sit flush.
    //
    // The result is expressed in the local space of `referenceParent` (the same
    // space HexToWorld outputs into, i.e. the tileParent), so any scale on that
    // parent is divided out. Pass the instance (not the prefab asset) so the real
    // imported meshes and their transforms are measured.
    public static bool TryMeasureHexSize(
        GameObject tileInstance,
        Transform referenceParent,
        out float hexSize)
    {
        hexSize = 0f;

        if (!TryGetVisualBounds(tileInstance, out Bounds bounds))
        {
            return false;
        }

        Vector3 parentScale = referenceParent != null ? referenceParent.lossyScale : Vector3.one;

        float sizeX = bounds.size.x / Mathf.Max(1e-6f, Mathf.Abs(parentScale.x));
        float sizeZ = bounds.size.z / Mathf.Max(1e-6f, Mathf.Abs(parentScale.z));

        float diameter = Mathf.Max(sizeX, sizeZ);

        if (diameter <= 0f)
        {
            return false;
        }

        hexSize = diameter * 0.5f;
        return true;
    }

    // Places a freshly-instantiated tile so the CENTRE of its rendered mesh lands
    // exactly on the cell, regardless of the tile's rotation.
    //
    // This matters because the tile mesh's geometric centre is not at the prefab's
    // transform origin (the point it is rotated around). If we only set the root's
    // position, tiles with different rotations swing their visuals off in different
    // directions, which is why some neighbours touched and others left big gaps.
    // Measuring the rendered centre AFTER applying the rotation and shifting the
    // root to compensate makes every tile line up the same way.
    //
    // Call this once, after the tile has been instantiated (under its final parent),
    // its data applied and its rotation set. `cellLocalPosition` is the HexToWorld
    // result (the cell centre in the parent's local space). `extraLocalHeight` lets
    // callers float the tile above the board (e.g. the hover preview).
    public static void AlignTileVisualToCell(
        GameObject tileInstance,
        Vector3 cellLocalPosition,
        float extraLocalHeight = 0f)
    {
        if (tileInstance == null)
        {
            return;
        }

        Transform tileTransform = tileInstance.transform;
        Transform parent = tileTransform.parent;

        Vector3 targetLocal = cellLocalPosition;
        targetLocal.y += extraLocalHeight;

        // Default: position by the root (used if the tile has no renderers yet).
        tileTransform.localPosition = targetLocal;

        if (!TryGetVisualBounds(tileInstance, out Bounds bounds))
        {
            return;
        }

        Vector3 targetWorld = parent != null
            ? parent.TransformPoint(targetLocal)
            : targetLocal;

        // Shift only in the board plane so the configured height is preserved.
        Vector3 delta = targetWorld - bounds.center;
        delta.y = 0f;

        tileTransform.position += delta;
    }

    private static bool TryGetVisualBounds(GameObject tileInstance, out Bounds bounds)
    {
        bounds = default;

        if (tileInstance == null)
        {
            return false;
        }

        Renderer[] renderers = tileInstance.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public static HexCoord WorldToHex(Vector3 worldPosition, float hexSize, HexOrientation orientation)
    {
        float q;
        float r;

        if (orientation == HexOrientation.PointyTop)
        {
            q = ((Mathf.Sqrt(3f) / 3f) * worldPosition.x - (1f / 3f) * worldPosition.z) / hexSize;
            r = ((2f / 3f) * worldPosition.z) / hexSize;
        }
        else
        {
            q = ((2f / 3f) * worldPosition.x) / hexSize;
            r = (-(1f / 3f) * worldPosition.x + (Mathf.Sqrt(3f) / 3f) * worldPosition.z) / hexSize;
        }

        return RoundAxial(q, r);
    }

    private static HexCoord RoundAxial(float q, float r)
    {
        float x = q;
        float z = r;
        float y = -x - z;

        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float xDiff = Mathf.Abs(rx - x);
        float yDiff = Mathf.Abs(ry - y);
        float zDiff = Mathf.Abs(rz - z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new HexCoord(rx, rz);
    }
}