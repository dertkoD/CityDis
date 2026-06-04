// Central place that describes how each TerrainType behaves.
// Adding a new TerrainType later only requires updating the helpers here.
public static class TerrainCatalog
{
    // Empty is the future "city" tile center. It must never appear on an edge.
    public static bool IsCenterOnly(TerrainType type)
    {
        return type == TerrainType.Empty;
    }

    // Strict types may only sit next to a matching strict type:
    //   - Railroad edges only connect to Railroad edges.
    //   - River edges only connect to River (and water) edges.
    public static bool IsStrictAdjacency(TerrainType type)
    {
        return type == TerrainType.River || type == TerrainType.Railroad;
    }

    // Path types run THROUGH a tile (an entry edge and an exit edge).
    public static bool IsPathType(TerrainType type)
    {
        return type == TerrainType.River || type == TerrainType.Railroad;
    }

    // Types that form scoring groups when matching edges meet.
    // Plains never form groups; Empty never appears on an edge.
    public static bool CreatesGroups(TerrainType type)
    {
        return type == TerrainType.River || type == TerrainType.Railroad;
    }

    // Area types fill the body of a tile (everything that is not a path
    // and not the center-only city placeholder).
    public static bool IsAreaType(TerrainType type)
    {
        return !IsPathType(type) && !IsCenterOnly(type);
    }
}
