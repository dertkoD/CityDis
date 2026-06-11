using UnityEngine;

public class TowerStack : MonoBehaviour
{
    [SerializeField] private Transform startingTopPoint;

    private TowerBlock _topBlock;

    public float TopY
    {
        get
        {
            if (_topBlock != null)
            {
                return _topBlock.TopY;
            }

            return startingTopPoint.position.y;
        }
    }

    public float TopX
    {
        get
        {
            if (_topBlock != null)
            {
                return _topBlock.transform.position.x;
            }

            return startingTopPoint.position.x;
        }
    }

    public Vector3 SpawnReferencePosition
    {
        get
        {
            return new Vector3(TopX, TopY, 0f);
        }
    }

    public void RegisterSuccessfulBlock(TowerBlock block)
    {
        _topBlock = block;
    }
}