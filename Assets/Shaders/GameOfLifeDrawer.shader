Shader "Unlit/GameOfLifeDrawer"
{
	Properties
	{
		_Interp ("Interpolation", Range(0, 1)) = 0 //TODO: Figure out what to do with this

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

				//TODO: Grid width depends on board size
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

				//TODO: instead of keeping the grid the same size,
				//scale it according to grid's scale level
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
					chunkData = flipCells[chunkIndex];
				}
				else
				{
					chunkData = cells[chunkIndex];
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


				//TODO: Make the board look better (grid etc.)
				//TODO: Make a heatmap shader
				//TODO: Add customizable colors to heatmap shader
				//	+ ability to round the current heatmap color to highest/lowest color (maybe for things like OTCA metapixel)



				

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
