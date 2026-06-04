using System.Collections.Generic;
using UnityEngine;

public class TileDataGenerator : MonoBehaviour
{
    [Header("Generation Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float riverTileChance = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float railroadTileChance = 0.1f;

    [Header("Rules")]
    [SerializeField] private int minSidesMatchingCenter = 2;
    [SerializeField] private int maxTerrainTypesPerTile = 4;

    private readonly TerrainType[] normalCenterTypes =
    {
        TerrainType.Plain
    };

    private readonly TerrainType[] normalSideTypes =
    {
        TerrainType.Plain
    };

    public TileData GenerateTile()
    {
        float roll = Random.value;

        if (roll < railroadTileChance)
        {
            return GeneratePathTile(TerrainType.Railroad);
        }

        if (roll < railroadTileChance + riverTileChance)
        {
            return GeneratePathTile(TerrainType.River);
        }

        return GenerateNormalTile();
    }

    private TileData GenerateNormalTile()
    {
        TileData data = new TileData();

        TerrainType centerType = GetRandom(normalCenterTypes);
        data.Center = centerType;

        for (int i = 0; i < 6; i++)
        {
            data.SetSide(i, TerrainType.Empty);
        }

        int centerBlockSize = Random.Range(minSidesMatchingCenter, 5);
        int centerStartSide = Random.Range(0, 6);

        FillCircularBlock(data, centerStartSide, centerBlockSize, centerType);

        List<TerrainType> availableTypes = BuildTypePool(centerType);

        for (int side = 0; side < 6; side++)
        {
            if (data.GetSide(side) != TerrainType.Empty)
            {
                continue;
            }

            TerrainType type = availableTypes[Random.Range(0, availableTypes.Count)];
            data.SetSide(side, type);
        }

        return data;
    }

    private TileData GeneratePathTile(TerrainType pathType)
    {
        TileData data = new TileData();

        TerrainType centerType = pathType;

        data.Center = centerType;

        for (int i = 0; i < 6; i++)
        {
            data.SetSide(i, TerrainType.Empty);
        }

        int entrySide = Random.Range(0, 6);
        int exitSide = ChoosePathExitSide(entrySide);

        data.SetSide(entrySide, pathType);
        data.SetSide(exitSide, pathType);

        List<TerrainType> availableTypes = BuildTypePool(TerrainType.Plain);
        availableTypes.Remove(pathType);

        for (int side = 0; side < 6; side++)
        {
            if (data.GetSide(side) != TerrainType.Empty)
            {
                continue;
            }

            TerrainType type = availableTypes[Random.Range(0, availableTypes.Count)];
            data.SetSide(side, type);
        }

        return data;
    }

    private int ChoosePathExitSide(int entrySide)
    {
        float roll = Random.value;

        if (roll < 0.6f)
        {
            return Mod(entrySide + 3, 6); // straight
        }

        if (roll < 0.8f)
        {
            return Mod(entrySide + 2, 6); // soft turn
        }

        return Mod(entrySide + 1, 6); // sharp turn
    }

    private void FillCircularBlock(TileData data, int startSide, int length, TerrainType terrainType)
    {
        for (int i = 0; i < length; i++)
        {
            int side = Mod(startSide + i, 6);
            data.SetSide(side, terrainType);
        }
    }

    private List<TerrainType> BuildTypePool(TerrainType requiredType)
    {
        List<TerrainType> result = new List<TerrainType>();
        result.Add(requiredType);

        List<TerrainType> candidates = new List<TerrainType>(normalSideTypes);
        candidates.Remove(requiredType);

        int additionalTypeCount = Random.Range(1, maxTerrainTypesPerTile);

        while (result.Count < maxTerrainTypesPerTile && additionalTypeCount > 0 && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);

            result.Add(candidates[index]);
            candidates.RemoveAt(index);

            additionalTypeCount--;
        }

        return result;
    }

    private TerrainType GetRandom(TerrainType[] types)
    {
        return types[Random.Range(0, types.Length)];
    }

    private int Mod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}
