using TMPro;
using UnityEngine;

// A single quest mark (the Dorfromantik-style flag) that floats over a tile and
// shows how many more sub-parts are needed to complete the matching quest.
//
// Attach this to your quest-mark model prefab and assign the TMP_Text that draws
// the number. The board/preview visualizers pool these and call SetRemaining.
public class QuestMark : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void SetRemaining(int remaining)
    {
        if (label != null)
        {
            label.text = remaining.ToString();
        }
    }

    public void Show()
    {
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
