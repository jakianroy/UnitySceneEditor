Shader "Unlit/ShowUV" 
{
	Properties 
	{
		_Color ("Line Color", Color) = (1,0,0,1)
		_Thickness ("Thickness", Float) = 1
	}

	SubShader 
	{
		

		Pass
			{
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

				struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				//float3 normal: NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed4 color : TEXCOORD1;
				//float3 normal : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				//o.normal = v.normal;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = i.color;
			return col;
			}
				ENDCG
			}
		Pass
			{
				Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }

				Blend SrcAlpha OneMinusSrcAlpha
				Cull Off
				LOD 200

				CGPROGRAM
				#pragma target 5.0
				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom


				struct v2g
				{
					float4	pos		: POSITION;
				};

				struct g2f
				{
					float4	pos		: POSITION;
					float3  dist	: TEXCOORD1;
				};

				float _Thickness = 1;
				float4 _Color = { 1,1,1,1 };

				v2g vert(appdata_base v)
				{
					v2g output;
					output.pos = UnityObjectToClipPos(v.vertex);
					return output;
				}

				[maxvertexcount(3)]
				void geom(triangle v2g p[3], inout TriangleStream<g2f> triStream)
				{

					float2 p0 = _ScreenParams.xy * p[0].pos.xy / p[0].pos.w;
					float2 p1 = _ScreenParams.xy * p[1].pos.xy / p[1].pos.w;
					float2 p2 = _ScreenParams.xy * p[2].pos.xy / p[2].pos.w;


					float2 v0 = p2 - p1;
					float2 v1 = p2 - p0;
					float2 v2 = p1 - p0;


					float area = abs(v1.x*v2.y - v1.y * v2.x);


					float dist0 = area / length(v0);
					float dist1 = area / length(v1);
					float dist2 = area / length(v2);

					g2f pIn;

					pIn.pos = p[0].pos;
					pIn.dist = float3(dist0, 0, 0);
					triStream.Append(pIn);

					pIn.pos = p[1].pos;
					pIn.dist = float3(0, dist1, 0);
					triStream.Append(pIn);

					pIn.pos = p[2].pos;
					pIn.dist = float3(0, 0, dist2);
					triStream.Append(pIn);
				}


				float4 frag(g2f input) : COLOR
				{
					float val = min(input.dist.x, min(input.dist.y, input.dist.z));

				val = exp2(-1 / _Thickness * val * val);

				float4 transCol = _Color;
				transCol.a = 0;
				float4 col = val * _Color + (1 - val) * transCol;

				clip(col.a - 0.5f);

				return col;
				}

				ENDCG
			}
	} 
}
