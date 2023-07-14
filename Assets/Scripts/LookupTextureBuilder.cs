using System.Linq;

public static class LookupTextureBuilder
{
    public static Packed4Bytes[] GenerateLookupTexture(int[] birthCount, int[] surviveCount)
    {
        const int rows = 4;
        const int columns = 6;

        //Amount of all possible configurations (1 byte each)
        const int configurations = 1 << (rows * columns);

        //Amount of elements required to store all possible configurations when
        //they are packed into Packed4Bytes structure
        //(each Packed4Bytes contains exacly 4 configurations and has size of 4 bytes)
        const int packedConfigurations = configurations / 4;

        //We pack our configurations
        Packed4Bytes[] result = new Packed4Bytes[packedConfigurations];
        for (int i = 0; i < packedConfigurations; i++)
        {
            byte configuration1 = SimulateConfiguration(i * 4, birthCount, surviveCount, rows, columns);
            byte configuration2 = SimulateConfiguration(i * 4 + 1, birthCount, surviveCount, rows, columns);
            byte configuration3 = SimulateConfiguration(i * 4 + 2, birthCount, surviveCount, rows, columns);
            byte configuration4 = SimulateConfiguration(i * 4 + 3, birthCount, surviveCount, rows, columns);

            Packed4Bytes packed = new()
            {
                Byte1 = configuration1,
                Byte2 = configuration2,
                Byte3 = configuration3,
                Byte4 = configuration4
            };

            result[i] = packed;
        }

        return result;
    }

    static byte SimulateConfiguration(int startingConfiguration, int[] birthCount, int[] surviveCount, int rows, int columns)
    {
        int newRows = rows - 2;
        int newColumns = columns - 2;

        bool[,] cells = ToCells(startingConfiguration, rows, columns);
        bool[,] newCells = new bool[newColumns, newRows];
        for (int y = 0; y < newRows; y++)
        {
            for (int x = 0; x < newColumns; x++)
            {
                bool wasAlive = cells[x + 1, y + 1];
                int neighbours = CountNeighbours(cells, x + 1, y + 1);
                newCells[x, y] =
                    wasAlive ?
                    surviveCount.Contains(neighbours) :
                    birthCount.Contains(neighbours);
            }
        }
        return (byte)ToNumber(newCells, newRows, newColumns);
    }

    static int CountNeighbours(bool[,] cells, int x, int y)
    {
        int count = 0;
        count += cells[x - 1, y - 1] ? 1 : 0;
        count += cells[x - 1, y] ? 1 : 0;
        count += cells[x - 1, y + 1] ? 1 : 0;
        count += cells[x, y - 1] ? 1 : 0;
        count += cells[x, y + 1] ? 1 : 0;
        count += cells[x + 1, y - 1] ? 1 : 0;
        count += cells[x + 1, y] ? 1 : 0;
        count += cells[x + 1, y + 1] ? 1 : 0;

        return count;
    }

    static bool[,] ToCells(int number, int rows, int columns)
    {
        bool[,] result = new bool[columns, rows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                result[columns - x - 1, rows - y - 1] = number % 2 == 1;
                number /= 2;
            }
        }

        return result;
    }

    static int ToNumber(bool[,] cells, int rows, int columns)
    {
        int result = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                result = (result << 1) + (cells[x, y] ? 1 : 0);
            }
        }
        return result;
    }

    static string debugConfiguration(bool[,] cells)
    {
        string result = "\n";
        for (int y = 0; y < cells.GetLength(1); y++)
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
