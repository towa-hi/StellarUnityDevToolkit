using UnityEngine;

public class RowContainer : MonoBehaviour
{
    public ArrowContainer leftArrowContainer;
    public ArrowContainer rightArrowContainer;

    public void ClearRow()
    {
        leftArrowContainer.SetArrowVisible(false);
        rightArrowContainer.SetArrowVisible(false);
    }
    public void SetRow(Column start, Column end, string label)
    {
        bool longArrow = false;
        bool isLeft = false;
        if (start == Column.Client)
        {
            if (end == Column.Wallet)
            {
                longArrow = false;
                isLeft = false;
            }
            else if (end == Column.Stellar)
            {
                longArrow = true;
                isLeft = false;
            }
        }
        else if (start == Column.Wallet)
        {
            if (end == Column.Client)
            {
                longArrow = false;
                isLeft = true;
            }
            else if (end == Column.Stellar)
            {
                longArrow = false;
                isLeft = false;
            }
        }
        else if (start == Column.Stellar)
        {
            if (end == Column.Client)
            {
                longArrow = true;
                isLeft = true;
            }
            else if (end == Column.Wallet)
            {
                longArrow = false;
                isLeft = true;
            }
        }
        
        leftArrowContainer.SetArrowVisible(true);
        rightArrowContainer.gameObject.SetActive(!longArrow);
        leftArrowContainer.SetArrow(isLeft, label);
        rightArrowContainer.SetArrow(isLeft, label);
    }
}
