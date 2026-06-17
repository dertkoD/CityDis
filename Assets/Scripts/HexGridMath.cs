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
    // The result is expressed in the local space of `referenceParent` (the same
    // space HexToWorld outputs into, i.e. the tileParent), so any scale on that
    // parent is divided out. Pass the instance (not the prefab asset) so the real
    // transforms are measured.
    public static bool TryMeasureHexSize(
        GameObject tileInstance,
        Transform referenceParent,
        out float hexSize)
    {
        hexSize = 0f;

        if (tileInstance == null)
        {
            return false;
        }

        // Preferred: use the tile's side anchors (TileTerrainSlot). They are placed
        // at the edge midpoints, i.e. exactly where two neighbouring tiles touch, so
        // they give the spacing the tiles were authored for. This is robust against
        // the renderer bounds being inflated by sloped/extruded sides of the tile
        // body (which is what makes a bounds-based guess too big -> visible gaps).
        if (TryMeasureFromSideAnchors(tileInstance, referenceParent, out hexSize))
        {
            return true;
        }

        // Fallback: derive it from the rendered footprint. For a regular hexagon the
        // larger horizontal extent is the corner-to-corner span (2 * size).
        return TryMeasureFromRendererBounds(tileInstance, referenceParent, out hexSize);
    }

    private static bool TryMeasureFromSideAnchors(
        GameObject tileInstance,
        Transform referenceParent,
        out float hexSize)
    {
        hexSize = 0f;

        TileTerrainSlot[] slots = tileInstance.GetComponentsInChildren<TileTerrainSlot>(true);

        if (slots == null || slots.Length == 0)
        {
            return false;
        }

        Vector3 centre = Vector3.zero;
        int count = 0;

        // The shared reference frame: world positions scaled back into the parent's
        // local space, where HexToWorld lives.
        Matrix4x4 toLocal = referenceParent != null
            ? referenceParent.worldToLocalMatrix
            : Matrix4x4.identity;

        Vector3[] sides = new Vector3[6];
        bool[] hasSide = new bool[6];

        foreach (TileTerrainSlot slot in slots)
        {
            if (slot == null || slot.IsCenter)
            {
                continue;
            }

            int s = (int)slot.Side;

            if (s < 0 || s >= 6 || hasSide[s])
            {
                continue;
            }

            Vector3 local = toLocal.MultiplyPoint3x4(slot.transform.position);
            local.y = 0f;

            sides[s] = local;
            hasSide[s] = true;
            centre += local;
            count++;
        }

        if (count < 2)
        {
            return false;
        }

        centre /= count;

        // Apothem = centre-to-edge distance. For a regular hexagon apothem =
        // (sqrt(3) / 2) * size, so size = apothem * 2 / sqrt(3). Averaging over all
        // present anchors smooths out small authoring imprecision.
        float apothemSum = 0f;

        for (int s = 0; s < 6; s++)
        {
            if (hasSide[s])
            {
                apothemSum += Vector3.Distance(sides[s], centre);
            }
        }

        float apothem = apothemSum / count;

        if (apothem <= 0f)
        {
            return false;
        }

        hexSize = apothem * 2f / Mathf.Sqrt(3f);
        return true;
    }

    private static bool TryMeasureFromRendererBounds(
        GameObject tileInstance,
        Transform referenceParent,
        out float hexSize)
    {
        hexSize = 0f;

        Renderer[] renderers = tileInstance.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        bool hasBounds = false;
        Bounds bounds = default;

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

        if (!hasBounds)
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