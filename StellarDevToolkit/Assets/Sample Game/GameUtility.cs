using UnityEngine;

public static class GameUtility
{
    const float BoardCellSize = 1.0f;
    const int BasePointsPerLine = 10;
    const int StreakSoftener = 4;
    const int MaxStreakBonusPercent = 150;

    public static Vector2Int GetBoardSize()
    {
        return new Vector2Int(BlockBlastConstants.BoardSize, BlockBlastConstants.BoardSize);
    }

    public static Vector2Int GetPlacementAnchorCoord(Vector2Int hoveredCoord, int shapeGridSize = ShapeDefinition.GridSize)
    {
        int pivotOffset = shapeGridSize / 2;
        return hoveredCoord - new Vector2Int(pivotOffset, pivotOffset);
    }

    public static Vector2Int GetDropCellCoord(Vector2Int anchorCoord, int shapeGridSize = ShapeDefinition.GridSize)
    {
        int pivotOffset = shapeGridSize / 2;
        return anchorCoord + new Vector2Int(pivotOffset, pivotOffset);
    }

    public static Vector2Int GetPlacementTargetCoord(Vector2Int anchorCoord, Vector2Int tileOffset)
    {
        return anchorCoord + tileOffset;
    }

    public static float GetShapeGridCenterOffset(int shapeGridSize = ShapeDefinition.GridSize)
    {
        return (shapeGridSize - 1) * 0.5f;
    }

    public static Vector2Int GetLocalShapeCoord(Vector3 localPosition, int shapeGridSize = ShapeDefinition.GridSize)
    {
        float centerOffset = GetShapeGridCenterOffset(shapeGridSize);
        return new Vector2Int(
            Mathf.RoundToInt(localPosition.x + centerOffset),
            Mathf.RoundToInt(localPosition.y + centerOffset));
    }

    public static int CalculatePlacementScore(int newlyCompletedLineCount, int streak)
    {
        if (newlyCompletedLineCount <= 0)
        {
            return 0;
        }

        int clampedStreak = Mathf.Max(0, streak);
        int multiplierPercent = 100 + (MaxStreakBonusPercent * clampedStreak) / (StreakSoftener + clampedStreak);
        int lineScore = newlyCompletedLineCount * BasePointsPerLine;
        return (lineScore * multiplierPercent) / 100;
    }

    public static Vector2Int GetCellCoord(Vector2 boardPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(boardPosition.x / BoardCellSize),
            Mathf.RoundToInt(boardPosition.y / BoardCellSize));
    }

    public static Vector2 GetCellCenter(Vector2Int cellCoord)
    {
        return new Vector2(cellCoord.x, cellCoord.y) * BoardCellSize;
    }

    public static bool IsOnBoard(Vector2Int cellCoord)
    {
        return cellCoord.x >= 0 && cellCoord.x < BlockBlastConstants.BoardSize
            && cellCoord.y >= 0 && cellCoord.y < BlockBlastConstants.BoardSize;
    }

    public static float GetDistanceToNearestCellEdge(Vector2 boardPosition, Vector2Int cellCoord)
    {
        Vector2 local = boardPosition - GetCellCenter(cellCoord);
        float half = BoardCellSize * 0.5f;
        float distToVerticalEdge = half - Mathf.Abs(local.x);
        float distToHorizontalEdge = half - Mathf.Abs(local.y);
        return Mathf.Max(0.0f, Mathf.Min(distToVerticalEdge, distToHorizontalEdge));
    }

    public static float GetDistanceOutsideBoard(Vector2 boardPosition)
    {
        float half = BoardCellSize * 0.5f;
        float min = -half;
        float max = (BlockBlastConstants.BoardSize - 1) * BoardCellSize + half;
        float dx = DistanceOutsideRange(boardPosition.x, min, max);
        float dy = DistanceOutsideRange(boardPosition.y, min, max);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float DistanceOutsideRange(float value, float min, float max)
    {
        if (value < min)
        {
            return min - value;
        }

        if (value > max)
        {
            return value - max;
        }

        return 0.0f;
    }
}
