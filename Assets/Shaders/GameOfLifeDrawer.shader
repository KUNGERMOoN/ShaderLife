Shader "Unlit/GameOfLifeDrawer"
{
	Properties
	{
		_Interp ("Interpolation", Range(0, 1)) = 0 //TODO: Figure out what to do with this
		_sizeX ("Board Size X", Integer) = 1024 //I guess we will hide these as they're supposed
		_sizeY ("Board Size Y", Integer) = 2048 //to be modified only by code
		_AliveCol ("Alive Color", COLOR) = (1, 1, 1, 1)
		_DeadCol ("Dead Color", COLOR) = (0, 0, 0, 1)
		_GridCol ("Grid Color", COLOR) = (.5, .5, .5, 1)
		_GridWidth ("Grid Width", Range(0, 1)) = 0.1
		_GridPower ("Grid Power", Integer) = 4
		_Zoom ("Zoom", FLOAT) = 1
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
			int _GridPower;
			int _Zoom;

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

				bool alive = (chunkData >> (7 - localPos.x - 4 * localPos.y)) & 1;

				float zoom = max(_Zoom, 1);
				int gridScale = pow(_GridPower, zoom - 1);
				float2 distanceFromGrid = float2(
						(i.uv.x * _sizeX * 4) % gridScale,
						(i.uv.y * _sizeY * 2) % gridScale);

				//bool grid = pow(max(distanceFromGrid.x, distanceFromGrid.y) / (_GridWidth / 2), 0.2);

				/*return //grid ? _GridCol :
					(alive ? _AliveCol : _DeadCol);*/
				
				return (distanceFromGrid.x < _GridWidth) || (distanceFromGrid.y < _GridWidth); 

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
