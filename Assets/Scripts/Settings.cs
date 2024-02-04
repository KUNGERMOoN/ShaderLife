using MiniBinding;
using System;
using UnityEngine;

[Serializable]
public class Settings
{
    [Header("Camera")]
    public Bindable<float> CameraMaxSpeed = new(2f);
    public Bindable<float> CameraAcceleration = new(0.10f);
    public Bindable<float> CameraZoomMultiplier = new(0.15f);
    public Bindable<float> CameraZoomSmoothness = new(3f);
}
