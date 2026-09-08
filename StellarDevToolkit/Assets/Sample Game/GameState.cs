using System.Collections.Generic;

public enum GamePhase
{
    NotStarted,
    WaitingForDrag,
    DraggingShape,
    ResolvingPlacement,
    GameOver
}

public class GameState
{
    public GamePhase Phase;
    public int Score;
    public int CurrentStreak;
    public uint GameSeed;
    public IntegerRng Random;
    public readonly List<byte> PackedMoves;
    public int[] TrominoPackedShapes;
    public int[] TetrominoPackedShapes;

    public GameState(uint seed)
    {
        Phase = GamePhase.WaitingForDrag;
        Score = 0;
        CurrentStreak = 0;
        GameSeed = seed;
        Random = new IntegerRng(seed);
        PackedMoves = new List<byte>();
        TrominoPackedShapes = BlockBlastConstants.TrominoPackedShapes;
        TetrominoPackedShapes = BlockBlastConstants.TetrominoPackedShapes;
    }

    public int NextIndex(int count)
    {
        return Random.NextIndex(count);
    }
}
