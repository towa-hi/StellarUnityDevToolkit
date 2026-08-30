using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class ShapeOfferArea : MonoBehaviour
{
    [SerializeField] List<ShapeOfferSlot> offerSlots = new List<ShapeOfferSlot>();
    [SerializeField] ShapeTray shapeTrayPrefab = null;
    [SerializeField] List<ShapeOfferSlot> previewSlots = new List<ShapeOfferSlot>();
    [SerializeField] float previewShapeScaleMultiplier = 0.65f;
    [SerializeField] float promoteDuration = 0.22f;

    public IReadOnlyList<ShapeOfferSlot> OfferSlots => offerSlots;
    public IReadOnlyList<ShapeOfferSlot> PreviewSlots => previewSlots;
    public int TraySize => offerSlots.Count;
    public bool IsPromoting { get; private set; }

    Sequence promoteSequence = null;
    Func<int, int[]> packedShapeBatchGenerator = null;

    public void ValidateSceneWiring(MonoBehaviour context)
    {
        if (shapeTrayPrefab == null)
        {
            Debug.LogWarning("ShapeOfferArea: ShapeTray prefab reference is missing.", context);
        }

        ValidateSlotListWiring(offerSlots, "slot", context);
        ValidateSlotListWiring(previewSlots, "preview slot", context);
    }

    public bool CanBeginDragFrom(ShapeOfferSlot slot)
    {
        if (IsPromoting || slot == null || !slot.HasShape())
        {
            return false;
        }

        if (previewSlots.Contains(slot) || slot.IsPreview)
        {
            return false;
        }

        return offerSlots.Contains(slot);
    }

    public void PopulateShapeOfferSlots(Func<int, int[]> packedShapeBatchGenerator)
    {
        this.packedShapeBatchGenerator = packedShapeBatchGenerator;
        KillPromotion();
        ClearAndPopulateSlots(offerSlots);
        ClearAndPopulateSlots(previewSlots);
    }

    public void PopulateEmptyShapeOfferSlots()
    {
        PopulateEmptySlots(offerSlots);
    }

    public void PopulateShapeOfferSlotsIfEmpty()
    {
        if (!AreAllSlotsEmpty(offerSlots))
        {
            return;
        }

        TryPromotePreviewBatch();
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

        if (AreAllSlotsEmpty(offerSlots))
        {
            TryPromotePreviewBatch();
        }
    }

    void PopulateShapeOfferSlot(ShapeOfferSlot slot, int packedShapeData)
    {
        if (slot == null || shapeTrayPrefab == null)
        {
            return;
        }

        Transform slotAnchor = slot.GetSlotAnchor();
        ShapeTray spawnedTray = Instantiate(shapeTrayPrefab, slotAnchor.position, slotAnchor.rotation);
        spawnedTray.InitializeFromPackedShapeData(packedShapeData);
        slot.SetShape(spawnedTray);
    }

    void TryPromotePreviewBatch()
    {
        if (shapeTrayPrefab == null)
        {
            return;
        }

        KillPromotion();

        Sequence sequence = DOTween.Sequence();
        bool anyPromotion = false;
        int pairCount = Mathf.Min(offerSlots.Count, previewSlots.Count);
        for (int i = 0; i < pairCount; i++)
        {
            ShapeOfferSlot previewSlot = previewSlots[i];
            ShapeOfferSlot activeSlot = offerSlots[i];
            if (previewSlot == null || activeSlot == null || !previewSlot.HasShape())
            {
                continue;
            }

            ShapeTray shape = previewSlot.CurrentShape;
            previewSlot.Clear();
            activeSlot.SetShape(shape, snapToPose: false);
            Tween lerpTween = shape.LerpToSlotPose(promoteDuration);
            if (lerpTween != null)
            {
                sequence.Join(lerpTween);
                anyPromotion = true;
            }
        }

        PopulateEmptySlots(offerSlots);

        if (!anyPromotion)
        {
            PopulateEmptySlots(previewSlots);
            return;
        }

        IsPromoting = true;
        promoteSequence = sequence
            .OnComplete(HandlePromotionCompleted)
            .OnKill(HandlePromotionKilled);
    }

    void HandlePromotionCompleted()
    {
        IsPromoting = false;
        promoteSequence = null;
        PopulateEmptySlots(previewSlots);
    }

    void HandlePromotionKilled()
    {
        IsPromoting = false;
        promoteSequence = null;
    }

    void KillPromotion()
    {
        if (promoteSequence != null && promoteSequence.IsActive())
        {
            promoteSequence.Kill();
        }

        promoteSequence = null;
        IsPromoting = false;
    }

    void ClearAndPopulateSlots(List<ShapeOfferSlot> slots)
    {
        if (shapeTrayPrefab == null)
        {
            return;
        }

        List<ShapeOfferSlot> slotsToFill = new List<ShapeOfferSlot>();
        foreach (ShapeOfferSlot slot in slots)
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

            slotsToFill.Add(slot);
        }

        PopulateSlotsWithBatch(slotsToFill);
    }

    void PopulateEmptySlots(List<ShapeOfferSlot> slots)
    {
        if (shapeTrayPrefab == null)
        {
            return;
        }

        List<ShapeOfferSlot> slotsToFill = new List<ShapeOfferSlot>();
        foreach (ShapeOfferSlot slot in slots)
        {
            if (slot == null || slot.HasShape())
            {
                continue;
            }

            slotsToFill.Add(slot);
        }

        PopulateSlotsWithBatch(slotsToFill);
    }

    void PopulateSlotsWithBatch(List<ShapeOfferSlot> slots)
    {
        if (slots == null || slots.Count == 0)
        {
            return;
        }

        int[] packedShapes = GeneratePackedShapeBatch(slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            int packedShapeData = packedShapes != null && i < packedShapes.Length
                ? packedShapes[i]
                : 0;
            PopulateShapeOfferSlot(slots[i], packedShapeData);
        }
    }

    bool AreAllSlotsEmpty(List<ShapeOfferSlot> slots)
    {
        foreach (ShapeOfferSlot slot in slots)
        {
            if (slot != null && slot.HasShape())
            {
                return false;
            }
        }

        return true;
    }

    int[] GeneratePackedShapeBatch(int count)
    {
        if (packedShapeBatchGenerator == null)
        {
            Debug.LogWarning("ShapeOfferArea: Packed shape batch generator is missing.", this);
            return new int[Mathf.Max(0, count)];
        }

        int[] packedShapes = packedShapeBatchGenerator(count);
        if (packedShapes == null)
        {
            return new int[Mathf.Max(0, count)];
        }

        return packedShapes;
    }

    void ValidateSlotListWiring(List<ShapeOfferSlot> slots, string slotLabel, MonoBehaviour context)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogWarning($"ShapeOfferArea: {slotLabel} {i} is missing.", context);
            }
        }
    }

    void OnDisable()
    {
        KillPromotion();
    }

    void OnValidate()
    {
        previewShapeScaleMultiplier = Mathf.Max(0.1f, previewShapeScaleMultiplier);
        promoteDuration = Mathf.Max(0.0f, promoteDuration);
        EnsureSlotListSize(offerSlots);
        EnsureSlotListSize(previewSlots);
        ApplySlotRoles(offerSlots, false, 1.0f);
        ApplySlotRoles(previewSlots, true, previewShapeScaleMultiplier);
    }

    static void EnsureSlotListSize(List<ShapeOfferSlot> slots)
    {
        while (slots.Count < BlockBlastConstants.TraySize)
        {
            slots.Add(null);
        }

        while (slots.Count > BlockBlastConstants.TraySize)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    static void ApplySlotRoles(List<ShapeOfferSlot> slots, bool isPreview, float scaleMultiplier)
    {
        foreach (ShapeOfferSlot slot in slots)
        {
            if (slot != null)
            {
                slot.ConfigureRole(isPreview, scaleMultiplier);
            }
        }
    }
}
