#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

public class RawBufferTest : MonoBehaviour
{
    public ComputeShader Shader;
    ComputeBuffer rawBuffer;

    [Button]
    void Start()
    {
        rawBuffer = new ComputeBuffer(64, sizeof(int), ComputeBufferType.Raw);
        Shader.SetBuffer(0, "rawBuffer", rawBuffer);
        Shader.Dispatch(0, 1, 1, 1);

        int[] data = new int[64];
        rawBuffer.GetData(data);

        for (int i = 0; i < data.Length; i++)
            Debug.Log($"Thread: {i}, data: {data[i]}");

        rawBuffer.Release();
    }
}
#endif