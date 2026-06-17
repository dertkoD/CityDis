using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HouseBuildData
{
    [SerializeField] private List<HouseBlockData> blocks = new();

    public IReadOnlyList<HouseBlockData> Blocks => blocks;

    public HouseBuildData(IEnumerable<HouseBlockData> blocks)
    {
        this.blocks = new List<HouseBlockData>(blocks);
    }
}
