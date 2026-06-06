using TMPro;
using UnityEngine;

// Dorfromantik-style preview of the deck:
//   - up to N upcoming tiles shown at fixed anchor slots (slot 0 = the CURRENT
//     tile that will be placed, the rest are the tiles coming after it),
//   - an optional "stack" object representing the reserve,
//   - a counter label with how many tiles are left ("∞" when the deck is infinite).
//
// It reuses one tile instance per slot (no per-placement Instantiate/Destroy) and
// listens to CurrentTileController.DeckChanged.
//
// Wiring:
//   1. Create N empty child GameObjects as slot anchors (positioned where you want
//      the preview tiles to appear, e.g. in a corner in front of the camera).
//   2. Add this component, assign the CurrentTileController, the slot transforms,
//      optionally a tile prefab (defaults to the controller's base tile prefab),
//      a TMP_Text for the counter, and a stack GameObject.
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

    [Header("Reserve display")]
    [Tooltip("Optional. Shows how many tiles are left.")]
    [SerializeField] private TMP_Text remainingLabel;
    [Tooltip("Text shown for the counter when the deck is infinite.")]
    [SerializeField] private string infiniteText = "\u221E";
    [Tooltip("Optional. Toggled on while tiles remain (e.g. a stack of blocks).")]
    [SerializeField] private GameObject stackRoot;

    private GameObject[] slotInstances;

    private void OnEnable()
    {
        if (tileController != null)
        {
            tileController.DeckChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (tileController != null)
        {
            tileController.DeckChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (tileController == null || slots == null || slots.Length == 0)
        {
            return;
        }

        EnsureInstances();

        var upcoming = tileController.GetUpcoming(slots.Length);

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject instance = slotInstances[i];

            if (instance == null)
            {
                continue;
            }

            if (i < upcoming.Count && upcoming[i] != null)
            {
                TileObjectSetup.ApplyData(instance, upcoming[i]);

                if (!instance.activeSelf)
                {
                    instance.SetActive(true);
                }
            }
            else if (instance.activeSelf)
            {
                instance.SetActive(false);
            }
        }

        UpdateReserveDisplay();
    }

    private void UpdateReserveDisplay()
    {
        bool hasTiles = tileController.IsInfiniteDeck || tileController.RemainingCount > 0;

        if (remainingLabel != null)
        {
            remainingLabel.text = tileController.IsInfiniteDeck
                ? infiniteText
                : tileController.RemainingCount.ToString();
        }

        if (stackRoot != null && stackRoot.activeSelf != hasTiles)
        {
            stackRoot.SetActive(hasTiles);
        }
    }

    private void EnsureInstances()
    {
        if (slotInstances != null && slotInstances.Length == slots.Length)
        {
            return;
        }

        GameObject prefab = tilePrefab != null ? tilePrefab : tileController.BaseTilePrefab;

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

    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
