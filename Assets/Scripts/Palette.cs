using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct Palette
{
    public Color DeadCell;
    public Color Grid;
    public Color AliveCell;

    public string Name;
    public string Description;

    static Texture2D loadTexture
    {
        get
        {
            if (tempTexture == null)
                tempTexture = new(1, 1);

            return tempTexture;
        }
    }
    static Texture2D tempTexture;
    public static Palette FromImage(string imagePath, string descriptionPath)
    {
        Palette palette = FromImage(imagePath);
        palette.Description = string.Join("\n", File.ReadAllLines(descriptionPath));

        return palette;
    }

    public static Palette FromImage(string imagePath)
    {
        loadTexture.LoadImage(File.ReadAllBytes(imagePath));

        Palette palette = FromTexture(loadTexture);
        palette.Name = Path.GetFileNameWithoutExtension(imagePath);

        return palette;
    }

    public static Palette FromTexture(Texture2D texture)
        => new Palette
        {
            DeadCell = loadTexture.GetPixel(0, 0),
            Grid = loadTexture.GetPixel(1, 0),
            AliveCell = loadTexture.GetPixel(2, 0)
        };
}
