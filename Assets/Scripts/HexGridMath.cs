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