using System.Collections.Generic;
using UnityEngine;

// Generates the data for a single tile.
//
// Rules (from the design):
//   - At least `minSidesMatchingCenter` of the 6 outer subsections must match
//     the tile's primary sub-type (the center), unless the center is Empty.
//   - A tile may use at most `maxTerrainTypesPerTile` distinct terrain types.
//   - Empty (the city placeholder) may ONLY appear in the center, and how often
//     it appears is controlled by `emptyCenterChance`.
//   - River / Railroad tiles are "path" tiles: the type runs through the tile
//     from one edge to another.
public class TileDataGenerator : MonoBehaviour
{
    [Header("Center")]
    [Tooltip("Chance that a tile's CENTER is Empty (future city). Empty never appears on an edge.")]
    [Range(0f, 1f)]
    [SerializeField] private float emptyCenterChance = 0.08f;

    [Header("Tile theme weights (relative)")]
    [Tooltip("Weight of plain area tiles.")]
    [SerializeField] private float plainWeight = 1f;
    [Tooltip("Weight of river path tiles.")]
    [SerializeField] private float riverWeight = 0.25f;
    [Tooltip("Weight of railroad path tiles.")]
    [SerializeField] private float railroadWeight = 0.18f;

    [Header("Rules")]
    [Tooltip("At least this many outer subsections must match the center sub-type.")]
    [SerializeField] private int minSidesMatchingCenter = 2;
    [Tooltip("Maximum number of distinct terrain types a single tile may contain.")]
    [SerializeField] private int maxTerrainTypesPerTile = 4;
    [Tooltip("Safety cap on regeneration attempts when a tile fails validation.")]
    [SerializeField] private int maxGenerationAttempts = 12;

    // Area types currently available to fill a tile body (extend this list as
    // Trees / Houses / Fields / WaterBody are added).
    private static readonly TerrainType[] AreaThemeTypes =
    {
        TerrainType.Plain
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
        bool emptyCenter = Random.value < emptyCenterChance;

        if (emptyCenter)
        {
            return BuildAreaTile(PickAreaTheme(), true);
        }

        TerrainType theme = PickThemeType();

        if (TerrainCatalog.IsPathType(theme))
        {
            return BuildPathTile(theme, false);
        }

        return BuildAreaTile(theme, false);
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

    private TileData BuildPathTile(TerrainType pathType, bool emptyCenter)
    {
        TileData data = new TileData();
        data.Center = emptyCenter ? TerrainType.Empty : pathType;

        TerrainType filler = AreaThemeTypes[0];

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

        HashSet<TerrainType> distinct = new HashSet<TerrainType>();

        if (data.Center != TerrainType.Empty)
        {
            distinct.Add(data.Center);
        }

        int matchingCenter = 0;

        for (int side = 0; side < 6; side++)
        {
            TerrainType sideType = data.GetSide(side);

            // Empty (and any center-only type) is not allowed on an edge.
            if (TerrainCatalog.IsCenterOnly(sideType))
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

        // The "at least N sides match the center" rule only applies when the
        // center is a real terrain type (Empty city centers are exempt).
        if (data.Center != TerrainType.Empty && matchingCenter < minSidesMatchingCenter)
        {
            return false;
        }

        return true;
    }

    private TerrainType PickThemeType()
    {
        float total = Mathf.Max(0f, plainWeight) + Mathf.Max(0f, riverWeight) + Mathf.Max(0f, railroadWeight);

        if (total <= 0f)
        {
            return TerrainType.Plain;
        }

        float roll = Random.value * total;

        if (roll < Mathf.Max(0f, plainWeight))
        {
            return TerrainType.Plain;
        }

        roll -= Mathf.Max(0f, plainWeight);

        if (roll < Mathf.Max(0f, riverWeight))
        {
            return TerrainType.River;
        }

        return TerrainType.Railroad;
    }

    private TerrainType PickAreaTheme()
    {
        return AreaThemeTypes[Random.Range(0, AreaThemeTypes.Length)];
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
