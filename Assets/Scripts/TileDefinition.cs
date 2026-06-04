using UnityEngine;

public class TileDefinition : MonoBehaviour
{
    [SerializeField] private TileData tileData = new TileData();

    public TileData Data => tileData;

    public void SetData(TileData data)
    {
        if (data == null)
        {
            Debug.LogError("TileData is null.");
            return;
        }

        tileData = data.Clone();
    }

    public TerrainType GetLocalSide(int sideIndex)
    {
        return tileData.GetSide(sideIndex);
    }

    public TerrainType GetCenter()
    {
        return tileData.Center;
    }
}
