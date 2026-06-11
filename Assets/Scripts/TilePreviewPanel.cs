using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Dorfromantik-style preview of the deck:
//   - up to N upcoming tiles shown at fixed anchor slots (slot 0 = the CURRENT
//     tile that will be placed, the rest are the tiles coming after it),
//   - a procedurally built STACK of plain blocks representing the reserve tiles
//     (deck size minus the tiles already shown in the slots), which shrinks as
//     tiles get used,
//   - a counter label with how many tiles are left ("∞" when the deck is infinite).
//
// Nothing static needs authoring: the stack is generated from a block prefab
// (defaults to the base tile prefab) and grows/shrinks on its own. It reuses
// instances (no per-placement Instantiate/Destroy) and listens to
// CurrentTileController.DeckChanged.
//
// Wiring:
//   1. Create N empty child GameObjects as slot anchors (where the preview tiles
//      appear, e.g. a corner in front of the camera).
//   2. Create one empty GameObject as the stack anchor (where the reserve stack
//      starts; the stack grows along local -Y by default).
//   3. Add this component, assign the CurrentTileController, the slot transforms,
//      the stack anchor, optionally a tile/block prefab (defaults to the
//      controller's base tile prefab), and a TMP_Text for the counter.
public class TilePreviewPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CurrentTileController tileController;
    [Tooltip("Optional. Defaults to the CurrentTileController's base tile prefab.")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Preview slots (slot 0 = current tile)")]
    [SerializeField] private Transform[] slots;
    [Tooltip("Local scale applied to each preview tile instance.")]
    [SerializeField] private Vector3 slotScale = Vector3.one;

    [Header("Reserve stack")]
    [Tooltip("Where the reserve stack starts. Defaults to this transform.")]
    [SerializeField] private Transform stackAnchor;
    [Tooltip("Optional. Prefab used for one stack block. Defaults to the tile/base prefab.")]
    [SerializeField] private GameObject stackBlockPrefab;
    [Tooltip("Distance between two blocks in the stack (along local -Y).")]
    [SerializeField] private float stackHeightOffset = 0.02f;
    [Tooltip("Local scale applied to each stack block.")]
    [SerializeField] private Vector3 stackBlockScale = Vector3.one;
    [Tooltip("Max blocks actually instantiated. The counter still shows the true " +
             "remaining number; this only caps how tall the visual stack can get.")]
    [SerializeField] private int maxStackBlocks = 64;

    [Header("Counter")]
    [Tooltip("Optional. Shows how many tiles are left.")]
    [SerializeField] private TMP_Text remainingLabel;
    [Tooltip("Text shown for the counter when the deck is infinite.")]
    [SerializeField] private string infiniteText = "\u221E";

    [Header("Quest marks (optional)")]
    [Tooltip("Optional. When set, shows a quest mark on preview tiles that match an active quest.")]
    [SerializeField] private QuestManager questManager;
    [Tooltip("Quest-mark prefab (with a QuestMark component).")]
    [SerializeField] private GameObject questMarkPrefab;
    [Tooltip("Local offset of the mark relative to its slot.")]
    [SerializeField] private Vector3 questMarkOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private Vector3 questMarkScale = Vector3.one;

    private GameObject[] slotInstances;
    private QuestMark[] slotMarks;
    private readonly List<GameObject> stackBlocks = new List<GameObject>();

    private void OnEnable()
    {
        if (tileController != null)
        {
            tileController.DeckChanged += Refresh;
            tileController.RotationChanged += UpdateCurrentRotation;
        }

        if (questManager != null)
        {
            questManager.QuestsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (tileController != null)
        {
            tileController.DeckChanged -= Refresh;
            tileController.RotationChanged -= UpdateCurrentRotation;
        }

        if (questManager != null)
        {
            questManager.QuestsChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (tileController == null || slots == null || slots.Length == 0)
        {
            return;
        }

        var upcoming = tileController.GetUpcoming(slots.Length);

        RefreshSlots(upcoming);
        RefreshStack(upcoming.Count);
        RefreshCounter();
    }

    private void RefreshSlots(List<TileData> upcoming)
    {
        EnsureSlotInstances();

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject instance = slotInstances[i];

            if (instance == null)
            {
                continue;
            }

            bool filled = i < upcoming.Count && upcoming[i] != null;

            if (filled)
            {
                TileObjectSetup.ApplyData(instance, upcoming[i]);

                // Only slot 0 (the current tile) follows the player's rotation;
                // the upcoming tiles are shown unrotated.
                instance.transform.localRotation = i == 0
                    ? tileController.CurrentRotation
                    : Quaternion.identity;

                if (!instance.activeSelf)
                {
                    instance.SetActive(true);
                }
            }
            else if (instance.activeSelf)
            {
                instance.SetActive(false);
            }

            RefreshSlotMark(i, filled ? upcoming[i] : null);
        }
    }

    private void RefreshSlotMark(int slotIndex, TileData data)
    {
        if (questManager == null || questMarkPrefab == null || slots[slotIndex] == null)
        {
            return;
        }

        int rem = 0;
        bool show = data != null && questManager.TryGetRemainingFor(data, out rem);

        if (!show)
        {
            if (slotMarks != null && slotMarks[slotIndex] != null)
            {
                slotMarks[slotIndex].Hide();
            }

            return;
        }

        QuestMark mark = GetOrCreateSlotMark(slotIndex);

        if (mark == null)
        {
            return;
        }

        mark.transform.localPosition = questMarkOffset;
        mark.transform.localScale = questMarkScale;
        mark.SetRemaining(rem);
        mark.Show();
    }

    private QuestMark GetOrCreateSlotMark(int slotIndex)
    {
        if (slotMarks == null || slotMarks.Length != slots.Length)
        {
            slotMarks = new QuestMark[slots.Length];
        }

        if (slotMarks[slotIndex] != null)
        {
            return slotMarks[slotIndex];
        }

        GameObject instance = Instantiate(questMarkPrefab, slots[slotIndex]);
        instance.name = $"Preview Quest Mark {slotIndex}";

        QuestMark mark = instance.GetComponent<QuestMark>();

        if (mark == null)
        {
            Debug.LogError(
                $"'{questMarkPrefab.name}' is missing a QuestMark component. " +
                "Add it to the quest-mark prefab.");
            Destroy(instance);
            return null;
        }

        slotMarks[slotIndex] = mark;
        return mark;
    }

    private void UpdateCurrentRotation()
    {
        if (slotInstances == null || slotInstances.Length == 0)
        {
            return;
        }

        GameObject current = slotInstances[0];

        if (current != null)
        {
            current.transform.localRotation = tileController.CurrentRotation;
        }
    }

    // shownInSlots = how many tiles are already displayed in the slots, so the
    // stack only shows the tiles BEYOND those.
    private void RefreshStack(int shownInSlots)
    {
        int cap = Mathf.Max(0, maxStackBlocks);

        int reserve = tileController.IsInfiniteDeck
            ? cap
            : Mathf.Max(0, tileController.RemainingCount - shownInSlots);

        int visible = Mathf.Min(reserve, cap);

        EnsureStackBlocks(visible);

        for (int i = 0; i < stackBlocks.Count; i++)
        {
            GameObject block = stackBlocks[i];

            if (block == null)
            {
                continue;
            }

            bool shouldShow = i < visible;

            if (block.activeSelf != shouldShow)
            {
                block.SetActive(shouldShow);
            }
        }
    }

    private void RefreshCounter()
    {
        if (remainingLabel == null)
        {
            return;
        }

        remainingLabel.text = tileController.IsInfiniteDeck
            ? infiniteText
            : tileController.RemainingCount.ToString();
    }

    private void EnsureSlotInstances()
    {
        if (slotInstances != null && slotInstances.Length == slots.Length)
        {
            return;
        }

        GameObject prefab = ResolveSlotPrefab();

        if (prefab == null)
        {
            Debug.LogError("TilePreviewPanel has no tile prefab to display.");
            return;
        }

        slotInstances = new GameObject[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            Transform slot = slots[i];

            if (slot == null)
            {
                continue;
            }

            GameObject instance = Instantiate(prefab, slot);
            instance.name = $"Preview Tile {i}";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = slotScale;

            DisableColliders(instance);

            slotInstances[i] = instance;
        }
    }

    // Instantiates stack blocks up to `count` (never more than maxStackBlocks).
    // Each block sits a fixed step below the previous one, so the stack just grows
    // downward from the anchor and we only toggle visibility afterwards.
    private void EnsureStackBlocks(int count)
    {
        int cap = Mathf.Max(0, maxStackBlocks);
        int target = Mathf.Min(count, cap);

        if (stackBlocks.Count >= target)
        {
            return;
        }

        GameObject prefab = stackBlockPrefab != null ? stackBlockPrefab : ResolveSlotPrefab();

        if (prefab == null)
        {
            return;
        }

        Transform parent = stackAnchor != null ? stackAnchor : transform;

        for (int i = stackBlocks.Count; i < target; i++)
        {
            GameObject block = Instantiate(prefab, parent);
            block.name = $"Stack Block {i}";
            block.transform.localPosition = new Vector3(0f, -i * stackHeightOffset, 0f);
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = stackBlockScale;

            DisableColliders(block);

            stackBlocks.Add(block);
        }
    }

    private GameObject ResolveSlotPrefab()
    {
        return tilePrefab != null ? tilePrefab : tileController.BaseTilePrefab;
    }

    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
