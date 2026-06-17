using System.Collections.Generic;
using UnityEngine;

// Places quest marks on the BOARD, on the group each active quest should grow.
//
// For every active quest it finds the largest OPEN group of that quest's family
// and puts a single mark over its centre, showing how many sub-parts are still
// needed. Closed groups are skipped (they can no longer grow), and if there is no
// open group of that family yet, no board mark is shown for that quest (the player
// can see the preview marks to know which tiles start it).
//
// It pools the mark instances and refreshes whenever the quests change (after
// every placement).
//
// Wiring:
//   1. Make a quest-mark prefab from your model and add the QuestMark component
//      (assign its TMP_Text).
//   2. Add this component to a manager GameObject, assign the QuestManager, the
//      GroupTracker, the mark prefab, a parent transform (use the SAME transform
//      as the TilePlacementController's 'tileParent' so positions line up), and
//      match hexSize / orientation.
public class BoardQuestMarkVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private GroupTracker groupTracker;
    [SerializeField] private GameObject questMarkPrefab;
    [SerializeField] private Transform markParent;

    [Header("Grid (match TilePlacementController)")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Placement")]
    [Tooltip("Height above the group where the mark floats.")]
    [SerializeField] private float heightOffset = 0.08f;
    [SerializeField] private Vector3 markScale = Vector3.one;

    private readonly List<QuestMark> pool = new List<QuestMark>();

    private void OnEnable()
    {
        if (questManager != null)
        {
            questManager.QuestsChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (questManager != null)
        {
            questManager.QuestsChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (questManager == null || groupTracker == null || questMarkPrefab == null)
        {
            return;
        }

        int used = 0;

        if (questManager.HasActiveQuests)
        {
            IReadOnlyList<TileGroup> groups = groupTracker.CurrentGroups;

            foreach (TerrainGroupQuest quest in questManager.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                TileGroup target = FindLargestOpenGroup(groups, quest.family);

                if (target == null)
                {
                    continue;
                }

                QuestMark mark = GetOrCreate(used);

                if (mark == null)
                {
                    continue;
                }

                used++;

                Vector3 position = target.GetLayoutCentroid(
                    HexGridLayout.ResolveSize(hexSize),
                    HexGridLayout.ResolveOrientation(orientation));
                position.y += heightOffset;

                int remaining = Mathf.Max(0, quest.requiredCount - quest.currentProgress);

                mark.transform.localPosition = position;
                mark.transform.localScale = markScale;
                mark.SetRemaining(remaining);
                mark.Show();
            }
        }

        for (int i = used; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                pool[i].Hide();
            }
        }
    }

    private static TileGroup FindLargestOpenGroup(IReadOnlyList<TileGroup> groups, TerrainGroupFamily family)
    {
        if (groups == null)
        {
            return null;
        }

        TileGroup best = null;
        int bestSize = -1;

        foreach (TileGroup group in groups)
        {
            if (group.Family != family || group.IsClosed)
            {
                continue;
            }

            int size = group.GetSectionCount();

            if (size > bestSize)
            {
                bestSize = size;
                best = group;
            }
        }

        return best;
    }

    private QuestMark GetOrCreate(int index)
    {
        if (index < pool.Count)
        {
            return pool[index];
        }

        Transform parent = markParent != null ? markParent : transform;
        GameObject instance = Instantiate(questMarkPrefab, parent);

        QuestMark mark = instance.GetComponent<QuestMark>();

        if (mark == null)
        {
            Debug.LogError(
                $"'{questMarkPrefab.name}' is missing a QuestMark component. " +
                "Add it to the quest-mark prefab.");
            Destroy(instance);
            return null;
        }

        pool.Add(mark);
        return mark;
    }
}
