Shader "Unlit/NewUnlitShader 1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v, out float4 outVertex : SV_POSITION)
            {
                v2f o;
                outVertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
                // sample the texture
                /*
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
                */

                return fixed4(i.uv.x, 0, i.uv.y, 0);
                //return fixed4((float)screenPos.x / _ScreenParams.x, 0, (float)screenPos.y / _ScreenParams.y, 0);
            }
            ENDCG
        }
    }
}
