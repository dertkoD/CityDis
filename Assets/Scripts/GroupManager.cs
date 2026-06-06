using System.Collections.Generic;

// Builds the list of terrain groups currently on the board.
//
// A group is a connected component of group-forming subsections (edges). The
// connection model follows the design rules:
//
//   WITHIN a single tile:
//     - An outer subsection that MATCHES the center sub-tile connects to the
//       center, so every matching outer subsection joins one blob (a forest
//       that fills the tile, a river/railroad that runs through it, a lake that
//       reaches several edges).
//     - An outer subsection that does NOT match the center can only connect to a
//       physically adjacent outer subsection (side i with side i+1) of the same
//       family, never jump across the tile to a disconnected subsection.
//
//   ACROSS neighboring tiles:
//     - Two facing subsections connect when they share a group family
//       (CanCreateGroup), e.g. forest<->forest or river<->water.
//
// Groups are fully recomputed after each placement. The board is small, so this
// stays cheap and avoids subtle incremental-update bugs.
public class GroupManager
{
    private readonly ConnectionRules connectionRules;

    public GroupManager(ConnectionRules connectionRules)
    {
        this.connectionRules = connectionRules;
    }

    public List<TileGroup> BuildGroups(BoardGrid board)
    {
        Dictionary<EdgeKey, EdgeKey> parent = new();
        Dictionary<EdgeKey, TerrainType> edgeTerrain = new();

        IReadOnlyDictionary<HexCoord, PlacedTile> tiles = board.GetAllTiles();

        // Collect every group-forming edge.
        foreach (KeyValuePair<HexCoord, PlacedTile> entry in tiles)
        {
            PlacedTile tile = entry.Value;

            for (int side = 0; side < 6; side++)
            {
                TerrainType terrain = tile.GetSideTerrain(side);

                if (!TerrainCatalog.CreatesGroups(terrain))
                {
                    continue;
                }

                EdgeKey key = new EdgeKey(entry.Key, side);
                parent[key] = key;
                edgeTerrain[key] = terrain;
            }
        }

        // Connect edges WITHIN each tile.
        foreach (KeyValuePair<HexCoord, PlacedTile> entry in tiles)
        {
            HexCoord coord = entry.Key;
            PlacedTile tile = entry.Value;
            TerrainType center = tile.GetCenterTerrain();

            // (a) Every outer subsection that matches the center joins through it.
            bool hasCenterAnchor = false;
            EdgeKey centerAnchor = default;

            for (int side = 0; side < 6; side++)
            {
                EdgeKey key = new EdgeKey(coord, side);

                if (!edgeTerrain.TryGetValue(key, out TerrainType terrain))
                {
                    continue;
                }

                if (!connectionRules.CanCreateGroup(center, terrain))
                {
                    continue;
                }

                if (!hasCenterAnchor)
                {
                    hasCenterAnchor = true;
                    centerAnchor = key;
                }
                else
                {
                    Union(parent, centerAnchor, key);
                }
            }

            // (b) Physically adjacent outer subsections of the same family always
            // touch at their shared corner, even when the center does not match.
            for (int side = 0; side < 6; side++)
            {
                EdgeKey keyA = new EdgeKey(coord, side);

                if (!edgeTerrain.TryGetValue(keyA, out TerrainType terrainA))
                {
                    continue;
                }

                EdgeKey keyB = new EdgeKey(coord, (side + 1) % 6);

                if (!edgeTerrain.TryGetValue(keyB, out TerrainType terrainB))
                {
                    continue;
                }

                if (connectionRules.CanCreateGroup(terrainA, terrainB))
                {
                    Union(parent, keyA, keyB);
                }
            }
        }

        // Connect matching edges ACROSS neighbors.
        foreach (KeyValuePair<EdgeKey, TerrainType> edge in edgeTerrain)
        {
            HexCoord neighborCoord = HexGridMath.GetNeighbor(edge.Key.coord, edge.Key.side);
            PlacedTile neighborTile = board.GetTile(neighborCoord);

            if (neighborTile == null)
            {
                continue;
            }

            int oppositeSide = HexGridMath.GetOppositeDirection(edge.Key.side);
            TerrainType neighborTerrain = neighborTile.GetSideTerrain(oppositeSide);

            if (!connectionRules.CanCreateGroup(edge.Value, neighborTerrain))
            {
                continue;
            }

            EdgeKey neighborKey = new EdgeKey(neighborCoord, oppositeSide);

            if (parent.ContainsKey(neighborKey))
            {
                Union(parent, edge.Key, neighborKey);
            }
        }

        // Assemble components.
        Dictionary<EdgeKey, TileGroup> groupsByRoot = new();
        int counter = 0;

        foreach (KeyValuePair<EdgeKey, TerrainType> edge in edgeTerrain)
        {
            EdgeKey root = Find(parent, edge.Key);

            if (!groupsByRoot.TryGetValue(root, out TileGroup group))
            {
                group = new TileGroup($"group_{counter++}", edge.Value);
                groupsByRoot[root] = group;
            }

            group.AddSection(edge.Key);

            HexCoord neighborCoord = HexGridMath.GetNeighbor(edge.Key.coord, edge.Key.side);

            if (!board.IsOccupied(neighborCoord))
            {
                group.OpenEnds++;
            }
        }

        return new List<TileGroup>(groupsByRoot.Values);
    }

    private EdgeKey Find(Dictionary<EdgeKey, EdgeKey> parent, EdgeKey key)
    {
        EdgeKey root = key;

        while (!parent[root].Equals(root))
        {
            root = parent[root];
        }

        // Path compression.
        EdgeKey current = key;
        while (!parent[current].Equals(root))
        {
            EdgeKey next = parent[current];
            parent[current] = root;
            current = next;
        }

        return root;
    }

    private void Union(Dictionary<EdgeKey, EdgeKey> parent, EdgeKey a, EdgeKey b)
    {
        EdgeKey rootA = Find(parent, a);
        EdgeKey rootB = Find(parent, b);

        if (!rootA.Equals(rootB))
        {
            parent[rootA] = rootB;
        }
    }
}
