using UnityEngine;

public class MemoryLeakBehaviour : MonoBehaviour
{
    public int BufferCount = 2;

    ComputeBuffer[] buffers;

    public void Allocate()
    {
        Dispose();

        var bufferSize = (int)(SystemInfo.maxGraphicsBufferSize / 4);

        buffers = new ComputeBuffer[BufferCount];
        for (int i = 0; i < BufferCount; i++)
        {
            buffers[i] = new ComputeBuffer(bufferSize, 4);
        }
    }

    public void Dispose()
    {
        if (buffers != null)
            foreach (var buffer in buffers)
                buffer?.Dispose();
    }
}
