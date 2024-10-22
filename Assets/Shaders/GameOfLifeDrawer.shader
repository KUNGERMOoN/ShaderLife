Shader "Game Of Life/GameOfLifeDrawer"
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
		_AAScale ("Anti Aliasing Scale", Float) = 10 //Increase if the grid becomes too pixelated. Decrease if grid becomes too blurred.
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			//#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma multi_compile __ FLIP_BUFFER

			#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"
			#include "Shared.cginc"

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
			
			int _BoardSize;
			float4 _AliveCol;
			float4 _DeadCol;
			float4 _GridCol;
			float _GridWidth;
			int _GridPower;
			int _GridDetail;
			float _GridFadePow;
			float _AAScale;

			float logb(float base, float a)
			{
				return log2(a) / log2(base);
			}

			float grid(int gridScale, float2 uv, float zoom, float gridWidth)
			{
				float2 distance = float2(
						(uv.x * _BoardSize) % gridScale,
						(uv.y * _BoardSize) % gridScale);

				float distanceFromGrid = min(min(distance.x, distance.y), gridScale - max(distance.x, distance.y));
				
				float AAstep = zoom * _AAScale * _BoardSize / 8192;


				return smoothstep(distanceFromGrid - AAstep, distanceFromGrid + AAstep, gridWidth);
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

				float gridWidth = zoom * _GridWidth / 2 * _BoardSize / 32;

				float boardSizeExtraGridEdge = _BoardSize + 2 * gridWidth;
				i.uv = i.uv * boardSizeExtraGridEdge / _BoardSize - gridWidth / _BoardSize;

				float2 boardPos = i.uv * _BoardSize;

				//CELLS:
				uint2 cellPos = floor(boardPos);
				uint2 chunkPos = floor(float2(cellPos.x / 4, cellPos.y / 2));
				uint2 localPos = int2(cellPos.x % 4, cellPos.y % 2);

				int chunkIndex = (chunkPos.x + 1) * ((uint)_BoardSize / 2 + 2) + chunkPos.y + 1;

				int chunkData = GetCurrent(chunkIndex);

				bool alive = (chunkData >> (7 - localPos.x - 4 * localPos.y)) & 1;
				float4 cellCol = alive ? _AliveCol : _DeadCol;

				//GRID:
				float visibleArea = zoom * boardSizeExtraGridEdge;
				float visibleCells = clamp(visibleArea - gridWidth, 0, _BoardSize);

				float scaleLevel = logb(_GridPower, visibleCells / _GridDetail);

				int nextGridScale = pow(_GridPower, floor(max(scaleLevel, 0)));
				int currentGridScale = pow(_GridPower, floor(max(scaleLevel - 1, 0)));

				float nextGrid = grid(nextGridScale, i.uv, zoom, gridWidth);
				float currentGrid = grid(currentGridScale, i.uv, zoom, gridWidth);

				float currentGridFading = pow(1 - (scaleLevel - floor(scaleLevel)), _GridFadePow);

				float gridValue = max(nextGrid, currentGrid * currentGridFading);

				return lerp(cellCol, _GridCol, gridValue);
			}
			ENDCG
		}
	}
}
