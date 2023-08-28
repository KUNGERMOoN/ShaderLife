using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class LUTBuilder
{
    public const int outputRows = 2;
    public const int outputColumns = 4;

    //Size of each configuration
    public const int inputRows = 4;
    public const int inputColumns = 6;

    //Amount of all possible configurations (1 byte each)
    public const int configurations = 1 << (inputRows * inputColumns); //2^24

    //Amount of elements needed to store all possible configurations when they are packed
    //into 4-byte object (so it can be sent as a buffer to the compute shader)
    public const int packedLength = configurations / 4; //2^6


    public const string FileExtension = "lut";


    public bool Generated { get; private set; }
    public byte[] Bytes { get; private set; }
    public int[] Packed { get; private set; }

    public readonly int[] BirthCount, SurviveCount;


    public int GeneratedConfigurations { get; private set; }

    public LUTBuilder(int[] birthCount, int[] surviveCount)
    {
        BirthCount = birthCount;
        SurviveCount = surviveCount;
    }

    public bool WriteToFile(string fileName, string folderInDataPath)
    {
        fileName = $"{fileName.Split('.')[0]}.{FileExtension}";
        string path = $"{Application.dataPath}/{folderInDataPath}/{fileName.Split('.')[0]}.{FileExtension}";

        try
        {
            if (Generated == false)
            {
                Debug.LogError($"Cannot write Lookup Table \"{fileName}\" to file: " +
                    $"the Lookup Table's contents didn't get generated. " +
                    "Make sure you run Generate() (or BeginGenerate() and UpdateGenerate()) " +
                    "to generate the content before calling WriteToFile()");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using FileStream stream = new(path, FileMode.Create);
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(BirthCount.Length);
                foreach (int birth in BirthCount)
                    writer.Write(birth);

                writer.Write(SurviveCount.Length);
                foreach (int survive in SurviveCount)
                    writer.Write(survive);

                writer.Write(Bytes);
            }
#if UNITY_EDITOR
            AssetDatabase.ImportAsset($"Assets/{folderInDataPath}/{fileName}");
#endif

            Debug.Log($"Building Lookup Table \"{fileName}\" in " +
                $"{folderInDataPath} completed successfully.");
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError($"Building Lookup Table \"{fileName}\" in {folderInDataPath} " +
                "failed with the following error: " + error.ToString());
            DeleteLUTFile(fileName, folderInDataPath);
            return false;
        }
    }

    public void Generate()
    {
        Bytes = new byte[configurations];
        Packed = new int[packedLength];

        for (int i = 0; i < packedLength; i++)
        {
            GeneratePackedConfigurations(i);
        }

        Generated = true;
    }

    public void BeginGenerate()
    {
        Bytes = new byte[configurations];
        Packed = new int[packedLength];
        GeneratedConfigurations = 0;
    }

    public bool UpdateGenerate()
    {
        if (GeneratedConfigurations < packedLength)
        {
            GeneratePackedConfigurations(GeneratedConfigurations);
            GeneratedConfigurations++;

            return false;
        }
        else
        {
            Generated = true;
            return true;
        }
    }

    public static LUTBuilder LoadFromFile(string fileName, string folderInDataPath)
    {
        fileName = $"{fileName.Split('.')[0]}.{FileExtension}";
        string path = $"{Application.dataPath}/{folderInDataPath}/{fileName}";

        try
        {
            using FileStream stream = new(path, FileMode.Open);
            using BinaryReader reader = new(stream);

            int[] birthCount, surviveCount;

            birthCount = new int[reader.ReadInt32()];
            for (int i = 0; i < birthCount.Length; i++)
                birthCount[i] = reader.ReadInt32();

            surviveCount = new int[reader.ReadInt32()];
            for (int i = 0; i < surviveCount.Length; i++)
                surviveCount[i] = reader.ReadInt32();

            LUTBuilder builder = new(birthCount, surviveCount)
            {
                GeneratedConfigurations = configurations
            };

            builder.Bytes = new byte[configurations];
            builder.Packed = new int[packedLength];
            for (int i = 0; i < packedLength; i++)
            {
                byte byte1 = reader.ReadByte();
                byte byte2 = reader.ReadByte();
                byte byte3 = reader.ReadByte();
                byte byte4 = reader.ReadByte();

                builder.Bytes[i * 4] = byte1;
                builder.Bytes[i * 4 + 1] = byte2;
                builder.Bytes[i * 4 + 2] = byte3;
                builder.Bytes[i * 4 + 3] = byte4;

                builder.Packed[i] = PackBytes(byte1, byte2, byte3, byte4);
            }

            return builder;
        }
        catch (Exception error)
        {
            Debug.LogError($"Loading Lookup Table \"{fileName}\" in {folderInDataPath} " +
                "failed with the following error: " + error.ToString());

            return null;
        }
    }

    public static void DeleteLUTFile(string fileName, string folderInDataPath)
    {
        File.Delete($"{Application.dataPath}/" +
            $"{folderInDataPath}/" +
            $"{fileName.Split('.')[0]}.{FileExtension}");
    }

    void GeneratePackedConfigurations(int packedIndex)
    {
        byte byte1 = GenerateConfiguration(packedIndex * 4);
        byte byte2 = GenerateConfiguration(packedIndex * 4 + 1);
        byte byte3 = GenerateConfiguration(packedIndex * 4 + 2);
        byte byte4 = GenerateConfiguration(packedIndex * 4 + 3);

        Bytes[packedIndex * 4] = byte1;
        Bytes[packedIndex * 4 + 1] = byte2;
        Bytes[packedIndex * 4 + 2] = byte3;
        Bytes[packedIndex * 4 + 3] = byte4;

        Packed[packedIndex] = PackBytes(byte1, byte2, byte3, byte4);
    }

    static int PackBytes(byte byte1, byte byte2, byte byte3, byte byte4) =>
        (byte1 << 0) +
        (byte2 << 8) +
        (byte3 << 16) +
        (byte4 << 24);

    byte GenerateConfiguration(int startingConfiguration)
    {
        int newRows = inputRows - 2;
        int newColumns = inputColumns - 2;

        bool[,] cells = ToCells(startingConfiguration, inputRows, inputColumns);
        bool[,] newCells = new bool[newColumns, newRows];
        for (int y = 0; y < newRows; y++)
        {
            for (int x = 0; x < newColumns; x++)
            {
                bool wasAlive = cells[x + 1, y + 1];
                int neighbours = CountNeighbours(cells, x + 1, y + 1);
                newCells[x, y] =
                    wasAlive ?
                    SurviveCount.Contains(neighbours) :
                    BirthCount.Contains(neighbours);
            }
        }
        return (byte)ToNumber(newCells, inputRows - 2, inputColumns - 2);
    }

    public static int CountNeighbours(bool[,] cells, int x, int y)
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

    public static bool[,] ToCells(int number, int rows, int columns)
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

    public static int ToNumber(bool[,] cells, int rows, int columns)
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

    public static string PrintConfiguration(bool[,] cells)
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
