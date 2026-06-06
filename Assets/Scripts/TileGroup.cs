using System;
using System.Collections.Generic;
using UnityEngine;

// Identifies one edge ("section") of a placed tile: the tile coordinate plus
// the world-facing side index (0..5).
[Serializable]
public struct EdgeKey : IEquatable<EdgeKey>
{
    public HexCoord coord;
    public int side;

    public EdgeKey(HexCoord coord, int side)
    {
        this.coord = coord;
        this.side = side;
    }

    public bool Equals(EdgeKey other)
    {
        return coord.Equals(other.coord) && side == other.side;
    }

    public override bool Equals(object obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(coord.GetHashCode(), side);
    }
}

// A connected component of matching group-forming edges (e.g. a single river or
// railroad line) spread across one or more tiles.
public class TileGroup
{
    public string Id { get; }
    public TerrainType TerrainType { get; }

    // The scoring family this group belongs to (e.g. river + water bodies share
    // the Water family).
    public TerrainGroupFamily Family => TerrainCatalog.GroupFamily(TerrainType);

    private readonly HashSet<EdgeKey> sections = new();
    private readonly HashSet<HexCoord> tiles = new();

    // Group edges that face an empty (unplaced) cell. A group is "closed" when
    // it has no open ends left.
    public int OpenEnds { get; set; }

    public IReadOnlyCollection<EdgeKey> Sections => sections;
    public IReadOnlyCollection<HexCoord> Tiles => tiles;

    public TileGroup(string id, TerrainType terrainType)
    {
        Id = id;
        TerrainType = terrainType;
    }

    public void AddSection(EdgeKey section)
    {
        sections.Add(section);
        tiles.Add(section.coord);
    }

    public void MergeWith(TileGroup other)
    {
        foreach (EdgeKey section in other.sections)
        {
            AddSection(section);
        }

        OpenEnds += other.OpenEnds;
    }

    public int GetSize()
    {
        return tiles.Count;
    }

    public bool IsClosed => OpenEnds == 0 && sections.Count > 0;

    // Average board-layout position of the tiles in this group, used to anchor a
    // group-size label. Uses the same HexToWorld layout that places the tiles, so
    // the result is in the board parent's LOCAL space (y = 0 on the board plane).
    public Vector3 GetLayoutCentroid(float hexSize, HexOrientation orientation)
    {
        if (tiles.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;

        foreach (HexCoord tile in tiles)
        {
            sum += HexGridMath.HexToWorld(tile, hexSize, orientation);
        }

        return sum / tiles.Count;
    }

    // Stable signature used to award a closed group's bonus only once.
    public string GetSignature()
    {
        List<long> encoded = new List<long>(sections.Count);

        foreach (EdgeKey section in sections)
        {
            long packed = (((long)section.coord.q & 0xFFFFFF) << 32)
                          ^ (((long)section.coord.r & 0xFFFFFF) << 8)
                          ^ (long)section.side;
            encoded.Add(packed);
        }

        encoded.Sort();
        return $"{TerrainType}:{string.Join(",", encoded)}";
    }
}
