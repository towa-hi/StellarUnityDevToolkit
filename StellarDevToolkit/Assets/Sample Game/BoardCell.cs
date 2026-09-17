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
    [SerializeField] Color alternateBackgroundColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] Color hoverBackgroundColor = Color.red;

    Renderer backgroundRenderer = null;
    MaterialPropertyBlock backgroundPropertyBlock = null;
    bool isPreviewHighlighted;

    Color CheckerBackgroundColor => ((Coord.x + Coord.y) & 1) == 0
        ? idleBackgroundColor
        : alternateBackgroundColor;

    void Awake()
    {
        CacheBackgroundRenderer();
        ApplyBackgroundColor(CheckerBackgroundColor);
    }

    void OnValidate()
    {
        CacheBackgroundRenderer();
        if (!isPreviewHighlighted)
        {
            ApplyBackgroundColor(CheckerBackgroundColor);
        }
    }

    public void Initialize(Vector2Int coord)
    {
        Coord = coord;
        IsOccupied = false;
        isPreviewHighlighted = false;
        tile = null;
        ApplyBackgroundColor(CheckerBackgroundColor);
    }

    public void SetOccupiedState(bool occupied, Tile occupiedTile = null)
    {
        IsOccupied = occupied;
        tile = occupied ? occupiedTile : null;
    }

    public void SetPreviewHighlight(bool highlighted)
    {
        isPreviewHighlighted = highlighted;
        ApplyBackgroundColor(highlighted ? hoverBackgroundColor : CheckerBackgroundColor);
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
        TintProperties.Apply(backgroundPropertyBlock, color);
        backgroundRenderer.SetPropertyBlock(backgroundPropertyBlock);
    }
}
