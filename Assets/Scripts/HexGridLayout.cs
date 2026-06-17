// Single authoritative source for the hex layout (size + orientation) used to
// position everything on the board: the placed tiles, the available-cell
// markers, the hover preview, the edge highlights, the group labels and the
// quest marks.
//
// Previously every one of those systems carried its own `hexSize` field that had
// to be kept in sync by hand. If the value did not match the tile model exactly,
// tiles ended up with visible gaps (or overlaps) between them. Now the
// TilePlacementController measures the real tile once and publishes the result
// here, and all the visualizers read it back, so they can never drift apart.
public static class HexGridLayout
{
    // True once TilePlacementController has published a value this run.
    public static bool HasValue { get; private set; }

    // Hex "size" = circumradius (centre-to-corner). Defaults to the old hand-tuned
    // value so anything that runs before publication still behaves sensibly.
    public static float HexSize { get; private set; } = 0.1f;

    public static HexOrientation Orientation { get; private set; } = HexOrientation.FlatTop;

    public static void Publish(float hexSize, HexOrientation orientation)
    {
        HexSize = hexSize;
        Orientation = orientation;
        HasValue = true;
    }

    // Convenience for consumers: use the published value when available, otherwise
    // fall back to a locally configured value.
    public static float ResolveSize(float fallback)
    {
        return HasValue ? HexSize : fallback;
    }

    public static HexOrientation ResolveOrientation(HexOrientation fallback)
    {
        return HasValue ? Orientation : fallback;
    }
}
