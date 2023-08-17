using NUnit.Framework;
using UnityEngine;

public class LookupTableIndexGeneratorTest
{
    const int chunkRows = LUTBuilder.rows - 2;
    const int chunkColumns = LUTBuilder.columns - 2;

    [Test]
    public void TestSample()
    {
        int[,] nums3x3 = new int[,]
        {
            { 36, 128, 72 },
            { 157, 109, 96 },
            { 235, 205, 120 }
        };

        TestOnConfiguration(nums3x3, out int outputA, out int outputB);

        Assert.AreEqual(outputA, outputB);
    }

    [Test]
    public void TestRandom()
    {
        const int configurations = 1 << (chunkRows * chunkColumns);

        int[,] nums3x3 = new int[3, 3];
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                nums3x3[x, y] = Random.Range(0, configurations);
            }
        }

        TestOnConfiguration(nums3x3, out int outputA, out int outputB);

        Assert.AreEqual(outputA, outputB);
    }

    /*// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator LookupTableIndexGeneratorTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }*/

    static void TestOnConfiguration(int[,] nums3x3, out int outputA, out int outputB)
    {
        //Calculated manually
        bool[,] cells3x3 = new bool[3 * chunkColumns, 3 * chunkRows];

        for (int x_ = 0; x_ < 3; x_++)
        {
            for (int y_ = 0; y_ < 3; y_++)
            {
                bool[,] chunk = LUTBuilder.ToCells(nums3x3[x_, y_],
                    chunkRows,
                    chunkColumns);

                for (int u = 0; u < chunkColumns; u++)
                {
                    for (int v = 0; v < chunkRows; v++)
                    {
                        cells3x3[chunkColumns * x_ + u, chunkRows * y_ + v] = chunk[u, v];
                    }
                }
            }
        }

        bool[,] data = new bool[LUTBuilder.columns, LUTBuilder.rows];

        for (int x_ = 3; x_ < 3 * chunkColumns - 3; x_++)
        {
            for (int y_ = 1; y_ < 3 * chunkRows - 1; y_++)
            {
                data[x_ - 3, y_ - 1] = cells3x3[x_, y_];
            }
        }
        outputA = LUTBuilder.ToNumber(data, LUTBuilder.rows, LUTBuilder.columns);

        int x = nums3x3[1, 1];
        int a = nums3x3[0, 0];
        int b = nums3x3[1, 0];
        int c = nums3x3[2, 0];
        int d = nums3x3[0, 1];
        int e = nums3x3[2, 1];
        int f = nums3x3[0, 2];
        int g = nums3x3[1, 2];
        int h = nums3x3[2, 2];

        outputB =
            ((a << 23) & 0b100000000000000000000000) +
            ((b << 19) & 0b011110000000000000000000) +
            ((c << 15) & 0b000001000000000000000000) +
            ((d << 13) & 0b000000100000000000000000) +
            ((d << 11) & 0b000000000000100000000000) +
            ((e << 05) & 0b000000000001000000000000) +
            ((e << 03) & 0b000000000000000001000000) +
            ((f << 01) & 0b000000000000000000100000) +
            ((g >> 03) & 0b000000000000000000011110) +
            ((h >> 07) & 0b000000000000000000000001) +
            ((x << 09) & 0b000000011110000000000000) +
            ((x << 07) & 0b000000000000011110000000);

    }

    static string DebugConfiguration(bool[,] cells)
    {
        string result = "\n";
        for (int y = cells.GetLength(1) - 1; y >= 0; y--)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                result += (cells[x, y] ? 1 : 0) + " ";
            }
            result += "\n";
        }
        return result;
    }
}
