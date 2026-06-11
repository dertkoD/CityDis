using UnityEngine;

public class PlacementEvaluator : MonoBehaviour
{
    [SerializeField] private TowerBloxxConfig config;

    public PlacementResult Evaluate(TowerBlock droppedBlock, float targetTopY, float targetX)
    {
        float offset = Mathf.Abs(droppedBlock.transform.position.x - targetX);
        bool shouldSnap = offset <= config.snapOffset;

        if (offset <= config.perfectOffset)
        {
            PlaceBlock(droppedBlock, targetTopY, targetX);

            return new PlacementResult(
                true,
                true,
                true,
                config.perfectScore,
                offset
            );
        }

        if (offset <= config.goodOffset)
        {
            float finalX = shouldSnap ? targetX : droppedBlock.transform.position.x;

            PlaceBlock(droppedBlock, targetTopY, finalX);

            return new PlacementResult(
                true,
                shouldSnap,
                false,
                config.goodScore,
                offset
            );
        }

        if (offset <= config.acceptableOffset)
        {
            float finalX = shouldSnap ? targetX : droppedBlock.transform.position.x;

            PlaceBlock(droppedBlock, targetTopY, finalX);

            return new PlacementResult(
                true,
                shouldSnap,
                false,
                config.acceptableScore,
                offset
            );
        }

        return new PlacementResult(false, false, false, 0, offset);
    }

    private void PlaceBlock(TowerBlock block, float targetTopY, float targetX)
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