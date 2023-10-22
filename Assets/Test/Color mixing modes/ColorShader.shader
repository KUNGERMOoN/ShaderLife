Shader "Unlit/ColorShader"
{
    Properties
    {
        _Col1 ("Color 1", COLOR) = (0, 1, 0, 1)
        _Col2 ("Color 2", COLOR) = (.89, .89, .89, 1)
        _Transparency ("Transparency", Range(0, 1)) = 1
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

            float4 _Col1;
            float4 _Col2;
            float _Transparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 blendCol(float4 col1, float4 col2, float transparency)
            {
                return lerp(col1, col2, pow(transparency, 1.0f / 3));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = i.uv.x % 0.2;

                return (dist > 0.1) ? blendCol(_Col1, _Col2, _Transparency) : _Col1;
            }
            ENDCG
        }
    }
}
