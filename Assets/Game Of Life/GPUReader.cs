using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUReader<T> where T : struct
{
    public ComputeBuffer Buffer;
    public event Action<NativeArray<T>> OnReceiveData;

    AsyncGPUReadbackRequest? request;

    public bool ContinouslyRequest { get; private set; }

    public void StartContinuousRequesting()
    {
        ContinouslyRequest = true;
        SendRequest();
    }
    public void StopContinuousRequesting(bool abort)
    {
        ContinouslyRequest = false;
        if (abort) request = null;
    }

    public void Update()
    {
        if (request != null && request.Value.done)
        {
            if (request.Value.hasError)
            {
                Debug.LogError("GPU Reader has encounterer an error!");
            }
            else
            {
                OnReceiveData?.Invoke(request.Value.GetData<T>());
            }
            request = null;
        }

        if (ContinouslyRequest) SendRequest();
    }

    public void SendRequest()
    {
        if (request == null)
        {
            request = AsyncGPUReadback.Request(Buffer);
        }
    }

    public GPUReader(ComputeBuffer buffer, Action<NativeArray<T>> dataReceivedCallback)
    {
        Buffer = buffer;
        OnReceiveData += dataReceivedCallback;
        request = null;
    }
}
