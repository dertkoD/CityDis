public readonly struct PlacementResult
{
    public readonly bool IsSuccess;
    public readonly bool WasSnapped;
    public readonly bool IsPerfect;
    public readonly int Score;
    public readonly float Offset;

    public PlacementResult(bool isSuccess, bool wasSnapped, bool isPerfect, int score, float offset)
    {
        IsSuccess = isSuccess;
        WasSnapped = wasSnapped;
        IsPerfect = isPerfect;
        Score = score;
        Offset = offset;
    }
}