using UnityEngine;

public static class GameUtility
{
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
}
