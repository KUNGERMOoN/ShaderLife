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
		_GridWidth ("Grid Width", Range(0, 1)) = 0.1
		_Pow ("Grid Pow", Range(1, 10)) = 0.1
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
			int _sizeX;
			int _sizeY;
			float4 _AliveCol;
			float4 _DeadCol;
			float4 _GridCol;
			float _GridWidth;
			float _Pow;


			float valley(float x, float n)
			{
				return n != 0 ? clamp((x - 1) / n + 1, 0, 1) : 0;
			}

			float valleyCenter(float x, float n)
			{
				return valley(abs(x * 2 - 1), n);
			}

			float valleyCenter(float x, float y, float n)
			{
				return valley(max(abs(x * 2 - 1), abs(y * 2 - 1)), n);
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
				//calculate the position
				uint2 globalPos = floor(float2(i.uv.x * _sizeX * 4, i.uv.y * _sizeY * 2));
				uint2 chunkPos = floor(float2(globalPos.x / 4, globalPos.y / 2));
				uint2 localPos = int2(globalPos.x % 4, globalPos.y % 2);

				int chunkIndex = (chunkPos.x + 1) * (_sizeY + 2) + chunkPos.y + 1;

				int chunkData;
				if(FLIP_BUFFER)
				{
					chunkData = flipCells[chunkIndex];
				}
				else
				{
					chunkData = cells[chunkIndex];
				}

				float2 distanceFromGrid = float2(
						i.uv.x * _sizeX * 4 - globalPos.x,
						i.uv.y * _sizeY * 2 - globalPos.y);

				//float grid = pow(max(distanceFromGrid.x, distanceFromGrid.y) / (_GridWidth / 2), 0.2);

				bool alive = (chunkData >> (7 - localPos.x - 4 * localPos.y)) & 1;

				return pow(valleyCenter(distanceFromGrid.x, distanceFromGrid.y, _GridWidth), _Pow);

				//return max(abs(i.uv.x * 2 - 1), abs(i.uv.y * 2 - 1));
				//return abs(min(i.uv.x, i.uv.y) * 2 - 1);
				//return valleyCenter(i.uv.x, _GridWidth);
				//return valley(i.uv.x, _GridWidth / 2);

				/*return grid ? _GridCol :
					(alive ? _AliveCol : _DeadCol);*/

				//TODO: Make the board more pretty (gray lines between cells etc.)
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
