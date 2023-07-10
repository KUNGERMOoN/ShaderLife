using System;
using Unity.Collections;

public class FlipBoard<T> : IDisposable where T : struct
{
    readonly Board<T> board;
    readonly Board<T> flippedBoard;

    Board<T> CurrentBoard;

    public readonly int SizeExponent;
    public readonly int Size;
    public readonly int BufferSize;

    public readonly bool Spacing;
    readonly int spacingOffset;

    public bool Flipped { get; private set; }
    public void Flip()
    {
        Flipped = !Flipped;
        CurrentBoard = Flipped ? flippedBoard : board;
    }

    public T this[int x, int y]
    {
        get => CurrentBoard[x, y];
        set => CurrentBoard[x, y] = value;
    }

    public ref NativeArray<T> GetCells() => ref CurrentBoard.GetCells();
    public void SetCells(NativeArray<T> data) => CurrentBoard.SetCells(data);

    public FlipBoard(int sizeExponent, bool spacing)
    {
        Spacing = spacing;
        spacingOffset = Spacing ? 0 : 1;

        SizeExponent = sizeExponent;
        Size = 1 << SizeExponent;
        BufferSize = (Size + spacingOffset * 2) * (Size + spacingOffset * 2);

        board = new Board<T>(SizeExponent, Spacing);
        flippedBoard = new Board<T>(SizeExponent, Spacing);

        Flipped = false;
        CurrentBoard = board;
    }

    public void Dispose()
    {
        board.Dispose();
        flippedBoard.Dispose();
    }
}
