public struct Cell
{
    public int Alive;
    public int FlipAlive;

    public static readonly Cell On;
    public static readonly Cell Off;

    static Cell()
    {
        On = new() { Alive = 1 };

        Off = new() { Alive = 0 };
    }
}
