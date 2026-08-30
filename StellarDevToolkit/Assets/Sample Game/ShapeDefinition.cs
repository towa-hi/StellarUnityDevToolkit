using UnityEngine;
using System.Collections.Generic;

public struct TileData
{
    public Color Color;

    public static TileData WhiteDefault => new TileData { Color = Color.white };
}

[CreateAssetMenu(menuName = "Sample Game/Shape Definition")]
public class ShapeDefinition : ScriptableObject
{
    [SerializeField] string shapeId = "Shape";
    [SerializeField] int packedShapeData = 0;

    public string ShapeId => shapeId;
    public int PackedShapeData => packedShapeData;
    public int ReservedBits => (int)((uint)packedShapeData >> FootprintBitCount);
    public IReadOnlyList<Vector2Int> TileOffsets => UnpackOccupiedOffsets();

    public const int GridSize = 5;
    public const int FootprintBitCount = GridSize * GridSize;
    const int FootprintMask = (1 << FootprintBitCount) - 1;
    readonly List<Vector2Int> cachedOffsets = new List<Vector2Int>();
    int cachedPackedData = int.MinValue;

    public Dictionary<Vector2Int, TileData?> UnpackToTileDictionary()
    {
        Dictionary<Vector2Int, TileData?> tilesByCoord = new Dictionary<Vector2Int, TileData?>();
        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                Vector2Int localCoord = new Vector2Int(x, y);
                tilesByCoord[localCoord] = IsOccupied(localCoord) ? TileData.WhiteDefault : null;
            }
        }

        return tilesByCoord;
    }

    public bool IsOccupied(Vector2Int localCoord)
    {
        if (localCoord.x < 0 || localCoord.x >= GridSize || localCoord.y < 0 || localCoord.y >= GridSize)
        {
            return false;
        }

        int bitIndex = ToBitIndex(localCoord);
        return (packedShapeData & (1 << bitIndex)) != 0;
    }

    public void SetPackedData(int footprintBits, int reservedBits = 0)
    {
        int clampedFootprintBits = footprintBits & FootprintMask;
        packedShapeData = (reservedBits << FootprintBitCount) | clampedFootprintBits;
        cachedPackedData = int.MinValue;
    }

    IReadOnlyList<Vector2Int> UnpackOccupiedOffsets()
    {
        if (cachedPackedData == packedShapeData)
        {
            return cachedOffsets;
        }

        cachedOffsets.Clear();
        int footprintBits = packedShapeData & FootprintMask;
        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                int bitIndex = y * GridSize + x;
                if ((footprintBits & (1 << bitIndex)) != 0)
                {
                    cachedOffsets.Add(new Vector2Int(x, y));
                }
            }
        }

        cachedPackedData = packedShapeData;
        return cachedOffsets;
    }

    static int ToBitIndex(Vector2Int localCoord)
    {
        return localCoord.y * GridSize + localCoord.x;
    }
}
