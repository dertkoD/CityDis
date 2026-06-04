using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks the player's score and the terrain groups on the board.
//
// This component is OPTIONAL: if it is not wired into the TilePlacementController
// the game still runs, it just won't score.
//
// Scoring (all values tunable in the inspector):
//   - every placed tile is worth `pointsPerTile`
//   - every edge that matches its neighbor (same terrain) is worth `pointsPerMatchingEdge`
//   - placing a tile whose touching edges ALL match earns `perfectPlacementBonus`
//   - closing off a river / railroad group earns `closedGroupBonusPerTile` per tile
public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardGrid boardGrid;

    [Header("Scoring")]
    [SerializeField] private int pointsPerTile = 1;
    [SerializeField] private int pointsPerMatchingEdge = 2;
    [SerializeField] private int perfectPlacementBonus = 10;
    [SerializeField] private int closedGroupBonusPerTile = 15;

    [Header("Debug HUD")]
    [Tooltip("Draws the score in the top-left corner using OnGUI (handy for prototyping).")]
    [SerializeField] private bool showScoreOnGui = true;

    private readonly ConnectionRules connectionRules = new ConnectionRules();
    private GroupManager groupManager;
    private readonly HashSet<string> awardedClosedGroups = new();

    private string lastPlacementInfo = string.Empty;

    public int Score { get; private set; }
    public event Action<int> ScoreChanged;

    private void Awake()
    {
        groupManager = new GroupManager(connectionRules);
    }

    public void ResetScore()
    {
        Score = 0;
        awardedClosedGroups.Clear();
        lastPlacementInfo = string.Empty;
        ScoreChanged?.Invoke(Score);
    }

    public void OnTilePlaced(HexCoord coord)
    {
        if (boardGrid == null)
        {
            return;
        }

        if (groupManager == null)
        {
            groupManager = new GroupManager(connectionRules);
        }

        PlacedTile placedTile = boardGrid.GetTile(coord);

        if (placedTile == null)
        {
            return;
        }

        int gained = pointsPerTile;

        int occupiedNeighbors = 0;
        int matchingEdges = 0;

        for (int side = 0; side < 6; side++)
        {
            HexCoord neighborCoord = HexGridMath.GetNeighbor(coord, side);
            PlacedTile neighborTile = boardGrid.GetTile(neighborCoord);

            if (neighborTile == null)
            {
                continue;
            }

            occupiedNeighbors++;

            TerrainType mine = placedTile.GetSideTerrain(side);
            TerrainType theirs = neighborTile.GetSideTerrain(HexGridMath.GetOppositeDirection(side));

            if (mine == theirs)
            {
                matchingEdges++;
            }
        }

        gained += matchingEdges * pointsPerMatchingEdge;

        bool perfect = occupiedNeighbors > 0 && matchingEdges == occupiedNeighbors;

        if (perfect)
        {
            gained += perfectPlacementBonus;
        }

        int closureBonus = AwardNewlyClosedGroups();
        gained += closureBonus;

        Score += gained;

        lastPlacementInfo = perfect
            ? $"PERFECT! +{gained}"
            : $"+{gained}";

        ScoreChanged?.Invoke(Score);
    }

    private int AwardNewlyClosedGroups()
    {
        if (boardGrid == null)
        {
            return 0;
        }

        List<TileGroup> groups = groupManager.BuildGroups(boardGrid);
        int bonus = 0;

        foreach (TileGroup group in groups)
        {
            if (!group.IsClosed)
            {
                continue;
            }

            string signature = group.GetSignature();

            if (awardedClosedGroups.Add(signature))
            {
                bonus += closedGroupBonusPerTile * group.GetSize();
            }
        }

        return bonus;
    }

    private void OnGUI()
    {
        if (!showScoreOnGui)
        {
            return;
        }

        GUI.Label(new Rect(12f, 10f, 400f, 30f), $"Score: {Score}");

        if (!string.IsNullOrEmpty(lastPlacementInfo))
        {
            GUI.Label(new Rect(12f, 32f, 400f, 30f), $"Last: {lastPlacementInfo}");
        }
    }
}
