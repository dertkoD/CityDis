using UnityEngine;

public class PlacementEvaluator : MonoBehaviour
{
    [SerializeField] private TowerBloxxConfig config;

    public PlacementResult Evaluate(TowerBlock droppedBlock, float targetTopY, float targetX)
    {
        float offset = Mathf.Abs(droppedBlock.transform.position.x - targetX);

        if (offset <= config.perfectOffset)
        {
            PlaceBlock(droppedBlock, targetTopY, targetX, true);
            return new PlacementResult(true, true, true, config.perfectScore, offset);
        }

        if (offset <= config.snapOffset)
        {
            PlaceBlock(droppedBlock, targetTopY, targetX, true);
            return new PlacementResult(true, true, false, config.goodScore, offset);
        }

        if (offset <= config.acceptableOffset)
        {
            PlaceBlock(droppedBlock, targetTopY, droppedBlock.transform.position.x, false);
            return new PlacementResult(true, false, false, config.acceptableScore, offset);
        }

        return new PlacementResult(false, false, false, 0, offset);
    }

    private void PlaceBlock(TowerBlock block, float targetTopY, float targetX, bool snapped)
    {
        block.transform.rotation = Quaternion.identity;

        float bottomToPivotDistance = block.transform.position.y - block.BottomY;

        Vector3 position = block.transform.position;
        position.x = targetX;
        position.y = targetTopY + bottomToPivotDistance;

        block.transform.position = position;
        block.LockAsStackBlock();
    }
}