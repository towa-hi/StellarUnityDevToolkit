public static class BlockBlastConstants
{
    public const int BoardSize = 8;
    public const int TraySize = 3;

    // 5x5 footprint bitmasks for centered tetrominoes (all unique rotations).
    public const int TetrominoIHorizontal = 30720;
    public const int TetrominoIVertical = 4329600;
    public const int TetrominoO = 405504;

    public const int TetrominoTUp = 145408;
    public const int TetrominoTRight = 143488;
    public const int TetrominoTDown = 14464;
    public const int TetrominoTLeft = 137344;

    public const int TetrominoSHorizontal = 208896;
    public const int TetrominoSVertical = 274560;
    public const int TetrominoZHorizontal = 399360;
    public const int TetrominoZVertical = 143616;

    public const int TetrominoJUp = 79872;
    public const int TetrominoJRight = 397440;
    public const int TetrominoJDown = 14592;
    public const int TetrominoJLeft = 135360;

    public const int TetrominoLUp = 276480;
    public const int TetrominoLRight = 135552;
    public const int TetrominoLDown = 14400;
    public const int TetrominoLLeft = 200832;

    // 5x5 footprint bitmasks for centered trominoes (all unique rotations).
    public const int TrominoIHorizontal = 14336;
    public const int TrominoIVertical = 135296;

    public const int TrominoLUp = 143360;
    public const int TrominoLRight = 12416;
    public const int TrominoLDown = 6272;
    public const int TrominoLLeft = 137216;

    public static readonly int[] TrominoPackedShapes =
    {
        TrominoIHorizontal,
        TrominoIVertical,
        TrominoLUp,
        TrominoLRight,
        TrominoLDown,
        TrominoLLeft
    };

    public static readonly int[] TetrominoPackedShapes =
    {
        TetrominoIHorizontal,
        TetrominoIVertical,
        TetrominoO,
        TetrominoTUp,
        TetrominoTRight,
        TetrominoTDown,
        TetrominoTLeft,
        TetrominoSHorizontal,
        TetrominoSVertical,
        TetrominoZHorizontal,
        TetrominoZVertical,
        TetrominoJUp,
        TetrominoJRight,
        TetrominoJDown,
        TetrominoJLeft,
        TetrominoLUp,
        TetrominoLRight,
        TetrominoLDown,
        TetrominoLLeft
    };
}
