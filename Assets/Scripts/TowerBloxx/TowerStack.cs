using UnityEngine;
using System.Collections.Generic;

public class TowerStack : MonoBehaviour
{
    [SerializeField] private Transform startingTopPoint;

    private TowerBlock _topBlock;
    private readonly List<TowerBlock> _successfulBlocks = new();

    public IReadOnlyList<TowerBlock> SuccessfulBlocks => _successfulBlocks;

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

        if (!_successfulBlocks.Contains(block))
        {
            _successfulBlocks.Add(block);
        }
    }
}
