using UnityEngine;
using System.Collections.Generic;

public static class GameUtility
{
    public const float BoardCellSize = 1.0f;

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

    public static Vector2Int GetNearestCellCoord(Vector2 worldXY, float cellSize = BoardCellSize)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldXY.x / cellSize),
            Mathf.RoundToInt(worldXY.y / cellSize));
    }

    public static float GetDistanceToNearestCellEdge(Vector2 worldXY, Vector2Int cellCoord, float cellSize = BoardCellSize)
    {
        float half = cellSize * 0.5f;
        float localX = worldXY.x - cellCoord.x * cellSize;
        float localY = worldXY.y - cellCoord.y * cellSize;
        float distToVerticalEdge = half - Mathf.Abs(localX);
        float distToHorizontalEdge = half - Mathf.Abs(localY);
        return Mathf.Max(0.0f, Mathf.Min(distToVerticalEdge, distToHorizontalEdge));
    }

    public static float GetDistanceOutsideBoard(Vector2 worldXY, int boardSize = BlockBlastConstants.BoardSize, float cellSize = BoardCellSize)
    {
        float min = -cellSize * 0.5f;
        float max = (boardSize - 1) * cellSize + cellSize * 0.5f;

        float dx = 0.0f;
        if (worldXY.x < min)
        {
            dx = min - worldXY.x;
        }
        else if (worldXY.x > max)
        {
            dx = worldXY.x - max;
        }

        float dy = 0.0f;
        if (worldXY.y < min)
        {
            dy = min - worldXY.y;
        }
        else if (worldXY.y > max)
        {
            dy = worldXY.y - max;
        }

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    public static void AppendChebyshevNeighborhood(Vector2Int origin, int range, List<Vector2Int> results)
    {
        if (results == null || range < 0)
        {
            return;
        }

        for (int y = origin.y - range; y <= origin.y + range; y++)
        {
            for (int x = origin.x - range; x <= origin.x + range; x++)
            {
                results.Add(new Vector2Int(x, y));
            }
        }
    }
}
