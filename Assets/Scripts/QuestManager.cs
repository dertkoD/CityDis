using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks terrain-group quests and rewards the player with deck tiles on completion.
//
// It listens to GroupTracker.GroupsChanged, measures each quest's progress as the
// largest matching group's SUB-PART count, and when a quest is met it awards tiles
// and refreshes the goal to a new, harder target.
//
// Wiring: drop this on a manager GameObject, assign the GroupTracker and the
// CurrentTileController, and fill the Quests list in the inspector.
public class QuestManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GroupTracker groupTracker;
    [SerializeField] private CurrentTileController tileController;

    [Header("Quests")]
    [SerializeField] private List<TerrainGroupQuest> quests = new List<TerrainGroupQuest>();

    public IReadOnlyList<TerrainGroupQuest> Quests => quests;

    // Raised after progress is re-evaluated, so the quest HUD can refresh its text.
    public event Action QuestsChanged;

    // Raised when a single quest is completed (quest, tiles awarded).
    public event Action<TerrainGroupQuest, int> QuestCompleted;

    private void OnEnable()
    {
        if (groupTracker != null)
        {
            groupTracker.GroupsChanged += OnGroupsChanged;
            OnGroupsChanged(groupTracker.CurrentGroups);
        }
    }

    private void OnDisable()
    {
        if (groupTracker != null)
        {
            groupTracker.GroupsChanged -= OnGroupsChanged;
        }
    }

    private void OnGroupsChanged(IReadOnlyList<TileGroup> groups)
    {
        foreach (TerrainGroupQuest quest in quests)
        {
            if (quest == null)
            {
                continue;
            }

            int best = MaxSectionCount(groups, quest.family);
            quest.currentProgress = best;

            if (best >= quest.requiredCount)
            {
                CompleteQuest(quest, best);
            }
        }

        QuestsChanged?.Invoke();
    }

    private void CompleteQuest(TerrainGroupQuest quest, int achieved)
    {
        quest.timesCompleted++;

        if (tileController != null)
        {
            tileController.AddTiles(quest.rewardTiles);
        }

        // Refresh the goal so it sits above what was just built (no instant re-trigger).
        int growth = Mathf.Max(1, quest.growthOnComplete);
        quest.requiredCount = achieved + growth;

        QuestCompleted?.Invoke(quest, quest.rewardTiles);
    }

    private int MaxSectionCount(IReadOnlyList<TileGroup> groups, TerrainGroupFamily family)
    {
        int best = 0;

        if (groups == null)
        {
            return best;
        }

        foreach (TileGroup group in groups)
        {
            if (group.Family != family)
            {
                continue;
            }

            int size = group.GetSectionCount();

            if (size > best)
            {
                best = size;
            }
        }

        return best;
    }
}
