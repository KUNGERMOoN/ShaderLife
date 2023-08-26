Shader "Unlit/GameOfLifeDrawer"
{
    Properties
    {
        _Interp ("Interpolation", Range(0, 1)) = 0
        _size ("Board Size", Integer) = 1024
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile __ FLIP_BUFFER

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            StructuredBuffer<int> cells;
            StructuredBuffer<int> flipCells;
            float _Interp;
            int _size;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //calculate the position
                uint2 global = floor(i.uv * _size);
                uint2 chunk = floor(int2(global.x / 4, global.y / 2));
                uint2 local = int2(global.x % 4, global.y % 2);

                int chunkIndex = (chunk.x + 1) * (_size + 2) + chunk.y + 1;

                int chunkData;
                if(FLIP_BUFFER)
                {
                    chunkData = flipCells[chunkIndex];
                }
                else
                {
                    chunkData = cells[chunkIndex];
                }
                

                return (chunkData >> (7 - local.x - 4 * local.y)) & 1;
            }
            ENDCG
        }
    }
}
