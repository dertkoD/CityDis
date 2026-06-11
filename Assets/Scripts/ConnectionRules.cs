// Rules that decide whether two edge terrains may touch, and whether they
// build a scoring group together.
//
// Adjacency (which edges may physically meet):
//   - Any tile can be placed next to any tile, EXCEPT:
//       * Railroad edges may ONLY meet Railroad edges.
//       * River edges may ONLY meet River or WaterBody edges.
//   - An edge with no occupied neighbor is always allowed (open ends are fine).
//
// Groups (which matching edges score together):
//   - Plains never create groups.
//   - Rivers and water bodies create one shared "water" group.
//   - Every other group-forming type (railroad, forest, mountains) groups only
//     with its own type.
public class ConnectionRules
{
    public bool CanBeAdjacent(TerrainType a, TerrainType b)
    {
        // Railroad is the strictest: a railroad edge can only ever meet another
        // railroad edge.
        if (a == TerrainType.Railroad || b == TerrainType.Railroad)
        {
            return a == TerrainType.Railroad && b == TerrainType.Railroad;
        }

        // A river edge may meet a river or a water body (a river running into a
        // lake), but nothing else.
        if (a == TerrainType.River)
        {
            return b == TerrainType.River || b == TerrainType.WaterBody;
        }

        if (b == TerrainType.River)
        {
            return a == TerrainType.River || a == TerrainType.WaterBody;
        }

        // Everything else (plains, forest, mountains, water bodies, city) may sit
        // next to anything.
        return true;
    }

    public bool CanCreateGroup(TerrainType a, TerrainType b)
    {
        TerrainGroupFamily familyA = TerrainCatalog.GroupFamily(a);
        TerrainGroupFamily familyB = TerrainCatalog.GroupFamily(b);

        if (familyA == TerrainGroupFamily.None || familyB == TerrainGroupFamily.None)
        {
            return false;
        }

        return familyA == familyB;
    }
}
