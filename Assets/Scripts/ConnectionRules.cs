// Rules that decide whether two edge terrains may touch, and whether they
// build a scoring group together.
//
// Adjacency:
//   - Any tile can be placed next to any tile, EXCEPT:
//       * Railroad edges may only meet Railroad edges.
//       * River edges may only meet River (or water) edges.
//   - An edge with no occupied neighbor is always allowed (open ends are fine).
//
// Groups:
//   - Plains never create groups.
//   - Rivers/railroads (and future water) create groups only with a matching type.
public class ConnectionRules
{
    public bool CanBeAdjacent(TerrainType a, TerrainType b)
    {
        // A strict type forces BOTH sides to share the same strict type.
        if (TerrainCatalog.IsStrictAdjacency(a) || TerrainCatalog.IsStrictAdjacency(b))
        {
            return a == b;
        }

        return true;
    }

    public bool CanCreateGroup(TerrainType a, TerrainType b)
    {
        if (!TerrainCatalog.CreatesGroups(a) || !TerrainCatalog.CreatesGroups(b))
        {
            return false;
        }

        return a == b;
    }

    public bool IsStrictConnectionType(TerrainType type)
    {
        return TerrainCatalog.IsStrictAdjacency(type);
    }
}
