using UnityEngine;

// Writes both tint properties so the same code works against the built-in render
// pipeline (_Color) and URP/HDRP (_BaseColor).
public static class TintProperties
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public static void Apply(MaterialPropertyBlock propertyBlock, Color color)
    {
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
    }
}
