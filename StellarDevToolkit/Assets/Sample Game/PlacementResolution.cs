using UnityEngine;

public struct PlacementResolution
{
    public readonly bool IsValid;
    public readonly int ClearedLineCount;
    public readonly Vector2Int[] ClearedCoords;

    public PlacementResolution(bool isValid, int clearedLineCount, Vector2Int[] clearedCoords)
    {
        IsValid = isValid;
        ClearedLineCount = clearedLineCount;
        ClearedCoords = clearedCoords ?? new Vector2Int[0];
    }

    public static PlacementResolution Invalid()
    {
        return new PlacementResolution(false, 0, null);
    }
}
