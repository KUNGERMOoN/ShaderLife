using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public static class Utils
{
    public static float ToFloat(this StyleLength styleLength)
        => styleLength.value.value;

    public static bool IsValidFileName(string fileName, string absolutePath) =>
        !string.IsNullOrEmpty(fileName) &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !File.Exists(Path.Combine(absolutePath, fileName));

    public static string ThousandSpacing(this int number)
    {
        var f = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

        return number.ToString("n", f);
    }

    public static Vector2Int FloorToInt(this Vector2 vector)
        => new Vector2Int(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));

    public static Vector2 Clamp(this Vector2 vector, float min, float max)
        => new Vector2(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max));

    public static Vector2Int Clamp(this Vector2Int vector, int min, int max)
        => new Vector2Int(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max));
}

public class Range : IEnumerable<int>
{
    public readonly int Start, End;
    public int Delta => End - Start;
    public int Sign => Delta < 0 ? -1 : 1;

    public Range(int start, int end)
    {
        Start = start;
        End = end;
    }

    public IEnumerator<int> GetEnumerator() => new RangeEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new RangeEnumerator(this);
}

public class RangeEnumerator : IEnumerator<int>
{
    private readonly Range range;
    private int i;
    private readonly int sign;

    public RangeEnumerator(Range range)
    {
        this.range = range;
        sign = range.Sign;

        Reset();
    }

    public int Current => i;

    public bool MoveNext()
    {
        i += sign;

        if ((range.End - i) * sign >= 0)
            return true;
        else return false;
    }

    public void Reset() => i = range.Start - sign;

    object IEnumerator.Current => i;
    void IDisposable.Dispose() { }
}