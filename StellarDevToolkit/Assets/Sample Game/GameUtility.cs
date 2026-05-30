using UnityEngine;

public static class GameUtility
{
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

    public static int CalculatePlacementScore(BoardState boardStateBeforePlacement, BoardState boardStateAfterPlacement, int streak)
    {
        if (boardStateBeforePlacement.OccupiedBits == boardStateAfterPlacement.OccupiedBits)
        {
            return 0;
        }

        int completedLinesBeforePlacement = GetCompletedLineCount(boardStateBeforePlacement);
        int completedLinesAfterPlacement = GetCompletedLineCount(boardStateAfterPlacement);
        int newlyCompletedLineCount = Mathf.Max(0, completedLinesAfterPlacement - completedLinesBeforePlacement);
        if (newlyCompletedLineCount == 0)
        {
            return 0;
        }

        int clampedStreak = Mathf.Max(0, streak);
        int multiplierPercent = 100 + (MaxStreakBonusPercent * clampedStreak) / (StreakSoftener + clampedStreak);
        int lineScore = newlyCompletedLineCount * BasePointsPerLine;
        return (lineScore * multiplierPercent) / 100;
    }

    static int GetCompletedLineCount(BoardState boardState)
    {
        int boardSize = BlockBlastConstants.BoardSize;
        int completedLineCount = 0;

        for (int y = 0; y < boardSize; y++)
        {
            bool isFullRow = true;
            for (int x = 0; x < boardSize; x++)
            {
                if (!boardState.IsOccupied(x, y))
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
            {
                completedLineCount++;
            }
        }

        for (int x = 0; x < boardSize; x++)
        {
            bool isFullColumn = true;
            for (int y = 0; y < boardSize; y++)
            {
                if (!boardState.IsOccupied(x, y))
                {
                    isFullColumn = false;
                    break;
                }
            }

            if (isFullColumn)
            {
                completedLineCount++;
            }
        }

        return completedLineCount;
    }
}
