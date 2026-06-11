using System;
using UnityEngine;

// One Dorfromantik-style quest: grow a connected terrain group of a given family
// to a required number of SUB-PARTS (per-edge sub-sections of the hexes, not whole
// tiles). Completing it rewards the player with extra deck tiles and then the goal
// "refreshes" to a new, harder target.
//
// New quest kinds later only need a new entry in the QuestManager list (and, if a
// brand new rule is needed, a new TerrainGroupFamily) - the evaluation is generic.
[Serializable]
public class TerrainGroupQuest
{
    [Tooltip("Shown in the quest text, e.g. \"Railroad\".")]
    public string displayName = "Quest";

    [Tooltip("Which terrain group family this quest tracks.")]
    public TerrainGroupFamily family = TerrainGroupFamily.Railroad;

    [Tooltip("How many connected sub-parts are required to complete the quest.")]
    public int requiredCount = 4;

    [Tooltip("Tiles awarded to the player's deck when the quest is completed.")]
    public int rewardTiles = 5;

    [Tooltip("On completion the next goal becomes (achieved + this), so a fresh, " +
             "harder target appears. Keep this above 0.")]
    public int growthOnComplete = 5;

    // Runtime state, kept out of the serialized authoring data.
    [NonSerialized] public int currentProgress;
    [NonSerialized] public int timesCompleted;

    public string GetDescription()
    {
        int shown = Mathf.Clamp(currentProgress, 0, requiredCount);
        return $"{displayName}: {shown}/{requiredCount}";
    }
}
