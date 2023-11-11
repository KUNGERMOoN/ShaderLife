Shader "Unlit/GameOfLifeDrawer"
{
	Properties
	{
		[Header(Simulation)]
		[Space]
		_BoardSize ("Board Size", Integer) = 32
		_AliveCol ("Alive Color", COLOR) = (1, 1, 1, 1)
		_DeadCol ("Dead Color", COLOR) = (0, 0, 0, 1)

		[Header(Grid)]
		[Space]
		_GridCol ("Grid Color", COLOR) = (.36, .36, .36, 1)
		_GridWidth ("Grid Width", Float) = 0.15 //0.06 for Unity-like
		_GridPower ("Grid Power", Integer) = 2
		_GridDetail ("Grid Detail Level", Integer) = 4
		_GridFadePow ("Grid Fading Power", Float) = 0.333
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

			//Implementation of double-buffering the board
			//For more info, see Shaders/GameOfLifeSimulation.compute
			StructuredBuffer<int> chunksA;
			StructuredBuffer<int> chunksB;

			int _BoardSize;
			float4 _AliveCol;
			float4 _DeadCol;
			float4 _GridCol;
			float _GridWidth;
			int _GridPower;
			int _GridDetail;
			float _GridFadePow;

			float logb(float base, float a)
			{
				return log2(a) / log2(base);
			}

			bool grid(int gridScale, float2 uv, float zoom, float gridWidth)
			{
				float2 distance = float2(
						(uv.x * _BoardSize) % gridScale,
						(uv.y * _BoardSize) % gridScale);

				return distance.x < gridWidth 
					|| distance.y < gridWidth;
			}


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 blend(float4 colorA, float4 colorB)
			{
				return float4((colorA.rgb * colorA.a + colorB.rgb * colorB.a), colorA.a + colorB.a);
			}

			float4 frag (v2f i) : SV_Target
			{
				float zoom = (1 / length(UNITY_MATRIX_MVP._m01_m11_m21)) * 2;

				float gridWidth = zoom * _GridWidth * _BoardSize / 32;

				float boardSizeExtraGridEdge = _BoardSize + gridWidth;
				i.uv *= boardSizeExtraGridEdge / _BoardSize;

				//CELLS:
				uint2 globalPos = floor(float2(i.uv.x * _BoardSize, i.uv.y * _BoardSize));
				uint2 chunkPos = floor(float2(globalPos.x / 4, globalPos.y / 2));
				uint2 localPos = int2(globalPos.x % 4, globalPos.y % 2);

				int chunkIndex = (chunkPos.x + 1) * ((uint)_BoardSize / 2 + 2) + chunkPos.y + 1;

				int chunkData;
				if (FLIP_BUFFER)
				{
					chunkData = chunksB[chunkIndex];
				}
				else
				{
					chunkData = chunksA[chunkIndex];
				}

				bool alive = (chunkData >> (7 - localPos.x - 4 * localPos.y)) & 1;
				float4 cellCol = alive ? _AliveCol : _DeadCol;

				//GRID:
				float visibleArea = zoom * boardSizeExtraGridEdge;
				float visibleCells = clamp(visibleArea - gridWidth, 0, _BoardSize);

				float scaleLevel = logb(_GridPower, visibleCells / _GridDetail);

				int nextGridScale = pow(_GridPower, floor(max(scaleLevel, 0)));
				int currentGridScale = pow(_GridPower, floor(max(scaleLevel - 1, 0)));

				bool nextGrid = grid(nextGridScale, i.uv, zoom, gridWidth);
				bool currentGrid = grid(currentGridScale, i.uv, zoom, gridWidth);

				float currentGridTransparency = 1 - (scaleLevel - floor(scaleLevel));

				return
					nextGrid == true ? _GridCol :
					//currentGrid ? _GridCol : lerp(fixed4(1, 0, 0, 0), fixed4(0, 0, 1, 0), currentGridTransparency);
					lerp(cellCol, _GridCol, pow(currentGrid * currentGridTransparency, _GridFadePow));
			}
			ENDCG
		}
	}
}
