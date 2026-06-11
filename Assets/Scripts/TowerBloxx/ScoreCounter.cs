using System;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    private int _score;

    public int Score => _score;

    public event Action<int> ScoreChanged;

    public void AddScore(int amount)
    {
        _score += amount;
        ScoreChanged?.Invoke(_score);
    }

    public void ResetScore()
    {
        _score = 0;
        ScoreChanged?.Invoke(_score);
    }
}