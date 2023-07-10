using System;
using Unity.Collections;

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

    public readonly int SizeExponent;
    public readonly int Size;
    public readonly int BufferSize;

    public readonly bool Spacing;
    readonly int spacingOffset;

    public bool Disposed { get; private set; }

    public T this[int x, int y]
    {
        get => Cells[Index(x, y)];
        set => Cells[Index(x, y)] = value;
    }

    int Index(int x, int y) => (x + spacingOffset) * (Size + spacingOffset * 2) + y + spacingOffset;

    public Board(int sizeExponent, bool spacing)
    {
        Spacing = spacing;
        spacingOffset = Spacing ? 0 : 1;

        SizeExponent = sizeExponent;
        Size = 1 << SizeExponent;
        BufferSize = (Size + spacingOffset * 2) * (Size + spacingOffset * 2);

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
