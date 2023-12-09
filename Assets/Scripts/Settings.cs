using System;
using UnityEngine;

[Serializable]
public class Settings
{
    [Header("Camera")]
    public float CameraMaxSpeed = 0.6f;
    public float CameraAcceleration = 0.15f;
    public float CameraPositionLimit = 1;
    [Tooltip("The change \"s\" of camera's zoom, if v = s / t")]
    public float CameraZoomMultiplier = 0.15f;
    [Tooltip("The time \"t\" of camera's zoom, if v = s / t")]
    public float CameraZoomTime = 0.03f;
}
