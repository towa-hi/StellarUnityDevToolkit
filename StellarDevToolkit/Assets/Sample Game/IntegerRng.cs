// Deterministic xorshift32 generator, so a game can be replayed from its seed.
public struct IntegerRng
{
    const uint FallbackSeed = 0x9E3779B9u;

    public uint State;

    public IntegerRng(uint seed)
    {
        State = seed == 0u ? FallbackSeed : seed;
    }

    public uint NextU32()
    {
        uint x = State;
        if (x == 0u)
        {
            x = FallbackSeed;
        }

        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        State = x;
        return x;
    }

    public int NextIndex(int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (int)(NextU32() % (uint)count);
    }
}
