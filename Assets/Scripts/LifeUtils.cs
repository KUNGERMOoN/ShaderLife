using System.IO;

public static class LifeUtils
{
    public static bool IsValidFileName(string fileName, string absolutePath) =>
        !string.IsNullOrEmpty(fileName) &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !File.Exists(Path.Combine(absolutePath, fileName));
}
