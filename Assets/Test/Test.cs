using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Test : MonoBehaviour
{
    public ComputeShader Shader;
    ComputeBuffer buffer;

    AsyncGPUReadbackRequest request;

    NativeArray<int> Data;

    private void Awake()
    {
        Data = new NativeArray<int>(5760000, Allocator.Persistent);
        buffer = new ComputeBuffer(Data.Length, sizeof(int));
        buffer.SetData(Data);

        Shader.SetBuffer(0, "buffer", buffer);

        request = AsyncGPUReadback.Request(buffer);
    }

    private void Update()
    {
        Shader.Dispatch(0, 300, 300, 1);
        if (request.done && !request.hasError)
        {
            Data.Dispose();
            Data = request.GetData<int>();
            request = AsyncGPUReadback.Request(buffer);
        }
    }

    private void OnDestroy()
    {
        buffer.Dispose();
        Data.Dispose();
    }
}
