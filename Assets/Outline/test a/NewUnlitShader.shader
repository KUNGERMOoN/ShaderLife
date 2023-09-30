Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            void tri(inout TriangleStream<g2f> triStream, v2g points[3], float4 color){
                g2f o;

                for(int i = 0; i < 3; i++)
                {
                    o.vertex = UnityObjectToClipPos(points[i].vertex);
                    o.uv = TRANSFORM_TEX(points[i].uv, _MainTex);
                    o.color = color;
                    triStream.Append(o);
                }
                triStream.RestartStrip();
            }

            [maxvertexcount(21)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                float4 white = float4(1, 1, 1, 0);
                float4 black = float4(0, 0, 0, 0);

                g2f o;
                float4 edgeOffset = mul(UNITY_MATRIX_P, float2(0.1, 0));

                for(int i = 0; i < 3; i++) {
                    int next = (i + 1) % 3;
                    
                    v2g edge1[3];
                    edge1[0] = IN[i];
                    edge1[1] = IN[i];
                    edge1[1].vertex += edgeOffset;
                    edge1[2] = IN[next];

                    tri(triStream, edge1, white);
                }
  
                
                tri(triStream, IN, black);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = i.color;//tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
