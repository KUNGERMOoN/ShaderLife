//Implementation of double buffering (read more here: https://gameprogrammingpatterns.com/double-buffer.html)
#ifdef RANDOM_WRITE
    RWStructuredBuffer<int> chunksA;
    RWStructuredBuffer<int> chunksB;
#else
    StructuredBuffer<int> chunksA;
    StructuredBuffer<int> chunksB;
#endif


int GetByte(StructuredBuffer<int> buffer, uint index)
{
    uint offset = (index % 4u) * 8;
    return (buffer[index / 4u] >> offset) & 255;
}

#ifdef RANDOM_WRITE
int GetByte(RWStructuredBuffer<int> buffer, uint index)
{
    uint offset = (index % 4u) * 8;
    return (buffer[index / 4u] >> offset) & 255;
}

void SetByte(RWStructuredBuffer<int> buffer, uint index, int value)
{
    uint i = index / 4u;
    uint offset = (index % 4u) * 8;
    InterlockedAnd(buffer[i], ~(255 << offset)); //Reset the byte to 00000000
    InterlockedOr(buffer[i], value << offset); //Set the byte to value
}
#endif


#ifndef FLIP_BUFFER //Shader keywords
    #define CURRENT_BUFFER chunksA
    #define PREVIOUS_BUFFER chunksB
#else
    #define CURRENT_BUFFER chunksB
    #define PREVIOUS_BUFFER chunksA
#endif


int GetPrevious(uint i)
{
    return GetByte(PREVIOUS_BUFFER, i);
}

int GetCurrent(uint i)
{
    return GetByte(CURRENT_BUFFER, i);
}

#ifdef RANDOM_WRITE
void SetPrevious(uint i, int value)
{
    SetByte(PREVIOUS_BUFFER, i, value);
}

void SetCurrent(uint i, int value)
{
    SetByte(CURRENT_BUFFER, i, value);
}
#endif