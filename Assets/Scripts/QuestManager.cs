using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

// Tracks terrain-group quests and rewards the player with deck tiles on completion.
//
// Quests live in a POOL. A fixed number of them are ACTIVE at a time. When an
// active quest is completed:
//   - the player is awarded its reward tiles,
//   - its requiredCount grows (so it is harder next time),
//   - it goes back into the pool,
//   - a DIFFERENT quest is drawn from the pool to take its slot.
//
// It listens to GroupTracker.GroupsChanged and measures each active quest's
// progress as the largest matching group's SUB-PART count.
//
// Wiring: drop this on a manager GameObject, assign the GroupTracker and the
// CurrentTileController, and fill the Quest Pool list in the inspector.
public class QuestManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GroupTracker groupTracker;
    [SerializeField] private CurrentTileController tileController;

    [Header("Quests")]
    [Tooltip("All possible quests. The active ones are drawn from this pool.")]
    [FormerlySerializedAs("quests")]
    [SerializeField] private List<TerrainGroupQuest> questPool = new List<TerrainGroupQuest>();

    [Tooltip("How many quests are shown / active at once.")]
    [SerializeField] private int activeQuestCount = 3;

    // Quests currently being worked on (what the HUD shows).
    private readonly List<TerrainGroupQuest> active = new List<TerrainGroupQuest>();

    // Quests waiting in reserve.
    private readonly List<TerrainGroupQuest> pool = new List<TerrainGroupQuest>();

    public IReadOnlyList<TerrainGroupQuest> Quests => active;

    public bool HasActiveQuests => active.Count > 0;

    // Raised after progress is re-evaluated, so the quest HUD can refresh its text.
    public event Action QuestsChanged;

    // Raised when a single quest is completed (quest, tiles awarded).
    public event Action<TerrainGroupQuest, int> QuestCompleted;

    private void Awake()
    {
        BuildInitialQuests();
    }

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

    private void BuildInitialQuests()
    {
        active.Clear();
        pool.Clear();

        foreach (TerrainGroupQuest quest in questPool)
        {
            if (quest != null)
            {
                pool.Add(quest);
            }
        }

        int target = Mathf.Min(Mathf.Max(0, activeQuestCount), pool.Count);

        for (int i = 0; i < target; i++)
        {
            TerrainGroupQuest drawn = DrawFromPool();

            if (drawn != null)
            {
                drawn.currentProgress = 0;
                active.Add(drawn);
            }
        }
    }

    private void OnGroupsChanged(IReadOnlyList<TileGroup> groups)
    {
        // Evaluate each active slot once. A quest drawn as a replacement this pass
        // is checked on the next placement, which avoids completion chains.
        for (int i = 0; i < active.Count; i++)
        {
            TerrainGroupQuest quest = active[i];

            if (quest == null)
            {
                continue;
            }

            int best = MaxSectionCount(groups, quest.family);
            quest.currentProgress = best;

            if (best >= quest.requiredCount)
            {
                CompleteAndSwap(i, best);
            }
        }

        QuestsChanged?.Invoke();
    }

    private void CompleteAndSwap(int slot, int achieved)
    {
        TerrainGroupQuest completed = active[slot];

        completed.timesCompleted++;

        if (tileController != null)
        {
            tileController.AddTiles(completed.rewardTiles);
        }

        // Harder next time it is drawn.
        int growth = Mathf.Max(1, completed.growthOnComplete);
        completed.requiredCount = achieved + growth;

        // Draw a replacement BEFORE returning the completed quest to the pool, so
        // the same quest is never re-drawn into the slot it just left.
        TerrainGroupQuest replacement = DrawFromPool();

        if (replacement != null)
        {
            pool.Add(completed);

            replacement.currentProgress = 0;
            active[slot] = replacement;
        }
        // If the pool is empty, the completed quest simply stays active with its
        // new (higher) target.

        QuestCompleted?.Invoke(completed, completed.rewardTiles);
    }

    private TerrainGroupQuest DrawFromPool()
    {
        if (pool.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, pool.Count);
        TerrainGroupQuest quest = pool[index];
        pool.RemoveAt(index);

        return quest;
    }

    // Quest-mark helpers: a tile "matches" an active quest when one of its
    // sub-sections (center or any edge) belongs to that quest's family. The number
    // shown on the mark is how many more sub-parts that quest still needs. When a
    // tile matches several active quests, the one closest to completion wins.

    public bool TryGetRemainingFor(TileData data, out int remaining)
    {
        remaining = 0;

        if (data == null)
        {
            return false;
        }

        bool found = false;
        int best = int.MaxValue;

        foreach (TerrainGroupQuest quest in active)
        {
            if (quest == null || !TileDataHasFamily(data, quest.family))
            {
                continue;
            }

            int rem = Mathf.Max(0, quest.requiredCount - quest.currentProgress);

            if (rem < best)
            {
                best = rem;
                found = true;
            }
        }

        if (found)
        {
            remaining = best;
        }

        return found;
    }

    public bool TryGetRemainingFor(PlacedTile tile, out int remaining)
    {
        remaining = 0;

        if (tile == null)
        {
            return false;
        }

        bool found = false;
        int best = int.MaxValue;

        foreach (TerrainGroupQuest quest in active)
        {
            if (quest == null || !PlacedTileHasFamily(tile, quest.family))
            {
                continue;
            }

            int rem = Mathf.Max(0, quest.requiredCount - quest.currentProgress);

            if (rem < best)
            {
                best = rem;
                found = true;
            }
        }

        if (found)
        {
            remaining = best;
        }

        return found;
    }

    private static bool TileDataHasFamily(TileData data, TerrainGroupFamily family)
    {
        if (TerrainCatalog.GroupFamily(data.Center) == family)
        {
            return true;
        }

        for (int side = 0; side < 6; side++)
        {
            if (TerrainCatalog.GroupFamily(data.GetSide(side)) == family)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PlacedTileHasFamily(PlacedTile tile, TerrainGroupFamily family)
    {
        if (TerrainCatalog.GroupFamily(tile.GetCenterTerrain()) == family)
        {
            return true;
        }

        for (int side = 0; side < 6; side++)
        {
            if (TerrainCatalog.GroupFamily(tile.GetSideTerrain(side)) == family)
            {
                return true;
            }
        }

        return false;
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
