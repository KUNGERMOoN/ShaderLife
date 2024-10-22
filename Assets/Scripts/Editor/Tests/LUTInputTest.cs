using GameOfLife;
using NUnit.Framework;

/// <summary>
/// Tests whether the code generating the configuration used as an input to the LUT works properly.
/// </summary>
public class LUTInputTest
{
    [Test]
    public void TestAll()
    {
        for (int i = 0; i < LookupTable.configurations; i++)
        {
            int[,] nums3x3 = new int[3, 3];

            bool[,][,] cells3x3 = new bool[3, 3][,];

            for (int chunkX = 0; chunkX < 3; chunkX++)
            {
                for (int chunkY = 0; chunkY < 3; chunkY++)
                {
                    cells3x3[chunkX, chunkY] = new bool[LookupTable.outputColumns, LookupTable.outputRows];
                }
            }

            for (int x = 3, j = i; x < 9; x++)
            {
                for (int y = 1; y < 5; y++)
                {
                    int chunkX = x / LookupTable.outputColumns;
                    int chunkY = y / LookupTable.outputRows;

                    int localX = x - chunkX * LookupTable.outputColumns;
                    int localY = y - chunkY * LookupTable.outputRows;

                    cells3x3[chunkX, chunkY][localX, localY] = j % 2 == 1;
                    j /= 2;
                }
            }

            for (int chunkX = 0; chunkX < 3; chunkX++)
            {
                for (int chunkY = 0; chunkY < 3; chunkY++)
                {
                    nums3x3[chunkX, chunkY] = LookupTable.ToNumber(
                        cells3x3[chunkX, chunkY],
                        LookupTable.outputRows,
                        LookupTable.outputColumns);
                }
            }

            TestOnConfiguration(nums3x3, out int outputA, out int outputB);

            Assert.AreEqual(outputA, outputB);
        }
    }

    /*[Test]
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
        const int configurations = 1 << (LUTBuilder.outputRows * LUTBuilder.outputColumns);

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
    }*/

    static void TestOnConfiguration(int[,] nums3x3, out int outputA, out int outputB)
    {
        //Calculated manually
        bool[,] cells3x3 = new bool[3 * LookupTable.outputColumns, 3 * LookupTable.outputRows];

        for (int chunkX = 0; chunkX < 3; chunkX++)
        {
            for (int chunkY = 0; chunkY < 3; chunkY++)
            {
                bool[,] chunk = LookupTable.ToCells(nums3x3[chunkX, chunkY],
                    LookupTable.outputRows,
                    LookupTable.outputColumns);

                for (int u = 0; u < LookupTable.outputColumns; u++)
                {
                    for (int v = 0; v < LookupTable.outputRows; v++)
                    {
                        cells3x3[LookupTable.outputColumns * chunkX + u, LookupTable.outputRows * chunkY + v] = chunk[u, v];
                    }
                }
            }
        }

        bool[,] data = new bool[LookupTable.inputColumns, LookupTable.inputRows];

        for (int x_ = 3; x_ < 3 * LookupTable.outputColumns - 3; x_++)
        {
            for (int y_ = 1; y_ < 3 * LookupTable.outputRows - 1; y_++)
            {
                data[x_ - 3, y_ - 1] = cells3x3[x_, y_];
            }
        }
        outputA = LookupTable.ToNumber(data, LookupTable.inputRows, LookupTable.inputColumns);

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
            ((a << 23) & 8388608) +
            ((b << 19) & 7864320) +
            ((c << 15) & 262144) +
            ((d << 13) & 131072) +
            ((d << 11) & 2048) +
            ((e << 05) & 4096) +
            ((e << 03) & 64) +
            ((f << 01) & 32) +
            ((g >> 03) & 30) +
            ((h >> 07) & 1) +
            ((x << 09) & 122880) +
            ((x << 07) & 1920);
    }
}
