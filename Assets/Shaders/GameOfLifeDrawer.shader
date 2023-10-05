Shader "Unlit/GameOfLifeDrawer"
{
	Properties
	{
		_Interp ("Interpolation", Range(0, 1)) = 0 //TODO: Figure out what to do with this
		_BoardSize ("Board Size", Integer) = 1024
		_sizeY ("Board Size Y", Integer) = 2048
		_AliveCol ("Alive Color", COLOR) = (1, 1, 1, 1)
		_DeadCol ("Dead Color", COLOR) = (0, 0, 0, 1)
		_GridCol ("Grid Color", COLOR) = (.5, .5, .5, 1)
		_GridWidth ("Grid Width", Float) = 0.1
		_GridPower ("Grid Power", Integer) = 4
		_ZoomLevelTreshold ("Zoom Level Treshold", Float) = 1

		[IntRange] _Test ("Test", Range (0, 180)) = 20
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
			//TODO: Use only one variable
			int _BoardSize;
			float4 _AliveCol;
			float4 _DeadCol;
			float4 _GridCol;
			float _GridWidth;
			int _GridPower;
			float _ZoomLevelTreshold;
			int _Test;

			float logb(float base, float a)
			{
				return log2(a) / log2(base);
			}

			bool grid(int gridScale, float2 uv, int2 boardSize, float zoom)
			{
				float2 distance = float2(
						(uv.x * _BoardSize) % gridScale,
						(uv.y * _BoardSize) % gridScale);

				//TODO: Grid width depends on board size
									//TODO: make a variable so we don't recalculate 
									//_GridWidth * zoom so many times
				return distance.x < _GridWidth * zoom 
					|| distance.y < _GridWidth * zoom;
			}


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				int boardSize = int(_BoardSize);

				float zoom = (1 / length(UNITY_MATRIX_P._m01_m11_m21)) * 2;

				i.uv *= (float)(boardSize + _GridWidth * zoom) / boardSize;

				//CELLS:
				uint2 globalPos = floor(float2(i.uv.x * boardSize, i.uv.y * boardSize));
				uint2 chunkPos = floor(float2(globalPos.x / 4, globalPos.y / 2));
				uint2 localPos = int2(globalPos.x % 4, globalPos.y % 2);

				int chunkIndex = (chunkPos.x + 1) * (_BoardSize / 2 + 2) + chunkPos.y + 1;

				return chunkIndex < _Test; //(chunkIndex % 2 == 0) & (chunkIndex % 5 == 0);

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
				float4 cellCol = alive ? _AliveCol : _DeadCol;

				//GRID:
				float visibleArea = zoom * (float)(boardSize + _GridWidth * zoom);
				float visibleCells = clamp(visibleArea - zoom * _GridWidth, 0, boardSize);
				float scaleLevel = logb(_GridPower, visibleCells / (_ZoomLevelTreshold / _GridPower));

				int nextGridScale = pow(_GridPower, floor(max(scaleLevel, 0)));
				int currentGridScale = pow(_GridPower, floor(max(scaleLevel - 1, 0)));

				bool nextGrid = grid(nextGridScale, i.uv, boardSize, zoom);
				bool currentGrid = grid(currentGridScale, i.uv, boardSize, zoom);

				float currentGridTransparency = 1 - (scaleLevel - floor(scaleLevel));

				return 
					nextGrid == true ? _GridCol : 
					lerp(cellCol, _GridCol, currentGrid * currentGridTransparency);


				//TODO: Make the board look better (grid etc.)
				//TODO: Make a heatmap shader
				//TODO: Add customizable colors to heatmap shader
				//	+ ability to round them to highest/lowest color (maybe for things like OTCA metapixel)



				

				//bool grid;
				//if(TEST_CURRENT) grid = currentGrid;
				//else grid = nextGrid;
				
				//fixed4 debugCol;
				//if(TEST_CURRENT)
					//debugCol = lerp(fixed4(1, 0, 0, 0), fixed4(0, 0, 1, 0), 1 - (scaleLevel - floor(scaleLevel)));
				//else
					//debugCol = lerp(fixed4(1, 0, 0, 0), fixed4(0, 0, 1, 0), floor(scaleLevel) / 5);

				//return grid > 0 ? grid : debugCol;
						//(previousGrid > 0 ? previousGrid * green : debugCol);




				

				//return fixed4(chunk.x % 7 == 0, ((chunk.x % 2 == 0) || (chunk.y % 2 == 0)) * 0.2, chunk.y % 7 == 0, 0);

				/*fixed4 col1 = (chunkData >> (7 - local.x - 4 * local.y)) & 1;
				fixed4 col2 = debugBuffer[chunkIndex] == 100;
				return lerp(col1, col2, _Interp);*/

				/*fixed x = (fixed)chunk.x * 4 / _BoardSize;
				fixed y = (fixed)chunk.y * 2 / _sizeY;

				fixed4 col1 = fixed4(x, 0, y, 0);
				//fixed index = (fixed)chunkIndex / ((_BoardSize / 4) * (_sizeY / 2));
				fixed4 col2 = fixed4(chunkIndex % 10 == 0, 0, 0, 0);
				return lerp(col1, col2, _Interp);*/
				//return lerp(1, 0, (fixed)chunkIndex / 8192);
			}
			ENDCG
		}
	}
}
