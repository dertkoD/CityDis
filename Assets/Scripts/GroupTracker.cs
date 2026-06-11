using System;
using System.Collections.Generic;
using UnityEngine;

// Single source of truth for the terrain groups currently on the board.
//
// The board is small, so groups are fully recomputed after each placement. Other
// systems (scoring, group-size labels) subscribe to GroupsChanged or read
// CurrentGroups instead of recomputing on their own.
//
// Wiring: drop this on a manager GameObject, assign the BoardGrid, and have the
// TilePlacementController call Recompute() after every placement.
public class GroupTracker : MonoBehaviour
{
    [SerializeField] private BoardGrid boardGrid;

    private readonly ConnectionRules connectionRules = new ConnectionRules();
    private GroupManager groupManager;

    private readonly List<TileGroup> currentGroups = new List<TileGroup>();

    public IReadOnlyList<TileGroup> CurrentGroups => currentGroups;
    public event Action<IReadOnlyList<TileGroup>> GroupsChanged;

    private void Awake()
    {
        groupManager = new GroupManager(connectionRules);
    }

    public IReadOnlyList<TileGroup> Recompute()
    {
        if (boardGrid == null)
        {
            return currentGroups;
        }

        groupManager ??= new GroupManager(connectionRules);

        currentGroups.Clear();
        currentGroups.AddRange(groupManager.BuildGroups(boardGrid));

        GroupsChanged?.Invoke(currentGroups);

        return currentGroups;
    }
}
