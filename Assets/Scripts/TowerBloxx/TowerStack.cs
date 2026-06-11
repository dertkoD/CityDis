using UnityEngine;

public class TowerStack : MonoBehaviour
{
    [SerializeField] private Transform startingTopPoint;

    private TowerBlock _topBlock;

    public float TopY
    {
        get
        {
            if (_topBlock) return _topBlock.TopY;

            return startingTopPoint.position.y;
        }
    }

    public float TopX
    {
        get
        {
            if (_topBlock) return _topBlock.transform.position.x;

            return startingTopPoint.position.x;
        }
    }

    public void RegisterSuccessfulBlock(TowerBlock block)
    {
        _topBlock = block;
    }
}