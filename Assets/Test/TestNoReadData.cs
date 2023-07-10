using Unity.Collections;
using UnityEngine;

public class TestNoReadData : MonoBehaviour
{
    public ComputeShader Shader;
    ComputeBuffer buffer;

    NativeArray<int> Data;

    private void Awake()
    {
        Data = new NativeArray<int>(5760000, Allocator.Persistent);
        buffer = new ComputeBuffer(Data.Length, sizeof(int));
        buffer.SetData(Data);

        Shader.SetBuffer(0, "buffer", buffer);
    }

    private void Update()
    {
        Shader.Dispatch(0, 300, 300, 1);
    }

    private void OnDestroy()
    {
        buffer.Dispose();
        Data.Dispose();
    }
}
