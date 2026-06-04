using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoord Coord { get; private set; }

    [SerializeField] private Transform visualAnchor;

    public Transform VisualAnchor => visualAnchor;

    private void Reset()
    {
        if (visualAnchor == null)
        {
            GameObject anchor = new GameObject("VisualAnchor");
            anchor.transform.SetParent(transform, false);
            visualAnchor = anchor.transform;
        }
    }

    public void Initialize(HexCoord coord)
    {
        Coord = coord;
        gameObject.name = $"Hex Cell {coord}";
    }
}
