using UnityEngine;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject slotPrefab = null;
    readonly Dictionary<Vector2Int, BoardCell> boardCells = new Dictionary<Vector2Int, BoardCell>();
    readonly HashSet<BoardCell> highlightedBoardCells = new HashSet<BoardCell>();

    public void InitializeBoard(Vector2Int size)
    {
        ClearPreviewHighlights();
        if (slotPrefab == null)
        {
            Debug.LogWarning("Board: slotPrefab is missing.", this);
            return;
        }

        if (size.x > BoardState.Size || size.y > BoardState.Size)
        {
            Debug.LogWarning($"Board: size {size} exceeds the {BoardState.Size}x{BoardState.Size} grid BoardState can represent.", this);
            return;
        }

        foreach (KeyValuePair<Vector2Int, BoardCell> boardCell in boardCells)
        {
            Destroy(boardCell.Value.gameObject);
        }

        boardCells.Clear();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                GameObject slot = Instantiate(slotPrefab, transform);
                slot.transform.position = new Vector3(x, y, 0);
                BoardCell boardCell = slot.GetComponent<BoardCell>();
                if (boardCell == null)
                {
                    continue;
                }

                boardCell.Initialize(new Vector2Int(x, y));
                boardCells[new Vector2Int(x, y)] = boardCell;
            }
        }
    }

    public void SetPreviewHighlights(IReadOnlyCollection<Vector2Int> coords)
    {
        if (coords == null || coords.Count == 0)
        {
            ClearPreviewHighlights();
            return;
        }

        HashSet<BoardCell> nextHighlightedBoardCells = new HashSet<BoardCell>();
        foreach (Vector2Int coord in coords)
        {
            if (!boardCells.TryGetValue(coord, out BoardCell boardCell) || boardCell == null)
            {
                ClearPreviewHighlights();
                return;
            }

            nextHighlightedBoardCells.Add(boardCell);
        }

        bool isSamePreview =
            nextHighlightedBoardCells.Count == highlightedBoardCells.Count &&
            nextHighlightedBoardCells.IsSubsetOf(highlightedBoardCells);
        if (isSamePreview)
        {
            return;
        }

        foreach (BoardCell boardCell in highlightedBoardCells)
        {
            if (!nextHighlightedBoardCells.Contains(boardCell))
            {
                boardCell.SetPreviewHighlight(false);
            }
        }

        foreach (BoardCell boardCell in nextHighlightedBoardCells)
        {
            if (!highlightedBoardCells.Contains(boardCell))
            {
                boardCell.SetPreviewHighlight(true);
            }
        }

        highlightedBoardCells.Clear();
        highlightedBoardCells.UnionWith(nextHighlightedBoardCells);
    }

    public void ClearPreviewHighlights()
    {
        if (highlightedBoardCells.Count == 0)
        {
            return;
        }

        foreach (BoardCell boardCell in highlightedBoardCells)
        {
            if (boardCell != null)
            {
                boardCell.SetPreviewHighlight(false);
            }
        }

        highlightedBoardCells.Clear();
    }

    public bool TryGetCell(Vector2Int coord, out BoardCell boardCell)
    {
        if (boardCells.TryGetValue(coord, out boardCell) && boardCell != null)
        {
            return true;
        }

        boardCell = null;
        return false;
    }
}
