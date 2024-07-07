using GameOfLife;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;

public class LUTContentsTest : MonoBehaviour
{
    [Test]
    public void TestAll()
    {
        bool[] birthCount = { false, false, false, true, false, false, false, false, false };
        bool[] surviveCount = { false, false, true, true, false, false, false, false, false };

        string originalPath = Path.Combine(Application.dataPath, "Scripts", "Editor", "Tests", "GameOfLife.lut");
        byte[] original = LookupTable.ReadFromFile(originalPath).Contents;

        LookupTable builder = new(birthCount, surviveCount);
        IEnumerator enumerator = builder.Generate();

        int i = 0;
        while (enumerator.MoveNext())
        {
            Assert.AreEqual(builder.Contents[i + 0], original[i + 0]);
            Assert.AreEqual(builder.Contents[i + 1], original[i + 1]);
            Assert.AreEqual(builder.Contents[i + 2], original[i + 2]);
            Assert.AreEqual(builder.Contents[i + 3], original[i + 3]);

            i += 4;
        }
    }
}
