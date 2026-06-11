using System;
using UnityEngine;

public class CraneController : MonoBehaviour
{
    [SerializeField] private TowerBloxxConfig config;
    [SerializeField] private Transform hookPoint;
    [SerializeField] private LineRenderer ropeRenderer;

    private TowerBlock _heldBlock;
    private float _time;
    private float _currentHeight;
    private int _spawnIndex;
    private Vector3 _previousHookPosition;

    public event Action<TowerBlock> BlockDropped;

    private void Awake()
    {
        _currentHeight = config.highCraneY;
        _previousHookPosition = hookPoint.position;
    }

    private void Update()
    {
        MoveCrane();

        if (_heldBlock)
        {
            MoveHeldBlock();
            DrawRope();
        }
    }

    public void SpawnBlock(Vector3 stackTopPosition)
    {
        _currentHeight = GetNextCraneHeight();

        Vector3 spawnPosition = new Vector3(
            stackTopPosition.x,
            stackTopPosition.y + config.blockSpawnYOffsetFromTop + _currentHeight,
            0f
        );

        _heldBlock = Instantiate(config.blockPrefab, spawnPosition, Quaternion.identity);
        _heldBlock.SetHeldState();

        _previousHookPosition = hookPoint.position;

        if (ropeRenderer)
        {
            ropeRenderer.enabled = true;
        }
    }

    public void DropHeldBlock()
    {
        if (!_heldBlock)
        {
            return;
        }

        TowerBlock droppedBlock = _heldBlock;
        _heldBlock = null;

        droppedBlock.SetDroppedState();

        if (ropeRenderer)
        {
            ropeRenderer.enabled = false;
        }

        BlockDropped?.Invoke(droppedBlock);
    }

    private void MoveCrane()
    {
        _time += Time.deltaTime * config.horizontalSpeed;

        float x = Mathf.Sin(_time) * config.horizontalAmplitude;

        Vector3 position = hookPoint.position;
        position.x = x;
        position.y = _currentHeight + Camera.main.transform.position.y;
        hookPoint.position = position;
    }

    private void MoveHeldBlock()
    {
        Vector3 hookPosition = hookPoint.position;

        Vector3 blockPosition = hookPosition;
        blockPosition.y -= config.blockSize.y * 0.8f;

        _heldBlock.transform.position = blockPosition;

        float swingAngle = Mathf.Cos(_time) * 8f;
        _heldBlock.transform.rotation = Quaternion.Euler(0f, 0f, swingAngle);
    }

    private void DrawRope()
    {
        if (!ropeRenderer || !_heldBlock)
        {
            return;
        }

        ropeRenderer.SetPosition(0, hookPoint.position);
        ropeRenderer.SetPosition(1, _heldBlock.transform.position + Vector3.up * config.blockSize.y * 0.5f);
    }

    private Vector2 CalculateHookVelocity()
    {
        Vector3 currentPosition = hookPoint.position;
        Vector2 velocity = (currentPosition - _previousHookPosition) / Time.deltaTime;
        _previousHookPosition = currentPosition;

        return velocity;
    }

    private float GetNextCraneHeight()
    {
        if (config.heightMode == CraneHeightMode.Random)
        {
            return UnityEngine.Random.value > 0.5f ? config.highCraneY : config.lowCraneY;
        }

        float height = _spawnIndex % 2 == 0 ? config.highCraneY : config.lowCraneY;
        _spawnIndex++;

        return height;
    }
}