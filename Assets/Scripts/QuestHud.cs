using TMPro;
using UnityEngine;

// Displays the active quests as text on the player's canvas.
//
// Each quest is bound to one TMP_Text label (slot 0 -> quest 0, etc.). It listens
// to QuestManager.QuestsChanged and rewrites the labels, so it never polls.
//
// Wiring:
//   1. Create a Canvas with one TextMeshProUGUI per quest (3 for the starter set).
//   2. Add this component, assign the QuestManager, and drag the texts into
//      'Quest Labels' in the same order as the quests.
public class QuestHud : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private TMP_Text[] questLabels;

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
        if (questManager == null || questLabels == null)
        {
            return;
        }

        var quests = questManager.Quests;

        for (int i = 0; i < questLabels.Length; i++)
        {
            TMP_Text label = questLabels[i];

            if (label == null)
            {
                continue;
            }

            if (i < quests.Count && quests[i] != null)
            {
                label.text = quests[i].GetDescription();
            }
            else
            {
                label.text = string.Empty;
            }
        }
    }
}
