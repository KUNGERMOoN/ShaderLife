using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GameOfLife
{
    public class LookupTable
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
        public const int packedLength = configurations / 4; //2^22


        public static string FileExtension => "lut";
        public static string LUTsPath => Path.Combine(Application.streamingAssetsPath, "Lookup Tables");
        public static string DefaultLUT => "GameOfLife.lut";
        public static bool[] DefaultBirthCount => defaultBirthCount.ToArray();

        private static readonly bool[] value = { false, false, false, true, false, false, false, false, false };
        static readonly bool[] defaultBirthCount = value;
        public static bool[] DefaultSurviveCount => defaultSurviveCount.ToArray();
        static readonly bool[] defaultSurviveCount = { false, false, true, true, false, false, false, false, false };

        public readonly string Rulestring;

        public bool Generated { get; private set; }
        public byte[] Contents { get; private set; }
        public int[] PackedContents { get; private set; }

        public readonly bool[] BirthCount, SurviveCount;

        public static string GenerateRulestring(bool[] birthCount, bool[] surviveCount)
        {
            StringBuilder result = new();
            result.Append("B");
            for (int i = 0; i <= 8; i++)
                if (birthCount[i]) result.Append(i);
            result.Append("/S");
            for (int i = 0; i <= 8; i++)
                if (surviveCount[i]) result.Append(i);

            return result.ToString();
        }

        public int GeneratedPacks { get; private set; }

        public LookupTable(bool[] birthCount, bool[] surviveCount)
        {
            BirthCount = birthCount;
            SurviveCount = surviveCount;
            Rulestring = GenerateRulestring(BirthCount, SurviveCount);
        }

        public void WriteToFile(string path)
        {
            if (Generated == false)
            {
                throw new InvalidOperationException($"Cannot write a Lookup Table to file: {path}" +
                    $"the Lookup Table's contents didn't get generated. " +
                    "Make sure you run GenerateAll() (or enumerate the Generate() coroutine) " +
                    "to generate the content before calling WriteToFile()");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using FileStream stream = new(path, FileMode.Create);
            using (BinaryWriter writer = new(stream))
            {
                for (int i = 0; i <= 8; i++)
                    writer.Write(BirthCount[i]);

                for (int i = 0; i <= 8; i++)
                    writer.Write(SurviveCount[i]);

                writer.Write(Contents);
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public void GenerateAll()
        {
            IEnumerator enumerator = Generate();
            while (enumerator.MoveNext()) { }
        }

        public IEnumerator Generate()
        {
            Contents = new byte[configurations];
            PackedContents = new int[packedLength];
            GeneratedPacks = 0;

            for (int i = 0; i < packedLength; i++)
            {
                GeneratePackedConfigurations(i);
                GeneratedPacks++;
                yield return null;
            }

            Generated = true;
        }

        public static LookupTable ReadFromFile(string path)
        {
            try
            {
                using FileStream stream = new(path, FileMode.Open);
                using BinaryReader reader = new(stream);

                bool[] birthCount, surviveCount;

                birthCount = new bool[9];
                for (int i = 0; i <= 8; i++)
                    birthCount[i] = reader.ReadBoolean();

                surviveCount = new bool[9];
                for (int i = 0; i <= 8; i++)
                    surviveCount[i] = reader.ReadBoolean();

                LookupTable lut = new(birthCount, surviveCount)
                {
                    GeneratedPacks = configurations,
                    Contents = new byte[configurations],
                    PackedContents = new int[packedLength]
                };
                for (int i = 0; i < packedLength; i++)
                {
                    byte byte1 = reader.ReadByte();
                    byte byte2 = reader.ReadByte();
                    byte byte3 = reader.ReadByte();
                    byte byte4 = reader.ReadByte();

                    lut.Contents[i * 4] = byte1;
                    lut.Contents[i * 4 + 1] = byte2;
                    lut.Contents[i * 4 + 2] = byte3;
                    lut.Contents[i * 4 + 3] = byte4;

                    lut.PackedContents[i] = PackBytes(byte1, byte2, byte3, byte4);
                }

                lut.Generated = true;
                return lut;
            }
            catch (Exception error)
            {
                Debug.LogError($"Loading Lookup Table from {path} " +
                    "failed with the following error: " + error.ToString());

                return null;
            }
        }

        void GeneratePackedConfigurations(int packedIndex)
        {
            byte byte0 = Simulate(packedIndex * 4);
            byte byte1 = Simulate(packedIndex * 4 + 1);
            byte byte2 = Simulate(packedIndex * 4 + 2);
            byte byte3 = Simulate(packedIndex * 4 + 3);

            Contents[packedIndex * 4] = byte0;
            Contents[packedIndex * 4 + 1] = byte1;
            Contents[packedIndex * 4 + 2] = byte2;
            Contents[packedIndex * 4 + 3] = byte3;

            PackedContents[packedIndex] = PackBytes(byte0, byte1, byte2, byte3);
        }

        static int PackBytes(byte byte0, byte byte1, byte byte2, byte byte3) =>
            (byte0 << 0) +
            (byte1 << 8) +
            (byte2 << 16) +
            (byte3 << 24);

        public byte Simulate(int startingConfiguration)
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
                        SurviveCount[neighbours] :
                        BirthCount[neighbours];
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

        public static string LogConfiguration(bool[,] cells)
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
}