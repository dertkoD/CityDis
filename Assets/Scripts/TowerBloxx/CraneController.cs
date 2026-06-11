using System;
using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TowerBloxxConfig config;

    [Header("References")]
    [SerializeField] private Transform hookPoint;
    [SerializeField] private LineRenderer ropeRenderer;

    private TowerBlock _heldBlock;

    private float _ellipseTime;
    private float _currentHeight;
    private float _currentHeldBlockAngle;

    private int _spawnIndex;

    private Vector2 _currentHookVelocity;

    public event Action<TowerBlock> BlockDropped;

    private void Awake()
    {
        if (config) _currentHeight = config.highCraneY;
    }

    private void Update()
    {
        MoveHookOnEllipse();

        if (!_heldBlock) return;

        MoveHeldBlockUnderHook();
        DrawRope();
    }

    public void SpawnBlock()
    {
        if (!config || !hookPoint || !config.blockPrefab) return;

        _currentHeight = GetNextCraneHeight();
        _currentHeldBlockAngle = 0f;

        MoveHookOnEllipse();

        Vector3 blockPosition = GetHeldBlockPosition();

        _heldBlock = Instantiate(config.blockPrefab, blockPosition, Quaternion.identity);
        _heldBlock.SetHeldState();

        SetRopeVisible(true);
    }

    public void DropHeldBlock()
    {
        if (!_heldBlock) return;

        TowerBlock droppedBlock = _heldBlock;
        _heldBlock = null;

        float inheritedXVelocity = GetDropXVelocity();

        droppedBlock.transform.rotation = Quaternion.identity;
        droppedBlock.SetDroppedState(inheritedXVelocity, config);

        _currentHeldBlockAngle = 0f;

        SetRopeVisible(false);

        BlockDropped?.Invoke(droppedBlock);
    }

    public void ReattachBlock(TowerBlock block)
    {
        if (!block || !hookPoint) return;

        _heldBlock = block;

        Vector3 blockPosition = GetHeldBlockPosition();

        _heldBlock.ReturnToCrane(blockPosition, Quaternion.identity);

        _currentHeldBlockAngle = 0f;

        SetRopeVisible(true);
    }

    private void MoveHookOnEllipse()
    {
        if (!config|| !hookPoint) return;

        Vector3 previousPosition = hookPoint.position;

        _ellipseTime += Time.deltaTime * config.craneEllipseSpeed;

        float x = config.craneCenterX + Mathf.Cos(_ellipseTime) * config.craneEllipseRadiusX;

        float cameraY = Camera.main != null ? Camera.main.transform.position.y : 0f;
        float baseY = cameraY + _currentHeight;
        float y = baseY + Mathf.Sin(_ellipseTime) * config.craneEllipseRadiusY;

        hookPoint.position = new Vector3(x, y, 0f);

        if (Time.deltaTime > 0f) _currentHookVelocity = (hookPoint.position - previousPosition) / Time.deltaTime;
    }

    private void MoveHeldBlockUnderHook()
    {
        _heldBlock.transform.position = GetHeldBlockPosition();

        float targetAngle = GetHeldBlockTiltAngle();

        _currentHeldBlockAngle = Mathf.Lerp(
            _currentHeldBlockAngle,
            targetAngle,
            Time.deltaTime * config.heldBlockTiltSmoothSpeed
        );

        _heldBlock.transform.rotation = Quaternion.Euler(0f, 0f, _currentHeldBlockAngle);
    }

    private Vector3 GetHeldBlockPosition()
    {
        Vector3 blockPosition = hookPoint.position;
        blockPosition.y -= config.blockDistanceBelowHook;

        return blockPosition;
    }

    private float GetHeldBlockTiltAngle()
    {
        float normalizedCraneX = 0f;

        if (config.craneEllipseRadiusX > 0f) normalizedCraneX = (hookPoint.position.x - config.craneCenterX) / config.craneEllipseRadiusX;

        normalizedCraneX = Mathf.Clamp(normalizedCraneX, -1f, 1f);

        float angle = normalizedCraneX * config.heldBlockTiltMaxAngle;

        if (config.invertHeldBlockTilt) angle *= -1f;

        return angle;
    }

    private void DrawRope()
    {
        if (!ropeRenderer || !_heldBlock ) return;

        ropeRenderer.SetPosition(0, hookPoint.position);
        ropeRenderer.SetPosition(1, _heldBlock.RopeAttachPosition);
    }

    private float GetDropXVelocity()
    {
        if (!config.useCraneDropMomentum) return 0f;

        float xVelocity = _currentHookVelocity.x * config.dropMomentumMultiplier;

        return Mathf.Clamp(
            xVelocity,
            -config.maxDropXVelocity,
            config.maxDropXVelocity
        );
    }

    private float GetNextCraneHeight()
    {
        if (config.heightMode == CraneHeightMode.Random) 
            return UnityEngine.Random.value > 0.5f ? config.highCraneY : config.lowCraneY;

        float height = _spawnIndex % 2 == 0 ? config.highCraneY : config.lowCraneY;
        _spawnIndex++;

        return height;
    }

    private void SetRopeVisible(bool isVisible)
    {
        if (ropeRenderer) ropeRenderer.enabled = isVisible;
    }
}