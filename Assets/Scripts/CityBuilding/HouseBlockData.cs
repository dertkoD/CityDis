using System;
using UnityEngine;

// Describes a single floor of a house in a scale-independent way.
//
// All values are RELATIVE to the tower base and NORMALIZED by the size of a
// single block in the 2D Tower Bloxx scene, so the 3D scene can rebuild the
// exact same shape (including the crookedness) at any size it wants.
//
//   horizontalOffset : how far this floor sits to the side of the tower center
//                      line, measured in block-widths. 0 = perfectly centered,
//                      +0.5 = half a block to the right, etc.
//   verticalOffset   : the bottom of this floor measured from the tower base,
//                      in block-heights. The first floor is ~0, the second ~1,
//                      and so on. Gaps are preserved if the player stacked badly.
//   angle            : the Z tilt of the floor, in degrees.
[Serializable]
public struct HouseBlockData
{
    [SerializeField] private float horizontalOffset;
    [SerializeField] private float verticalOffset;
    [SerializeField] private float angle;

    public float HorizontalOffset => horizontalOffset;
    public float VerticalOffset => verticalOffset;
    public float Angle => angle;

    public HouseBlockData(float horizontalOffset, float verticalOffset, float angle)
    {
        this.horizontalOffset = horizontalOffset;
        this.verticalOffset = verticalOffset;
        this.angle = angle;
    }
}
