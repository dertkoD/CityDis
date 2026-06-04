using System.Collections.Generic;

// Builds the list of terrain groups currently on the board.
//
// A group is a connected component of group-forming edges (river / railroad).
// Edges connect when:
//   - they are the same group type and live on the same tile (the line runs
//     through the tile), or
//   - they face each other across two neighboring tiles and CanCreateGroup.
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

        // Union same-type edges within a single tile (a line runs through it).
        foreach (KeyValuePair<HexCoord, PlacedTile> entry in tiles)
        {
            Dictionary<TerrainType, EdgeKey> firstByType = new();

            for (int side = 0; side < 6; side++)
            {
                EdgeKey key = new EdgeKey(entry.Key, side);

                if (!edgeTerrain.TryGetValue(key, out TerrainType terrain))
                {
                    continue;
                }

                if (firstByType.TryGetValue(terrain, out EdgeKey first))
                {
                    Union(parent, first, key);
                }
                else
                {
                    firstByType[terrain] = key;
                }
            }
        }

        // Union matching edges across neighbors.
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
