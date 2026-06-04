using UnityEngine;

public class TileSideAnchor : MonoBehaviour
{
    [SerializeField] private HexSide side;
    [SerializeField] private TerrainType terrainType;

    public HexSide Side => side;
    public TerrainType TerrainType => terrainType;
}