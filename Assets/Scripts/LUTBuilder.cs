using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class LUTBuilder
{
    //Size of each configuration
    public const int rows = 4;
    public const int columns = 6;

    //Amount of all possible configurations (1 byte each)
    public const int configurations = 1 << (rows * columns); //2^24

    //Amount of elements needed to store all possible configurations when they are packed
    //into 4-byte object (so it can be sent as a buffer to the compute shader)
    public const int packedLength = configurations / 4; //2^6


    public static string FileExtension = "lut";


    public bool Generated { get; private set; }
    public float GenerationProgress => GeneratedConfigurations / packedLength;
    public byte[] Bytes { get; private set; }
    public Packed4Bytes[] Packed { get; private set; }

    public readonly int[] BirthCount, SurviveCount;


    int GeneratedConfigurations;

    public LUTBuilder(int[] birthCount, int[] surviveCount)
    {
        BirthCount = birthCount;
        SurviveCount = surviveCount;
    }

    public void WriteToFile(string fileName, string folderInDataPath, Func<bool> overwriteCheck)
    {
        fileName = $"{fileName.Split('.')[0]}.{FileExtension}";
        string path = $"{Application.dataPath}/{folderInDataPath}/{fileName}";

        if (File.Exists(path))
        {
            if (overwriteCheck() == false)
                return;
        }

        try
        {
            if (Generated == false) GenerateLUT();

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

            Debug.Log($"Building Lookup Table \"{fileName}\" in {folderInDataPath} completed successfully.");
        }
        catch (Exception error)
        {
            Debug.LogError($"Building Lookup Table \"{fileName}\" in {folderInDataPath} " +
                "failed with the following error: " + error.ToString());
            DeleteLUTFile(fileName, folderInDataPath);
        }
    }

    public void GenerateLUT()
    {
        Bytes = new byte[configurations];
        Packed = new Packed4Bytes[packedLength];

        for (int i = 0; i < packedLength; i++)
        {
            Generate4Configurations(i);
        }

        Generated = true;
    }

    public void BeginGenerate()
    {
        Bytes = new byte[configurations];
        Packed = new Packed4Bytes[packedLength];
        GeneratedConfigurations = 0;
    }

    public void UpdateGenerate()
    {
        if (GeneratedConfigurations < packedLength)
        {
            Generate4Configurations(GeneratedConfigurations);
            GeneratedConfigurations++;
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
            builder.Packed = new Packed4Bytes[packedLength];
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

                builder.Packed[i] = new()
                {
                    Byte1 = byte1,
                    Byte2 = byte2,
                    Byte3 = byte3,
                    Byte4 = byte4
                };
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

    void Generate4Configurations(int packedIndex)
    {
        byte byte1 = GenerateConfiguration(packedIndex * 4);
        byte byte2 = GenerateConfiguration(packedIndex * 4 + 1);
        byte byte3 = GenerateConfiguration(packedIndex * 4 + 2);
        byte byte4 = GenerateConfiguration(packedIndex * 4 + 3);

        Bytes[packedIndex * 4] = byte1;
        Bytes[packedIndex * 4 + 1] = byte2;
        Bytes[packedIndex * 4 + 2] = byte3;
        Bytes[packedIndex * 4 + 3] = byte4;

        Packed[packedIndex] = new()
        {
            Byte1 = byte1,
            Byte2 = byte2,
            Byte3 = byte3,
            Byte4 = byte4
        };
    }

    byte GenerateConfiguration(int startingConfiguration)
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
                    SurviveCount.Contains(neighbours) :
                    BirthCount.Contains(neighbours);
            }
        }
        return (byte)ToNumber(newCells, rows - 2, columns - 2);
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
}
