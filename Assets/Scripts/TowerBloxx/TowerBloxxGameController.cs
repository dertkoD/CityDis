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

    private void OnEnable()
    {
        crane.BlockDropped += HandleBlockDropped;
    }

    private void OnDisable()
    {
        crane.BlockDropped -= HandleBlockDropped;
    }

    private void Start()
    {
        StartNextTurn();
    }

    private void Update()
    {
        if (!_canDrop || _isResolvingDrop)
        {
            return;
        }

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
            Debug.Log("House completed!");
            return;
        }

        crane.SpawnBlock(towerStack.SpawnReferencePosition);
        _canDrop = true;
    }

    private void HandleBlockDropped(TowerBlock block)
    {
        StartCoroutine(ResolveDroppedBlockRoutine(block));
    }

    private IEnumerator ResolveDroppedBlockRoutine(TowerBlock block)
    {
        _isResolvingDrop = true;

        yield return WaitForBlockToSettle(block);

        PlacementResult result = placementEvaluator.Evaluate(
            block,
            towerStack.TopY,
            towerStack.TopX
        );
        
        if (!result.IsSuccess)
        {
            Debug.Log($"Failed drop. Offset: {result.Offset}");
            cameraShake.Shake(0.2f, 0.15f);
            _isResolvingDrop = false;
            yield break;
        }

        towerStack.RegisterSuccessfulBlock(block);
        scoreCounter.AddScore(result.Score);
        _placedBlocks++;

        Debug.Log($"Success. Score: {result.Score}, Offset: {result.Offset}, Snapped: {result.WasSnapped}");

        cameraShake.Shake();

        yield return new WaitForSeconds(0.15f);

        cameraScroller.MoveToTowerTop(towerStack.TopY);

        yield return new WaitForSeconds(config.cameraMoveDuration);

        _isResolvingDrop = false;

        StartNextTurn();
    }

    private IEnumerator WaitForBlockToSettle(TowerBlock block)
    {
        float totalTimer = 0f;
        float settledTimer = 0f;

        while (totalTimer < config.maxSettleWaitTime)
        {
            totalTimer += Time.deltaTime;

            if (block.IsSettled(config.settleVelocityThreshold, config.settleAngularVelocityThreshold))
            {
                settledTimer += Time.deltaTime;

                if (settledTimer >= config.settleRequiredTime)
                {
                    yield break;
                }
            }
            else
            {
                settledTimer = 0f;
            }

            yield return null;
        }
    }
}