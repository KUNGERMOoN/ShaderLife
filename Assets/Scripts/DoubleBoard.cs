using System;
using Unity.Collections;

public class DoubleBoard<T> : IDisposable where T : struct
{
    readonly Board<T> boardA;
    readonly Board<T> boardB;

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
        CurrentBoard = Flipped ? boardB : boardA;
    }

    public T this[int x, int y]
    {
        get => CurrentBoard[x, y];
        set => CurrentBoard[x, y] = value;
    }

    public ref NativeArray<T> GetCells() => ref CurrentBoard.GetCells();
    public void SetCells(NativeArray<T> data) => CurrentBoard.SetCells(data);

    public DoubleBoard(int sizeExponent, bool spacing)
    {
        Spacing = spacing;
        spacingOffset = Spacing ? 0 : 1;

        SizeExponent = sizeExponent;
        Size = 1 << SizeExponent;
        BufferSize = (Size + spacingOffset * 2) * (Size + spacingOffset * 2);

        boardA = new Board<T>(SizeExponent, Spacing);
        boardB = new Board<T>(SizeExponent, Spacing);

        Flipped = false;
        CurrentBoard = boardA;
    }

    public void Dispose()
    {
        boardA.Dispose();
        boardB.Dispose();
    }
}
