using System;

[Serializable]
public class Settings
{
    public float CameraMaxSpeed = 2f;
    public float CameraAcceleration = 0.2f;
    public float CameraZoomMultiplier = 0.15f;
    public float CameraZoomSmoothness = 3f;

    public float FPSCounterRefreshRate = 0.5f;

    public string ColorPalette = "Cold Light.png";
}
