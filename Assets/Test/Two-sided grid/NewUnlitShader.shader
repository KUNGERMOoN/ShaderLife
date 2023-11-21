Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _GridWidth ("Grid Width", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float _GridWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float gridWidth = _GridWidth;

                float mapSize = 10;
                i.uv = i.uv * (mapSize + 2 * gridWidth) / mapSize - gridWidth / mapSize;

                float2 uv1 = frac(i.uv * mapSize);

                bool minGrid = min(uv1.x, uv1.y) <= gridWidth;
                bool maxGrid = (1 - max(uv1.x, uv1.y)) <= gridWidth;

                float4 col = 1;
                col *= minGrid ? float4(0.8, 0.3, 0.3, 1) : 1;
                col *= maxGrid ? float4(0.3, 0.3, 0.8, 1) : 1;

                return col;
            }
            ENDCG
        }
    }
}
