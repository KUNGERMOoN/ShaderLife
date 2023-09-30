Shader "Unlit/GameOfLifeDrawer"
{
	Properties
	{
		_Interp ("Interpolation", Range(0, 1)) = 0 //TODO: Figure out what to do with this
		_sizeX ("Board Size X", Integer) = 1024
		_sizeY ("Board Size Y", Integer) = 2048
		_AliveCol ("Alive Color", COLOR) = (1, 1, 1, 1)
		_DeadCol ("Dead Color", COLOR) = (0, 0, 0, 1)
		_GridCol ("Grid Color", COLOR) = (.5, .5, .5, 1)
		_GridWidth ("Grid Width", Float) = 0.1
		_GridPower ("Grid Power", Integer) = 4
		_ZoomLevelTreshold ("Zoom Level Treshold", Float) = 1

		[KeywordEnum(Next, Previous)] Test ("Test", Float) = 0
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
			#pragma multi_compile TEST_NEXT TEST_PREVIOUS

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
			int _sizeX;
			int _sizeY;
			float4 _AliveCol;
			float4 _DeadCol;
			float4 _GridCol;
			float _GridWidth;
			int _GridPower;
			float _ZoomLevelTreshold;

			float logb(float base, float a)
			{
				return log2(a) / log2(base);
			}

			bool grid(int gridScale, float2 uv, float zoom)
			{
				float2 distance = float2(
						(uv.x * _sizeX * 4) % gridScale,
						(uv.y * _sizeY * 2) % gridScale);

				return distance.x < _GridWidth * zoom || distance.y < _GridWidth * zoom;
			}


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				int2 boardSize = int2(_sizeX * 4, _sizeY * 2);

				float zoom = (1 / length(UNITY_MATRIX_P._m01_m11_m21)) * 2;

				i.uv *= (float)(boardSize + _GridWidth * zoom) / boardSize;

				//CELLS:
				//calculate the position
				uint2 globalPos = floor(float2(i.uv.x * boardSize.x, i.uv.y * boardSize.y));
				uint2 chunkPos = floor(float2(globalPos.x / 4, globalPos.y / 2));
				uint2 localPos = int2(globalPos.x % 4, globalPos.y % 2);

				int chunkIndex = (chunkPos.x + 1) * (_sizeY + 2) + chunkPos.y + 1;

				int chunkData;
				if (FLIP_BUFFER)
				{
					chunkData = flipCells[chunkIndex];
				}
				else
				{
					chunkData = cells[chunkIndex];
				}

				bool alive = (chunkData >> (7 - localPos.x - 4 * localPos.y)) & 1;

				//GRID:
				float visibleCells = (clamp(zoom, 0, 1) * boardSize.y - _GridWidth * zoom) / _ZoomLevelTreshold;
				float scale = logb(_GridPower, /*floor(*/visibleCells/*)*/);
				//Calculate every how many cells grid occurs
				int nextGridScale = pow(_GridPower, floor(max(scale, 0)));
				int previousGridScale = pow(_GridPower, floor(max(scale - 1, 0)));

				fixed4 debugCol = lerp(fixed4(1, 0, 0, 0), fixed4(0, 0, 1, 0), floor(scale) / 5);

				float nextGrid = grid(nextGridScale, i.uv, zoom);
				float previousGrid = grid(previousGridScale, i.uv, zoom);

				fixed4 green = fixed4(0, 1, 0, 0);

				float grid;
				if(TEST_PREVIOUS) grid = previousGrid;
				else grid = nextGrid;

				return grid > 0 ? grid : debugCol;
						//(previousGrid > 0 ? previousGrid * green : debugCol);

				//TODO: Make the board look better (grid etc.)
				//TODO: Make a heatmap shader

				//return fixed4(chunk.x % 7 == 0, ((chunk.x % 2 == 0) || (chunk.y % 2 == 0)) * 0.2, chunk.y % 7 == 0, 0);

				/*fixed4 col1 = (chunkData >> (7 - local.x - 4 * local.y)) & 1;
				fixed4 col2 = debugBuffer[chunkIndex] == 100;
				return lerp(col1, col2, _Interp);*/

				/*fixed x = (fixed)chunk.x * 4 / _sizeX;
				fixed y = (fixed)chunk.y * 2 / _sizeY;

				fixed4 col1 = fixed4(x, 0, y, 0);
				//fixed index = (fixed)chunkIndex / ((_sizeX / 4) * (_sizeY / 2));
				fixed4 col2 = fixed4(chunkIndex % 10 == 0, 0, 0, 0);
				return lerp(col1, col2, _Interp);*/
				//return lerp(1, 0, (fixed)chunkIndex / 8192);
			}
			ENDCG
		}
	}
}
