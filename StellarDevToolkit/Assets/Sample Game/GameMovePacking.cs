// Packed move layout (one byte).
// Rust unpack:
//   let tray = packed & 0b11;
//   let x = (packed >> 2) & 0b111;
//   let y = (packed >> 5) & 0b111;
//
// bits 0-1: tray index 0|1|2
// bits 2-4: drop cell x (0-7)
// bits 5-7: drop cell y (0-7)
public static class GameMovePacking
{
    const int TrayBitCount = 2;
    const int CoordBitCount = 3;
    const int TrayMask = 0b11;
    const int CoordMask = 0b111;

    public static byte Pack(int cellX, int cellY, int trayIndex)
    {
        return (byte)((trayIndex & TrayMask)
            | ((cellX & CoordMask) << TrayBitCount)
            | ((cellY & CoordMask) << (TrayBitCount + CoordBitCount)));
    }

    public static void Unpack(byte packed, out int cellX, out int cellY, out int trayIndex)
    {
        trayIndex = packed & TrayMask;
        cellX = (packed >> TrayBitCount) & CoordMask;
        cellY = (packed >> (TrayBitCount + CoordBitCount)) & CoordMask;
    }
}
