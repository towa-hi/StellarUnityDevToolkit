using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class ShapeTray : MonoBehaviour
{
    [SerializeField] ShapeDefinition definition = null;
    [SerializeField] GameObject tilePrefab = null;
    [SerializeField] MeshCollider hitbox = null;
    [Range(0.1f, 1.0f)]
    [SerializeField] float slotScaleMultiplier = 0.55f;
    [Range(0.0f, 1.0f)]
    [SerializeField] float idleAlpha = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] float dragAlpha = 0.55f;

    public ShapeDefinition Definition => definition;
    public GameObject TilePrefab => tilePrefab;
    public MeshCollider Hitbox => hitbox;
    public IReadOnlyDictionary<Vector2Int, Tile> TilesByLocalCoord => tilesByLocalCoord;
    public ShapeOfferSlot OwnerSlot { get; private set; }

    const int FootprintMask = (1 << ShapeDefinition.FootprintBitCount) - 1;
    readonly Dictionary<Vector2Int, Tile> tilesByLocalCoord = new Dictionary<Vector2Int, Tile>();
    readonly List<Renderer> cachedRenderers = new List<Renderer>();
    readonly Dictionary<Renderer, Color> rendererBaseColors = new Dictionary<Renderer, Color>();
    MaterialPropertyBlock alphaPropertyBlock = null;
    Vector3 fullScale = Vector3.one;
    Tween slotPoseTween = null;
    bool ownsDefinition = false;

    void Awake()
    {
        fullScale = transform.localScale;
        BuildTilesFromDefinition();
        RebuildTileDictionaryFromChildren();
        CacheRenderers();
    }

    void OnValidate()
    {
        slotScaleMultiplier = Mathf.Clamp(slotScaleMultiplier, 0.1f, 1.0f);
        idleAlpha = Mathf.Clamp01(idleAlpha);
        dragAlpha = Mathf.Clamp01(dragAlpha);
        RebuildTileDictionaryFromChildren();
        CacheRenderers();
    }

    public void SetOwnerSlot(ShapeOfferSlot ownerSlot)
    {
        OwnerSlot = ownerSlot;
    }

    public void InitializeFromPackedShapeData(int packedShapeData)
    {
        int footprintBits = packedShapeData & FootprintMask;

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ShapeDefinition>();
            ownsDefinition = true;
        }

        definition.SetPackedData(footprintBits);
        BuildTilesFromDefinition();
        RebuildTileDictionaryFromChildren();
        CacheRenderers();
    }

    public void SnapToSlotPose()
    {
        KillSlotPoseTween();
        Transform slotAnchor = OwnerSlot != null ? OwnerSlot.GetSlotAnchor() : transform.parent;
        if (slotAnchor == null)
        {
            return;
        }

        transform.SetParent(slotAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = GetTargetSlotScale();
        SetAlpha(idleAlpha);
    }

    public Tween LerpToSlotPose(float duration)
    {
        KillSlotPoseTween();
        Transform slotAnchor = OwnerSlot != null ? OwnerSlot.GetSlotAnchor() : transform.parent;
        if (slotAnchor == null)
        {
            return null;
        }

        if (duration <= 0.0f)
        {
            SnapToSlotPose();
            return null;
        }

        transform.SetParent(slotAnchor, true);
        slotPoseTween = DOTween.Sequence()
            .Join(transform.DOLocalMove(Vector3.zero, duration))
            .Join(transform.DOLocalRotateQuaternion(Quaternion.identity, duration))
            .Join(transform.DOScale(GetTargetSlotScale(), duration))
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
        return slotPoseTween;
    }

    public void EnterDragVisualState()
    {
        KillSlotPoseTween();
        transform.localScale = fullScale;
        SetAlpha(dragAlpha);
    }

    public void ExitDragVisualState()
    {
        SetAlpha(idleAlpha);
    }

    public void SetWorldDragPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    public void RebuildTileDictionaryFromChildren()
    {
        tilesByLocalCoord.Clear();
        Tile[] tileComponents = GetComponentsInChildren<Tile>(true);
        foreach (Tile tile in tileComponents)
        {
            Vector3 localPos = transform.InverseTransformPoint(tile.transform.position);
            Vector2Int localCoord = GameUtility.GetLocalShapeCoord(localPos);
            tilesByLocalCoord[localCoord] = tile;
        }
    }

    public void BuildTilesFromDefinition()
    {
        if (definition == null || tilePrefab == null)
        {
            return;
        }

        ClearExistingTiles();
        Dictionary<Vector2Int, TileData?> unpackedTilesByCoord = definition.UnpackToTileDictionary();
        float centerOffset = GameUtility.GetShapeGridCenterOffset();
        foreach (KeyValuePair<Vector2Int, TileData?> entry in unpackedTilesByCoord)
        {
            if (!entry.Value.HasValue)
            {
                continue;
            }

            Vector2Int tileOffset = entry.Key;
            GameObject tileObject = Instantiate(tilePrefab, transform);
            Tile tile = tileObject.GetComponent<Tile>();
            if (tile == null)
            {
                tile = tileObject.AddComponent<Tile>();
            }

            tile.transform.localPosition = new Vector3(tileOffset.x - centerOffset, tileOffset.y - centerOffset, 0.0f);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
        }
    }

    public bool TryGetTile(Vector2Int localCoord, out Tile tile)
    {
        return tilesByLocalCoord.TryGetValue(localCoord, out tile);
    }

    void OnDisable()
    {
        KillSlotPoseTween();
    }

    void OnDestroy()
    {
        // Only definitions this tray created at runtime are ours to destroy; a
        // designer-assigned ShapeDefinition is a shared project asset.
        if (ownsDefinition && definition != null)
        {
            Destroy(definition);
        }
    }

    Vector3 GetTargetSlotScale()
    {
        float ownerMultiplier = OwnerSlot != null ? OwnerSlot.ShapeScaleMultiplier : 1.0f;
        return fullScale * slotScaleMultiplier * ownerMultiplier;
    }

    void KillSlotPoseTween()
    {
        if (slotPoseTween != null && slotPoseTween.IsActive())
        {
            slotPoseTween.Kill();
        }

        slotPoseTween = null;
    }

    void CacheRenderers()
    {
        cachedRenderers.Clear();
        rendererBaseColors.Clear();
        foreach (Tile tile in tilesByLocalCoord.Values)
        {
            if (tile == null)
            {
                continue;
            }

            Renderer renderer = tile.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                cachedRenderers.Add(renderer);
                Material sharedMaterial = renderer.sharedMaterial;
                rendererBaseColors[renderer] = sharedMaterial != null ? sharedMaterial.color : Color.white;
            }
        }
    }

    void ClearExistingTiles()
    {
        Tile[] existingTiles = GetComponentsInChildren<Tile>(true);
        foreach (Tile existingTile in existingTiles)
        {
            if (existingTile == null)
            {
                continue;
            }

            Destroy(existingTile.gameObject);
        }
    }

    void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        if (alphaPropertyBlock == null)
        {
            alphaPropertyBlock = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Color color = rendererBaseColors.TryGetValue(renderer, out Color baseColor) ? baseColor : Color.white;
            color.a = alpha;
            renderer.GetPropertyBlock(alphaPropertyBlock);
            TintProperties.Apply(alphaPropertyBlock, color);
            renderer.SetPropertyBlock(alphaPropertyBlock);
        }
    }
}
