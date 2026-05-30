using UnityEngine;
using System.Collections.Generic;

public class ShapeOfferArea : MonoBehaviour
{
    [SerializeField] List<ShapeOfferSlot> offerSlots = new List<ShapeOfferSlot>();
    [SerializeField] ShapeTray shapeTrayPrefab = null;

    public IReadOnlyList<ShapeOfferSlot> OfferSlots => offerSlots;
    public int TraySize => offerSlots.Count;

    public void ValidateSceneWiring(MonoBehaviour context)
    {
        if (shapeTrayPrefab == null)
        {
            Debug.LogWarning("ShapeOfferArea: ShapeTray prefab reference is missing.", context);
        }

        for (int i = 0; i < offerSlots.Count; i++)
        {
            if (offerSlots[i] == null)
            {
                Debug.LogWarning($"ShapeOfferArea: slot {i} is missing.", context);
            }
        }
    }

    public void PopulateShapeOfferSlots()
    {
        if (shapeTrayPrefab == null)
        {
            return;
        }

        foreach (ShapeOfferSlot slot in offerSlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.HasShape())
            {
                Destroy(slot.CurrentShape.gameObject);
                slot.Clear();
            }

            PopulateShapeOfferSlot(slot);
        }
    }

    public void PopulateEmptyShapeOfferSlots()
    {
        if (shapeTrayPrefab == null)
        {
            return;
        }

        foreach (ShapeOfferSlot slot in offerSlots)
        {
            if (slot == null || slot.HasShape())
            {
                continue;
            }

            PopulateShapeOfferSlot(slot);
        }
    }

    public void PopulateShapeOfferSlotsIfEmpty()
    {
        foreach (ShapeOfferSlot slot in offerSlots)
        {
            if (slot != null && slot.HasShape())
            {
                return;
            }
        }

        PopulateEmptyShapeOfferSlots();
    }

    public void ConsumePlacedShape(ShapeOfferSlot slot, ShapeTray placedShape)
    {
        if (slot != null)
        {
            slot.Clear();
        }

        if (placedShape != null)
        {
            Destroy(placedShape.gameObject);
        }

        PopulateShapeOfferSlotsIfEmpty();
    }

    void PopulateShapeOfferSlot(ShapeOfferSlot slot)
    {
        if (slot == null || shapeTrayPrefab == null)
        {
            return;
        }

        Transform slotAnchor = slot.GetSlotAnchor();
        ShapeTray spawnedTray = Instantiate(shapeTrayPrefab, slotAnchor.position, slotAnchor.rotation);
        int randomPackedShapeData = GenerateRandomPackedShapeData();
        spawnedTray.InitializeFromPackedShapeData(randomPackedShapeData);
        slot.SetShape(spawnedTray);
    }

    int GenerateRandomPackedShapeData()
    {
        int[] tetrominoes = BlockBlastConstants.TetrominoPackedShapes;
        if (tetrominoes == null || tetrominoes.Length == 0)
        {
            return 0;
        }

        int randomIndex = Random.Range(0, tetrominoes.Length);
        return tetrominoes[randomIndex];
    }

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
