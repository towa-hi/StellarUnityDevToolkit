using UnityEngine;

public class BoardCell : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    public bool IsOccupied { get; private set; }
    public Tile OccupiedTile => tile;

    [SerializeField] Tile tile = null;
    [SerializeField] GameObject background = null;
    [SerializeField] MeshCollider hitbox = null;
    [SerializeField] Color idleBackgroundColor = Color.white;
    [SerializeField] Color hoverBackgroundColor = Color.red;

    Renderer backgroundRenderer = null;
    MaterialPropertyBlock backgroundPropertyBlock = null;
    bool isPreviewHighlighted;

    void Awake()
    {
        CacheBackgroundRenderer();
        ApplyBackgroundColor(idleBackgroundColor);
    }

    void OnValidate()
    {
        CacheBackgroundRenderer();
        if (!isPreviewHighlighted)
        {
            ApplyBackgroundColor(idleBackgroundColor);
        }
    }

    public void Initialize(Vector2Int coord)
    {
        Coord = coord;
        IsOccupied = false;
        isPreviewHighlighted = false;
        tile = null;
        ApplyBackgroundColor(idleBackgroundColor);
    }

    public void SetOccupiedState(bool occupied, Tile occupiedTile = null)
    {
        IsOccupied = occupied;
        tile = occupied ? occupiedTile : null;
    }

    public void SetPreviewHighlight(bool highlighted)
    {
        isPreviewHighlighted = highlighted;
        ApplyBackgroundColor(highlighted ? hoverBackgroundColor : idleBackgroundColor);
    }

    public MeshCollider GetHitbox()
    {
        return hitbox;
    }

    void CacheBackgroundRenderer()
    {
        if (background != null)
        {
            backgroundRenderer = background.GetComponent<Renderer>();
        }
    }

    void ApplyBackgroundColor(Color color)
    {
        if (backgroundRenderer == null)
        {
            return;
        }

        if (backgroundPropertyBlock == null)
        {
            backgroundPropertyBlock = new MaterialPropertyBlock();
        }

        backgroundRenderer.GetPropertyBlock(backgroundPropertyBlock);
        backgroundPropertyBlock.SetColor("_BaseColor", color);
        backgroundPropertyBlock.SetColor("_Color", color);
        backgroundRenderer.SetPropertyBlock(backgroundPropertyBlock);
    }
}
