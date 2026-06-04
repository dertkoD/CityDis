using System;
using UnityEngine;

[Serializable]
public class TileData
{
    [SerializeField] private TerrainType center = TerrainType.Empty;
    [SerializeField] private TerrainType[] sides = new TerrainType[6];

    public TerrainType Center
    {
        get => center;
        set => center = value;
    }

    public TerrainType[] Sides => sides;

    public TerrainType GetSide(int sideIndex)
    {
        if (sideIndex < 0 || sideIndex >= 6)
        {
            Debug.LogError($"Invalid side index: {sideIndex}");
            return TerrainType.Empty;
        }

        return sides[sideIndex];
    }

    public void SetSide(int sideIndex, TerrainType terrainType)
    {
        if (sideIndex < 0 || sideIndex >= 6)
        {
            Debug.LogError($"Invalid side index: {sideIndex}");
            return;
        }

        sides[sideIndex] = terrainType;
    }

    public TileData Clone()
    {
        TileData clone = new TileData();
        clone.center = center;

        for (int i = 0; i < 6; i++)
        {
            clone.sides[i] = sides[i];
        }

        return clone;
    }
}
