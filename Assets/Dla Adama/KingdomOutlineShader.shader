Shader "Unlit/KingdomOutlineShader"
{
    Properties
    {
        _MainTex ("Texture (Unused)", 2D) = "white" {}
        _Col ("Border Color", COLOR) = (1, 0, 1, 0)
        _Blurry ("Blurry Border Width", Range(0, 1)) = 0.2
        _Pow ("Blurry Border Power", Float) = 1
        _Solid ("Solid Border Width", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Col;
            float _Blurry;
            float _Pow;
            float _Solid;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float clampedUVPow = 
                    clamp(
                        1 - pow(
                            1 - (i.uv.x - _Solid) / clamp(_Blurry - _Solid, 0, 1),
                        max(_Pow, 1)),
                    0, 1);

                fixed4 col = lerp(_Col, 0, clampedUVPow);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
