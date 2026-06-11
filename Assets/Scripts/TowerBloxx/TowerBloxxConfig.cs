using UnityEngine;

[CreateAssetMenu(menuName = "Tower Bloxx/Tower Bloxx Config")]
public class TowerBloxxConfig : ScriptableObject
{
    [Header("Block")]
    public TowerBlock blockPrefab;
    public Vector2 blockSize = new Vector2(2f, 1f);
    public int blocksToBuild = 5;

    [Header("Crane Ellipse Movement")]
    public float craneCenterX = 0f;
    public float craneEllipseRadiusX = 2.8f;
    public float craneEllipseRadiusY = 0.45f;
    public float craneEllipseSpeed = 1.5f;
    public float blockDistanceBelowHook = 0.8f;

    [Header("Crane Height")]
    public CraneHeightMode heightMode = CraneHeightMode.Alternating;
    public float highCraneY = 4.5f;
    public float lowCraneY = 3.4f;

    [Header("Drop Momentum")]
    public bool useCraneDropMomentum = true;
    public float dropMomentumMultiplier = 0.25f;
    public float maxDropXVelocity = 1.2f;

    [Header("Placement Accuracy")]
    public float perfectOffset = 0.1f;
    public float goodOffset = 0.35f;
    public float acceptableOffset = 0.8f;

    [Header("Handicap Snap")]
    public float snapOffset = 0.15f;

    [Header("Score")]
    public int perfectScore = 100;
    public int goodScore = 60;
    public int acceptableScore = 25;
    public int missPenalty = 30;

    [Header("Camera")]
    public float cameraMoveUpAmount = 1f;
    public float cameraMoveDuration = 0.35f;

    [Header("Air Upright")]
    public bool keepBlockUprightWhileFalling = true;
    public float uprightTorqueStrength = 8f;
    public float uprightDamping = 1.8f;

    [Header("Held Block Tilt")]
    public float heldBlockTiltMaxAngle = 12f;
    public float heldBlockTiltSmoothSpeed = 8f;
    public bool invertHeldBlockTilt = false;
}

public enum CraneHeightMode
{
    Alternating,
    Random
}