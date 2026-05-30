using UnityEngine;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject slotPrefab = null;
    public Dictionary<Vector2Int, BoardCell> boardCells = new Dictionary<Vector2Int, BoardCell>();
    
    public void InitializeBoard(Vector2Int size)
    {
        if (slotPrefab == null)
        {
            Debug.LogWarning("Board: slotPrefab is missing.", this);
            return;
        }

        foreach (var boardCell in boardCells)
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
}
