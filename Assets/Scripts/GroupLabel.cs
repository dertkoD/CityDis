using TMPro;
using UnityEngine;

// A single world-space label that shows the size of one terrain group.
//
// Attach this to the label prefab and assign its TMP_Text. The GroupVisualizer
// pools these and calls SetValue / Show / Hide on them.
public class GroupLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void SetValue(int size, Color color)
    {
        if (label == null)
        {
            return;
        }

        label.text = size.ToString();
        label.color = color;
    }

    public void Show(Vector3 worldPosition, Quaternion rotation)
    {
        Transform t = transform;
        t.position = worldPosition;
        t.rotation = rotation;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Reset()
    {
        label = GetComponentInChildren<TMP_Text>();
    }
}
