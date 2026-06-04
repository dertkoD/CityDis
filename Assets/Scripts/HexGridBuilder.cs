using System.Collections.Generic;
using UnityEngine;

public class HexGridBuilder : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private HexOrientation orientation = HexOrientation.FlatTop;
    [SerializeField] private int gridRadius = 3;
    [SerializeField] private float hexSize = 0.01f;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellRootPrefab;
    [SerializeField] private GameObject visualPrefab;

    [Header("Visual Settings")]
    [SerializeField] private float visualYawOffset = 0f;
    [SerializeField] private Vector3 visualLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 visualLocalScale = Vector3.one;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool clearBeforeGenerate = true;

    private readonly Dictionary<HexCoord, HexCell> cells = new();

    private void Start()
    {
        if (generateOnStart)
            GenerateGrid();
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        if (clearBeforeGenerate)
            ClearGrid();

        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int rMin = Mathf.Max(-gridRadius, -q - gridRadius);
            int rMax = Mathf.Min(gridRadius, -q + gridRadius);

            for (int r = rMin; r <= rMax; r++)
            {
                CreateCell(new HexCoord(q, r));
            }
        }

        Debug.Log($"Generated cells: {cells.Count}");
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        cells.Clear();

        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void CreateCell(HexCoord coord)
    {
        Vector3 localPosition = HexGridMath.HexToWorld(coord, hexSize, orientation);

        GameObject cellObject;

        if (cellRootPrefab != null)
        {
            cellObject = Instantiate(cellRootPrefab, transform);
            cellObject.transform.localPosition = localPosition;
            cellObject.transform.localRotation = Quaternion.identity;
            cellObject.transform.localScale = Vector3.one;
        }
        else
        {
            cellObject = new GameObject();
            cellObject.transform.SetParent(transform, false);
            cellObject.transform.localPosition = localPosition;
        }

        HexCell cell = cellObject.GetComponent<HexCell>();
        if (cell == null)
            cell = cellObject.AddComponent<HexCell>();

        cell.Initialize(coord);

        if (visualPrefab != null)
        {
            Transform anchor = cell.VisualAnchor;
            if (anchor == null)
            {
                GameObject anchorObj = new GameObject("VisualAnchor");
                anchorObj.transform.SetParent(cell.transform, false);
                anchor = anchorObj.transform;
            }

            GameObject visual = Instantiate(visualPrefab, anchor);
            visual.transform.localPosition = visualLocalOffset;
            visual.transform.localRotation = Quaternion.Euler(0f, visualYawOffset, 0f);
            visual.transform.localScale = visualLocalScale;
        }

        cells.Add(coord, cell);
    }
}
