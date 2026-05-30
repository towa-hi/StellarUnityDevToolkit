using UnityEngine;
using System.Collections.Generic;

public class ShapeOfferArea : MonoBehaviour
{
    [SerializeField] List<ShapeOfferSlot> offerSlots = new List<ShapeOfferSlot>();

    public IReadOnlyList<ShapeOfferSlot> OfferSlots => offerSlots;
    public int TraySize => offerSlots.Count;

    void OnValidate()
    {
        while (offerSlots.Count < BlockBlastConstants.TraySize)
        {
            offerSlots.Add(null);
        }

        while (offerSlots.Count > BlockBlastConstants.TraySize)
        {
            offerSlots.RemoveAt(offerSlots.Count - 1);
        }
    }
}
