using UnityEngine;

public static class HexGridMath
{
     /*
     Direction order:
     
              2     1
            /         \
          3             0
            \         /
              4     5
     
     0 = Right
     1 = TopRight
     2 = TopLeft
     3 = Left
     4 = BottomLeft
     5 = BottomRight
    */

    public static readonly HexCoord[] Directions =
    {
        new HexCoord(1, 0),    // Right
        new HexCoord(1, -1),   // TopRight
        new HexCoord(0, -1),   // TopLeft
        new HexCoord(-1, 0),   // Left
        new HexCoord(-1, 1),   // BottomLeft
        new HexCoord(0, 1),    // BottomRight
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