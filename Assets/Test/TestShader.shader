// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TestShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Float) = 10
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
            #pragma enable_d3d11_debug_symbols
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //IMPORTANT: This works: UNITY_MATRIX_P._m01_m11_m21

                //fixed4 col = tex2D(_MainTex, i.uv * mul(unity_CameraProjection, float2(1, 1)).y);
                //float zoomScale = 1 / length(UNITY_MATRIX_P._m01_m11_m21);
                float zoomScale = 1 / length(UNITY_MATRIX_P._m01_m11_m21);
                //Thanks https://math.stackexchange.com/questions/237369/given-this-transformation-matrix-how-do-i-decompose-it-into-translation-rotati
                //fixed4 col = tex2D(_MainTex, i.uv * zoomScale);
                float2 coord = i.uv * 2 - float2(1, 1);
                return sqrt(coord.x * coord.x + coord.y * coord.y) < 0.5 * zoomScale;
            }
            ENDCG
        }
    }
}
