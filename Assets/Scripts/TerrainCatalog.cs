// Central place that describes how each TerrainType behaves.
// Adding a new TerrainType later only requires updating the helpers here.
//
// Terrain "kinds":
//   - Area-fill  : Plain, Forest, Mountains. Fill the body of a tile.
//   - Path       : River, Railroad. Run THROUGH a tile (entry edge -> exit edge).
//   - Water       : WaterBody. A lake that sits in the CENTER and reaches 1-2 edges.
//   - Center-only : Empty (future city) and City (already built). Never on an edge,
//                   and never produced by the generator (City is placed by the
//                   player, Empty only ever appears as a tile center).
public static class TerrainCatalog
{
    // Empty/City are the city placeholders. They must never appear on an edge.
    public static bool IsCenterOnly(TerrainType type)
    {
        return type == TerrainType.Empty || type == TerrainType.City;
    }

    // The generator is allowed to produce this type. City is excluded: it only
    // exists once the player builds on an Empty center.
    public static bool IsGenerated(TerrainType type)
    {
        return type != TerrainType.City;
    }

    // May this type be painted on an outer subsection (edge)?
    public static bool CanAppearOnEdge(TerrainType type)
    {
        return !IsCenterOnly(type);
    }

    // Path types run THROUGH a tile (an entry edge and an exit edge).
    public static bool IsPathType(TerrainType type)
    {
        return type == TerrainType.River || type == TerrainType.Railroad;
    }

    // A lake that lives in the tile center and only touches 1-2 edges.
    public static bool IsWaterBody(TerrainType type)
    {
        return type == TerrainType.WaterBody;
    }

    // Area-fill types fill the whole body of a tile and follow the
    // "at least N sides match the center" rule.
    public static bool IsAreaFill(TerrainType type)
    {
        return type == TerrainType.Plain
            || type == TerrainType.Forest
            || type == TerrainType.Mountains;
    }

    // Types that form scoring groups when matching edges meet.
    // Plains never form groups; Empty/City never appear on an edge.
    public static bool CreatesGroups(TerrainType type)
    {
        return GroupFamily(type) != TerrainGroupFamily.None;
    }

    // Two edges build a group together only when they share a (non-None) family.
    // Rivers and water bodies share the Water family, everything else groups
    // only with its own type.
    public static TerrainGroupFamily GroupFamily(TerrainType type)
    {
        switch (type)
        {
            case TerrainType.River:
            case TerrainType.WaterBody:
                return TerrainGroupFamily.Water;
            case TerrainType.Railroad:
                return TerrainGroupFamily.Railroad;
            case TerrainType.Forest:
                return TerrainGroupFamily.Forest;
            case TerrainType.Mountains:
                return TerrainGroupFamily.Mountains;
            default:
                return TerrainGroupFamily.None;
        }
    }
}
