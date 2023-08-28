using System;
using Unity.Collections;
using UnityEngine;

public class Board<T> : IDisposable where T : struct
{
    NativeArray<T> Cells;

    public ref NativeArray<T> GetCells() => ref Cells;
    public void SetCells(NativeArray<T> data)
    {
        Dispose();
        Cells = data;
        Disposed = false;
    }

    public readonly Vector2Int Size;
    public readonly int BufferSize;

    public readonly bool Spacing;
    readonly int spacingOffset;

    public bool Disposed { get; private set; }

    public T this[int x, int y]
    {
        get => Cells[Index(x, y)];
        set => Cells[Index(x, y)] = value;
    }

    int Index(int x, int y) => (x + spacingOffset) * (Size.y + spacingOffset * 2) + y + spacingOffset;

    public Board(Vector2Int size, bool spacing)
    {
        Spacing = spacing;
        spacingOffset = Spacing ? 0 : 1;

        Size = size;
        BufferSize = (Size.x + spacingOffset * 2) * (Size.y + spacingOffset * 2);

        Cells = new NativeArray<T>(BufferSize, Allocator.Persistent);
        Disposed = false;
    }

    public void Dispose()
    {
        if (Disposed == false)
        {
            Cells.Dispose();
            Disposed = true;
        }
    }
}
