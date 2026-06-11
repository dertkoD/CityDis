using System;
using System.Collections;
using UnityEngine;

public class TowerBloxxGameController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TowerBloxxConfig config;

    [Header("Systems")]
    [SerializeField] private CraneController crane;
    [SerializeField] private TowerStack towerStack;
    [SerializeField] private PlacementEvaluator placementEvaluator;
    [SerializeField] private CameraScroller cameraScroller;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private ScoreCounter scoreCounter;

    private int _placedBlocks;
    private bool _canDrop;
    private bool _isResolvingDrop;

    public event Action<int> BlocksLeftChanged;
    public event Action<string> DropFeedbackChanged;
    public event Action HouseCompleted;

    public int BlocksLeft => config.blocksToBuild - _placedBlocks;

    private void OnEnable()
    {
        if (crane) crane.BlockDropped += HandleBlockDropped;
    }

    private void OnDisable()
    {
        if (crane) crane.BlockDropped -= HandleBlockDropped;
    }

    private void Start()
    {
        BlocksLeftChanged?.Invoke(BlocksLeft);
        StartNextTurn();
    }

    private void Update()
    {
        if (!_canDrop || _isResolvingDrop) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            _canDrop = false;
            crane.DropHeldBlock();
        }
    }

    private void StartNextTurn()
    {
        if (_placedBlocks >= config.blocksToBuild)
        {
            CompleteHouse();
            return;
        }

        crane.SpawnBlock();
        _canDrop = true;
    }

    private void HandleBlockDropped(TowerBlock block)
    {
        _isResolvingDrop = true;
        block.Landed += HandleBlockLanded;
    }

    private void HandleBlockLanded(TowerBlock block)
    {
        block.Landed -= HandleBlockLanded;
        StartCoroutine(ResolveDroppedBlockAfterShortDelay(block));
    }

    private IEnumerator ResolveDroppedBlockAfterShortDelay(TowerBlock block)
    {
        yield return new WaitForSeconds(0.06f);

        PlacementResult result = placementEvaluator.Evaluate(
            block,
            towerStack.TopY,
            towerStack.TopX
        );

        if (!result.IsSuccess)
        {
            HandleFailedDrop(block, result);

            yield break;
        }

        yield return HandleSuccessfulDrop(block, result);
    }

    private void HandleFailedDrop(TowerBlock block, PlacementResult result)
    {
        Debug.Log($"Failed drop. Offset: {result.Offset}. Penalty: {config.missPenalty}");

        scoreCounter.RemoveScore(config.missPenalty);
        DropFeedbackChanged?.Invoke($"Miss -{config.missPenalty}");

        cameraShake.Shake(0.2f, 0.15f);

        StartCoroutine(ReturnFailedBlockRoutine(block));
    }

    private IEnumerator ReturnFailedBlockRoutine(TowerBlock block)
    {
        yield return new WaitForSeconds(0.35f);

        crane.ReattachBlock(block);

        _isResolvingDrop = false;
        _canDrop = true;
    }

    private IEnumerator HandleSuccessfulDrop(TowerBlock block, PlacementResult result)
    {
        towerStack.RegisterSuccessfulBlock(block);

        scoreCounter.AddScore(result.Score);
        DropFeedbackChanged?.Invoke(GetDropFeedbackText(result));

        _placedBlocks++;
        BlocksLeftChanged?.Invoke(BlocksLeft);

        cameraShake.Shake();

        yield return new WaitForSeconds(0.08f);

        cameraScroller.MoveToTowerTop(towerStack.TopY);

        yield return new WaitForSeconds(config.cameraMoveDuration);

        _isResolvingDrop = false;

        StartNextTurn();
    }

    private void CompleteHouse()
    {
        Debug.Log("House completed!");

        DropFeedbackChanged?.Invoke(string.Empty);
        HouseCompleted?.Invoke();
    }

    private string GetDropFeedbackText(PlacementResult result)
    {
        if (result.IsPerfect)
        {
            return $"Perfect +{result.Score}";
        }

        if (result.Score == config.goodScore)
        {
            return $"Good +{result.Score}";
        }

        if (result.Score == config.acceptableScore)
        {
            return $"Ok +{result.Score}";
        }

        return $"+{result.Score}";
    }
}