using UnityEngine;
using System.Collections.Generic;

public struct BoardState
{
    public const int Size = BlockBlastConstants.BoardSize;

    // Occupancy is packed one cell per bit into a ulong, so the grid cannot exceed
    // 64 cells. If BlockBlastConstants.BoardSize is raised past 8 this const goes
    // negative and fails to compile as a ulong, rather than silently truncating.
    const ulong SpareBits = 64 - (Size * Size);

    public readonly ulong OccupiedBits;

    public BoardState(ulong occupiedBits)
    {
        OccupiedBits = occupiedBits;
    }

    public static BoardState Empty => new BoardState(0UL);

    public static BoardState FromBoard(Board board)
    {
        if (board == null)
        {
            return Empty;
        }

        ulong occupiedBits = 0UL;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!board.TryGetCell(coord, out BoardCell boardCell) || !boardCell.IsOccupied)
                {
                    continue;
                }

                int bitIndex = ToBitIndex(x, y);
                occupiedBits |= 1UL << bitIndex;
            }
        }

        return new BoardState(occupiedBits);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    public bool IsInBounds(Vector2Int coord)
    {
        return IsInBounds(coord.x, coord.y);
    }

    public bool IsOccupied(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return false;
        }

        int bitIndex = ToBitIndex(x, y);
        return (OccupiedBits & (1UL << bitIndex)) != 0;
    }

    public bool IsOccupied(Vector2Int coord)
    {
        return IsOccupied(coord.x, coord.y);
    }

    public BoardState WithCellOccupied(int x, int y, bool occupied)
    {
        if (!IsInBounds(x, y))
        {
            return this;
        }

        int bitIndex = ToBitIndex(x, y);
        ulong mask = 1UL << bitIndex;
        ulong nextBits = occupied ? OccupiedBits | mask : OccupiedBits & ~mask;
        return new BoardState(nextBits);
    }

    public BoardState WithCellOccupied(Vector2Int coord, bool occupied)
    {
        return WithCellOccupied(coord.x, coord.y, occupied);
    }

    public HashSet<Vector2Int> GetCompletedLineCells(out int completedLineCount)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        completedLineCount = 0;

        for (int y = 0; y < Size; y++)
        {
            bool isFullRow = true;
            for (int x = 0; x < Size; x++)
            {
                if (!IsOccupied(x, y))
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
            {
                completedLineCount++;
                for (int x = 0; x < Size; x++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int x = 0; x < Size; x++)
        {
            bool isFullColumn = true;
            for (int y = 0; y < Size; y++)
            {
                if (!IsOccupied(x, y))
                {
                    isFullColumn = false;
                    break;
                }
            }

            if (isFullColumn)
            {
                completedLineCount++;
                for (int y = 0; y < Size; y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        return cells;
    }

    static int ToBitIndex(int x, int y)
    {
        return y * Size + x;
    }
}
