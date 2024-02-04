using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;

public class LUTContentsTest : MonoBehaviour
{
    [Test]
    public void TestAll()
    {
        int[] birthCount = new int[] { 3 };
        int[] surviveCount = new int[] { 2, 3 };

        string originalPath = Path.Combine(Application.dataPath, "Scripts", "Editor", "Tests", "GameOfLife.lut");
        byte[] original = File.ReadAllBytes(originalPath);
        int contentOffset = (birthCount.Length + surviveCount.Length + 2) * sizeof(int);

        LookupTable builder = new(birthCount, surviveCount);
        IEnumerator enumerator = builder.Generate();

        int i = 0;
        while (enumerator.MoveNext())
        {
            Assert.AreEqual(builder.Contents[i + 0], original[contentOffset + i + 0]);
            Assert.AreEqual(builder.Contents[i + 1], original[contentOffset + i + 1]);
            Assert.AreEqual(builder.Contents[i + 2], original[contentOffset + i + 2]);
            Assert.AreEqual(builder.Contents[i + 3], original[contentOffset + i + 3]);

            i += 4;
        }
    }
}
