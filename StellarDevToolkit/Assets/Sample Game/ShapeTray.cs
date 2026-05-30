using UnityEngine;
using System.Collections.Generic;

public class ShapeTray : MonoBehaviour
{
    [SerializeField] ShapeDefinition definition = null;
    [SerializeField] bool initializeFromPackedShapeDataOnAwake = false;
    [SerializeField] int initialPackedShapeData = 0;
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
    static readonly Vector2 GridCenterOffset = new Vector2((ShapeDefinition.GridSize - 1) * 0.5f, (ShapeDefinition.GridSize - 1) * 0.5f);
    readonly Dictionary<Vector2Int, Tile> tilesByLocalCoord = new Dictionary<Vector2Int, Tile>();
    readonly List<Renderer> cachedRenderers = new List<Renderer>();
    Vector3 fullScale = Vector3.one;

    void Awake()
    {
        fullScale = transform.localScale;
        if (initializeFromPackedShapeDataOnAwake)
        {
            InitializeFromPackedShapeData(initialPackedShapeData);
        }
        else
        {
            BuildTilesFromDefinition();
        }

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
        }

        definition.SetPackedData(footprintBits);
        BuildTilesFromDefinition();
        RebuildTileDictionaryFromChildren();
        CacheRenderers();
    }

    public void SnapToSlotPose()
    {
        Transform slotAnchor = OwnerSlot != null ? OwnerSlot.GetSlotAnchor() : transform.parent;
        if (slotAnchor == null)
        {
            return;
        }

        transform.SetParent(slotAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = fullScale * slotScaleMultiplier;
        SetAlpha(idleAlpha);
    }

    public void EnterDragVisualState()
    {
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
            Vector2Int localCoord = new Vector2Int(
                Mathf.RoundToInt(localPos.x + GridCenterOffset.x),
                Mathf.RoundToInt(localPos.y + GridCenterOffset.y));
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

            tile.transform.localPosition = new Vector3(tileOffset.x - GridCenterOffset.x, tileOffset.y - GridCenterOffset.y, 0.0f);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
        }
    }

    public bool TryGetTile(Vector2Int localCoord, out Tile tile)
    {
        return tilesByLocalCoord.TryGetValue(localCoord, out tile);
    }

    void CacheRenderers()
    {
        cachedRenderers.Clear();
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
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }
    }
}
