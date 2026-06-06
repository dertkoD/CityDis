using System.Collections.Generic;
using UnityEngine;

// Generates the data for a single tile.
//
// A tile is built from one of several "themes":
//   - Area  (Plain / Forest / Mountains): fills the body. At least
//     `minSidesMatchingCenter` of the 6 outer subsections match the center.
//   - Path  (River / Railroad): the type runs THROUGH the tile from one edge to
//     another, the rest of the tile is plain filler.
//   - Water (WaterBody): a lake in the CENTER that reaches only 1-2 adjacent
//     edges, the rest of the tile is plain filler.
//
// Extra rules:
//   - A tile may use at most `maxTerrainTypesPerTile` distinct terrain types.
//   - Empty (the city placeholder) may ONLY appear in the center; how often it
//     appears is controlled by `emptyCenterChance`.
//   - City is never generated (it only exists once the player builds an Empty).
public class TileDataGenerator : MonoBehaviour
{
    [Header("Center")]
    [Tooltip("Chance that an AREA tile's CENTER is Empty (future city). Empty never appears on an edge.")]
    [Range(0f, 1f)]
    [SerializeField] private float emptyCenterChance = 0.08f;

    [Header("Tile theme weights (relative)")]
    [Tooltip("Weight of plain area tiles.")]
    [SerializeField] private float plainWeight = 1f;
    [Tooltip("Weight of forest area tiles.")]
    [SerializeField] private float forestWeight = 0.6f;
    [Tooltip("Weight of mountain area tiles.")]
    [SerializeField] private float mountainsWeight = 0.4f;
    [Tooltip("Weight of river path tiles.")]
    [SerializeField] private float riverWeight = 0.25f;
    [Tooltip("Weight of railroad path tiles.")]
    [SerializeField] private float railroadWeight = 0.18f;
    [Tooltip("Weight of water body (lake) tiles.")]
    [SerializeField] private float waterWeight = 0.2f;

    [Header("Area rules")]
    [Tooltip("At least this many outer subsections must match the center sub-type.")]
    [SerializeField] private int minSidesMatchingCenter = 2;
    [Tooltip("Maximum number of distinct terrain types a single tile may contain.")]
    [SerializeField] private int maxTerrainTypesPerTile = 4;

    [Header("Water body rules")]
    [Tooltip("How many edges the central lake reaches (it picks a value in this range).")]
    [SerializeField] private int waterMinSides = 1;
    [SerializeField] private int waterMaxSides = 2;

    [Header("Safety")]
    [Tooltip("Safety cap on regeneration attempts when a tile fails validation.")]
    [SerializeField] private int maxGenerationAttempts = 12;

    // Area types available to fill / mix into the body of an area tile.
    private static readonly TerrainType[] AreaThemeTypes =
    {
        TerrainType.Plain,
        TerrainType.Forest,
        TerrainType.Mountains
    };

    public TileData GenerateTile()
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxGenerationAttempts); attempt++)
        {
            TileData data = BuildTile();

            if (ValidateGeneratedTile(data))
            {
                return data;
            }
        }

        // Fallback: a guaranteed-valid all-plain tile.
        return BuildAreaTile(TerrainType.Plain, false);
    }

    private TileData BuildTile()
    {
        TerrainType theme = PickThemeType();

        if (TerrainCatalog.IsPathType(theme))
        {
            return BuildPathTile(theme);
        }

        if (TerrainCatalog.IsWaterBody(theme))
        {
            return BuildWaterTile();
        }

        bool emptyCenter = Random.value < emptyCenterChance;
        return BuildAreaTile(theme, emptyCenter);
    }

    private TileData BuildAreaTile(TerrainType areaTheme, bool emptyCenter)
    {
        TileData data = new TileData();
        data.Center = emptyCenter ? TerrainType.Empty : areaTheme;

        List<TerrainType> pool = BuildAreaTypePool(areaTheme);

        // Guarantee the minimum number of sides that match the primary sub-type.
        int matchingTarget = Mathf.Clamp(
            Random.Range(minSidesMatchingCenter, 7),
            Mathf.Min(minSidesMatchingCenter, 6),
            6
        );

        List<int> sideOrder = ShuffledSideOrder();

        for (int i = 0; i < sideOrder.Count; i++)
        {
            int side = sideOrder[i];

            if (i < matchingTarget)
            {
                data.SetSide(side, areaTheme);
            }
            else
            {
                data.SetSide(side, pool[Random.Range(0, pool.Count)]);
            }
        }

        return data;
    }

    private TileData BuildPathTile(TerrainType pathType)
    {
        TileData data = new TileData();
        data.Center = pathType;

        const TerrainType filler = TerrainType.Plain;

        for (int side = 0; side < 6; side++)
        {
            data.SetSide(side, filler);
        }

        int entrySide = Random.Range(0, 6);
        int exitSide = ChoosePathExitSide(entrySide);

        data.SetSide(entrySide, pathType);
        data.SetSide(exitSide, pathType);

        return data;
    }

    private TileData BuildWaterTile()
    {
        TileData data = new TileData();
        data.Center = TerrainType.WaterBody;

        const TerrainType filler = TerrainType.Plain;

        for (int side = 0; side < 6; side++)
        {
            data.SetSide(side, filler);
        }

        int low = Mathf.Clamp(Mathf.Min(waterMinSides, waterMaxSides), 1, 6);
        int high = Mathf.Clamp(Mathf.Max(waterMinSides, waterMaxSides), 1, 6);
        int waterSides = Random.Range(low, high + 1);

        // The shoreline is a run of CONSECUTIVE edges, so the lake reaches the
        // border in one connected stretch instead of scattered single edges.
        int startSide = Random.Range(0, 6);

        for (int i = 0; i < waterSides; i++)
        {
            data.SetSide((startSide + i) % 6, TerrainType.WaterBody);
        }

        return data;
    }

    private List<TerrainType> BuildAreaTypePool(TerrainType primary)
    {
        List<TerrainType> pool = new List<TerrainType> { primary };

        List<TerrainType> candidates = new List<TerrainType>();
        foreach (TerrainType type in AreaThemeTypes)
        {
            if (type != primary)
            {
                candidates.Add(type);
            }
        }

        // Reserve one slot for the center when it is a real (non-Empty) type.
        while (pool.Count < maxTerrainTypesPerTile && candidates.Count > 0)
        {
            if (Random.value > 0.5f)
            {
                break;
            }

            int index = Random.Range(0, candidates.Count);
            pool.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return pool;
    }

    public bool ValidateGeneratedTile(TileData data)
    {
        if (data == null)
        {
            return false;
        }

        // The generator never produces City; it only appears once the player builds.
        if (!TerrainCatalog.IsGenerated(data.Center))
        {
            return false;
        }

        HashSet<TerrainType> distinct = new HashSet<TerrainType>();

        if (data.Center != TerrainType.Empty)
        {
            distinct.Add(data.Center);
        }

        int matchingCenter = 0;

        for (int side = 0; side < 6; side++)
        {
            TerrainType sideType = data.GetSide(side);

            // Center-only types (Empty / City) are never allowed on an edge.
            if (!TerrainCatalog.CanAppearOnEdge(sideType))
            {
                return false;
            }

            distinct.Add(sideType);

            if (sideType == data.Center)
            {
                matchingCenter++;
            }
        }

        if (distinct.Count > maxTerrainTypesPerTile)
        {
            return false;
        }

        // The "at least N sides match the center" rule only applies to AREA tiles
        // (path tiles run through, water tiles only touch a couple of edges, and
        // Empty city centers are exempt).
        if (TerrainCatalog.IsAreaFill(data.Center) && matchingCenter < minSidesMatchingCenter)
        {
            return false;
        }

        return true;
    }

    private TerrainType PickThemeType()
    {
        float plain = Mathf.Max(0f, plainWeight);
        float forest = Mathf.Max(0f, forestWeight);
        float mountains = Mathf.Max(0f, mountainsWeight);
        float river = Mathf.Max(0f, riverWeight);
        float railroad = Mathf.Max(0f, railroadWeight);
        float water = Mathf.Max(0f, waterWeight);

        float total = plain + forest + mountains + river + railroad + water;

        if (total <= 0f)
        {
            return TerrainType.Plain;
        }

        float roll = Random.value * total;

        if ((roll -= plain) < 0f) return TerrainType.Plain;
        if ((roll -= forest) < 0f) return TerrainType.Forest;
        if ((roll -= mountains) < 0f) return TerrainType.Mountains;
        if ((roll -= river) < 0f) return TerrainType.River;
        if ((roll -= railroad) < 0f) return TerrainType.Railroad;

        return TerrainType.WaterBody;
    }

    private List<int> ShuffledSideOrder()
    {
        List<int> order = new List<int> { 0, 1, 2, 3, 4, 5 };

        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }

    private int ChoosePathExitSide(int entrySide)
    {
        float roll = Random.value;

        if (roll < 0.6f)
        {
            return Mod(entrySide + 3, 6); // straight through
        }

        if (roll < 0.8f)
        {
            return Mod(entrySide + 2, 6); // soft turn
        }

        return Mod(entrySide + 1, 6); // sharp turn
    }

    private int Mod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}
