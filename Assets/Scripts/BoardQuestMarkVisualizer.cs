using System.Collections.Generic;
using UnityEngine;

// Places quest marks over the placed board tiles that contain a terrain belonging
// to an active quest. The mark shows how many sub-parts that quest still needs.
//
// Marks only appear while there are active quests and only on matching tiles. It
// pools the mark instances and refreshes whenever the quests change (which happens
// after every placement).
//
// Wiring:
//   1. Make a quest-mark prefab from your model and add the QuestMark component
//      (assign its TMP_Text).
//   2. Add this component to a manager GameObject, assign the QuestManager, the
//      BoardGrid, the mark prefab, a parent transform (use the SAME transform as
//      the TilePlacementController's 'tileParent' so positions line up), and match
//      hexSize / orientation.
public class BoardQuestMarkVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private BoardGrid boardGrid;
    [SerializeField] private GameObject questMarkPrefab;
    [SerializeField] private Transform markParent;

    [Header("Grid (match TilePlacementController)")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Placement")]
    [Tooltip("Height above the tile where the mark floats.")]
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
        if (questManager == null || boardGrid == null || questMarkPrefab == null)
        {
            return;
        }

        int used = 0;

        if (questManager.HasActiveQuests)
        {
            foreach (KeyValuePair<HexCoord, PlacedTile> entry in boardGrid.GetAllTiles())
            {
                if (!questManager.TryGetRemainingFor(entry.Value, out int remaining))
                {
                    continue;
                }

                QuestMark mark = GetOrCreate(used);

                if (mark == null)
                {
                    continue;
                }

                used++;

                Vector3 position = HexGridMath.HexToWorld(entry.Key, hexSize, orientation);
                position.y += heightOffset;

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
