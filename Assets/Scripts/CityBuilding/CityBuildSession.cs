using System;

public static class CityBuildSession
{
    public static bool HasPendingTile { get; private set; }
    public static HexCoord PendingTileCoord { get; private set; }
    public static HouseBuildData CompletedHouse { get; private set; }

    public static event Action<HexCoord, HouseBuildData> HouseCompleted;

    public static void StartBuilding(HexCoord tileCoord)
    {
        PendingTileCoord = tileCoord;
        CompletedHouse = null;
        HasPendingTile = true;
    }

    public static void CancelBuilding()
    {
        CompletedHouse = null;
        HasPendingTile = false;
    }

    public static void CompleteBuilding(HouseBuildData houseBuildData)
    {
        CompletedHouse = houseBuildData;
        HouseCompleted?.Invoke(PendingTileCoord, houseBuildData);
    }

    public static bool TryConsumeCompletedHouse(out HexCoord tileCoord, out HouseBuildData houseBuildData)
    {
        tileCoord = PendingTileCoord;
        houseBuildData = CompletedHouse;

        if (!HasPendingTile || CompletedHouse == null)
        {
            return false;
        }

        HasPendingTile = false;
        CompletedHouse = null;
        return true;
    }
}
