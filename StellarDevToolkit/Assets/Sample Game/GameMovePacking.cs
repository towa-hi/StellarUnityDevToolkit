// Packed move layout (one u64, little-endian bit fields).
// Rust unpack:
//   let shape = packed as u32;
//   let x = ((packed >> 32) as u8) as i8;
//   let y = ((packed >> 40) as u8) as i8;
//
// bits 0-31:  shape type = ShapeDefinition.PackedShapeData (u32)
// bits 32-39: drop x as i8 (two's complement)
// bits 40-47: drop y as i8 (two's complement)
// bits 48-63: reserved 0
public static class GameMovePacking
{
    const int ShapeBitCount = 32;
    const int CoordBitCount = 8;
    const ulong ShapeMask = 0xFFFFFFFFul;
    const ulong CoordMask = 0xFFul;

    public static ulong Pack(int x, int y, int packedShapeData)
    {
        ulong packed = (uint)packedShapeData;
        packed |= (ulong)unchecked((byte)(sbyte)x) << ShapeBitCount;
        packed |= (ulong)unchecked((byte)(sbyte)y) << (ShapeBitCount + CoordBitCount);
        return packed;
    }

    public static void Unpack(ulong packed, out int x, out int y, out int packedShapeData)
    {
        packedShapeData = (int)(packed & ShapeMask);
        x = unchecked((sbyte)((packed >> ShapeBitCount) & CoordMask));
        y = unchecked((sbyte)((packed >> (ShapeBitCount + CoordBitCount)) & CoordMask));
    }
}
