using System;
using System.Collections.Generic;
using UnityEngine;

// Shows the size of each terrain group as a floating number at the group's
// centroid (Dorfromantik style: a number that sits flat on the ground over the
// patch of forest / river / railroad / water it belongs to).
//
// It listens to GroupTracker.GroupsChanged and pools a set of GroupLabel
// objects, so no per-frame work and no Instantiate/Destroy churn after the pool
// has grown. It never uses FindObject/AddComponent.
//
// Wiring:
//   1. Create a label prefab: an empty GameObject with a child TextMeshPro - Text
//      (3D), add the GroupLabel component and assign its TMP_Text.
//   2. Add this component to a manager GameObject.
//   3. Assign: GroupTracker, the label prefab, a parent transform for the labels,
//      and match hexSize / orientation to the TilePlacementController.
public class GroupVisualizer : MonoBehaviour
{
    [Serializable]
    public class FamilyColor
    {
        public TerrainGroupFamily family;
        public Color color = Color.white;
    }

    [Header("References")]
    [SerializeField] private GroupTracker groupTracker;
    [SerializeField] private GameObject labelPrefab;
    [Tooltip("Set this to the SAME transform used as 'tileParent' on the " +
             "TilePlacementController, so labels line up with the tiles.")]
    [SerializeField] private Transform labelParent;

    [Header("Grid (match TilePlacementController)")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private float hexSize = 0.1f;

    [Header("Label placement")]
    [Tooltip("Height above the board where labels float (in the board's local space).")]
    [SerializeField] private float heightOffset = 0.01f;
    [Tooltip("Uniform local scale of each label. TextMeshPro text is huge by " +
             "default, so on a small board (hexSize ~0.1) this must be tiny.")]
    [SerializeField] private float labelScale = 0.01f;
    [Tooltip("Lay labels flat on the board (read from above) instead of standing upright.")]
    [SerializeField] private bool flatOnGround = true;
    [Tooltip("Only show a label once a group reaches at least this many tiles.")]
    [SerializeField] private int minGroupSizeToShow = 2;

    [Header("Colors per family")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private FamilyColor[] familyColors;

    private readonly List<GroupLabel> pool = new List<GroupLabel>();

    private void OnEnable()
    {
        if (groupTracker != null)
        {
            groupTracker.GroupsChanged += OnGroupsChanged;
            Refresh(groupTracker.CurrentGroups);
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
        Refresh(groups);
    }

    private void Refresh(IReadOnlyList<TileGroup> groups)
    {
        if (labelPrefab == null)
        {
            return;
        }

        int used = 0;
        Quaternion rotation = flatOnGround
            ? Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.identity;

        if (groups != null)
        {
            foreach (TileGroup group in groups)
            {
                int size = group.GetSize();

                if (size < minGroupSizeToShow)
                {
                    continue;
                }

                GroupLabel label = GetOrCreateLabel(used);

                if (label == null)
                {
                    continue;
                }

                used++;

                Vector3 position = group.GetLayoutCentroid(hexSize, orientation);
                position.y += heightOffset;

                label.SetValue(size, GetColor(group.Family));
                label.Show(position, rotation, Vector3.one * labelScale);
            }
        }

        // Hide any pooled labels we did not use this round.
        for (int i = used; i < pool.Count; i++)
        {
            pool[i].Hide();
        }
    }

    private GroupLabel GetOrCreateLabel(int index)
    {
        if (index < pool.Count)
        {
            return pool[index];
        }

        Transform parent = labelParent != null ? labelParent : transform;
        GameObject instance = Instantiate(labelPrefab, parent);

        GroupLabel label = instance.GetComponent<GroupLabel>();

        if (label == null)
        {
            // Add the GroupLabel component to the label prefab in the inspector.
            Debug.LogError(
                $"'{labelPrefab.name}' is missing a GroupLabel component. " +
                "Add it to the label prefab.");
            Destroy(instance);
            return null;
        }

        pool.Add(label);
        return label;
    }

    private Color GetColor(TerrainGroupFamily family)
    {
        if (familyColors != null)
        {
            foreach (FamilyColor entry in familyColors)
            {
                if (entry.family == family)
                {
                    return entry.color;
                }
            }
        }

        return defaultColor;
    }
}
