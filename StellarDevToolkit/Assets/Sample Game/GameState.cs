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

    public GameState(uint seed)
    {
        Phase = GamePhase.WaitingForDrag;
        Score = 0;
        CurrentStreak = 0;
        GameSeed = seed;
        Random = new IntegerRng(seed);
    }

    public int NextIndex(int count)
    {
        return Random.NextIndex(count);
    }
}
