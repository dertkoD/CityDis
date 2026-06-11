using UnityEngine;

[CreateAssetMenu(menuName = "Tower Bloxx/Tower Bloxx Config")]
public class TowerBloxxConfig : ScriptableObject
{
    [Header("Block")]
    public TowerBlock blockPrefab;
    public Vector2 blockSize = new Vector2(2f, 1f);
    public int blocksToBuild = 5;

    [Header("Crane Movement")]
    public float horizontalAmplitude = 3f;
    public float horizontalSpeed = 1.5f;

    [Header("Crane Height")]
    public CraneHeightMode heightMode = CraneHeightMode.Alternating;
    public float highCraneY = 4.5f;
    public float lowCraneY = 3.4f;

    [Header("Drop")]
    public float blockSpawnYOffsetFromTop = 2f;
    public float settleVelocityThreshold = 0.08f;
    public float settleAngularVelocityThreshold = 5f;
    public float settleRequiredTime = 0.35f;
    public float maxSettleWaitTime = 3f;

    [Header("Placement")]
    public float perfectOffset = 0.08f;
    public float snapOffset = 0.25f;
    public float acceptableOffset = 0.9f;

    [Header("Score")]
    public int perfectScore = 100;
    public int goodScore = 60;
    public int acceptableScore = 25;

    [Header("Camera")]
    public float cameraMoveUpAmount = 1f;
    public float cameraMoveDuration = 0.35f;
}

public enum CraneHeightMode
{
    Alternating,
    Random
}