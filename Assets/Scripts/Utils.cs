using System.Globalization;
using System.IO;

public static class Utils
{
    public static bool IsValidFileName(string fileName, string absolutePath) =>
        !string.IsNullOrEmpty(fileName) &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !File.Exists(Path.Combine(absolutePath, fileName));

    public static string ThousandSpacing(this int number)
    {
        var f = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

        return number.ToString("n", f);
    }
}
