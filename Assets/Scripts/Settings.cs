using System;
using UnityEngine;

[Serializable]
public class Settings
{
    [Header("Camera")]
    public float CameraMaxSpeed = 2f;
    public float CameraAcceleration = 0.10f;
    public float CameraZoomMultiplier = 0.15f;
    public float CameraZoomSmoothness = 3f;

    [Header("FPS Counter")]
    public float FPSCounterRefreshRate = 0.5f;
}
