public class ConnectionRules
{
    public bool CanBeAdjacent(TerrainType a, TerrainType b)
    {
        if (a == TerrainType.Railroad || b == TerrainType.Railroad)
        {
            return a == TerrainType.Railroad && b == TerrainType.Railroad;
        }

        if (a == TerrainType.River || b == TerrainType.River)
        {
            return a == TerrainType.River && b == TerrainType.River;
        }

        return true;
    }

    public bool CanCreateGroup(TerrainType a, TerrainType b)
    {
        if (a == TerrainType.Plain || b == TerrainType.Plain)
        {
            return false;
        }

        return a == b;
    }
}