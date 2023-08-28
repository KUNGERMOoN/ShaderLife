using System;
using Unity.Collections;
using UnityEngine;

public class DoubleBoard<T> : IDisposable where T : struct
{
    readonly Board<T> boardA;
    readonly Board<T> boardB;

    Board<T> CurrentBoard;

    public readonly Vector2Int Size;
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

    public DoubleBoard(Vector2Int size, bool spacing)
    {
        Spacing = spacing;
        spacingOffset = Spacing ? 0 : 1;

        Size = size;
        BufferSize = (Size.x + spacingOffset * 2) * (Size.y + spacingOffset * 2);

        boardA = new Board<T>(size, Spacing);
        boardB = new Board<T>(size, Spacing);

        Flipped = false;
        CurrentBoard = boardA;
    }

    public void Dispose()
    {
        boardA.Dispose();
        boardB.Dispose();
    }
}
