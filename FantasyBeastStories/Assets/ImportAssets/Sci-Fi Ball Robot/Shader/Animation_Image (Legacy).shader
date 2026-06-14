// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Animation_Image (Legacy)"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		_ExpressionImage("Expression Image", 2D) = "white" {}
		_ImageRow("Image Row", Float) = 0
		_ImageColumns("Image Columns", Float) = 0
		[HDR]_MainColor("Main Color", Color) = (1,1,1,1)
		_StartFrame("Start Frame", Float) = 0
		_Speed("Speed", Float) = 0

	}

	SubShader
	{
		LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha
		
		
		Pass
		{
		CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};
			
			uniform fixed4 _Color;
			uniform float _EnableExternalAlpha;
			uniform sampler2D _MainTex;
			uniform sampler2D _AlphaTex;
			uniform float4 _MainColor;
			uniform sampler2D _ExpressionImage;
			uniform float _ImageColumns;
			uniform float _ImageRow;
			uniform float _Speed;
			uniform float _StartFrame;

			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				
				
				IN.vertex.xyz +=  float3(0,0,0) ; 
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
				// get the color from an external texture (usecase: Alpha support for ETC1 on android)
				fixed4 alpha = tex2D (_AlphaTex, uv);
				color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
#endif //ETC1_EXTERNAL_ALPHA

				return color;
			}
			
			fixed4 frag(v2f IN  ) : SV_Target
			{
				float2 uv04 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				// *** BEGIN Flipbook UV Animation vars ***
				// Total tiles of Flipbook Texture
				float fbtotaltiles2 = _ImageColumns * _ImageRow;
				// Offsets for cols and rows of Flipbook Texture
				float fbcolsoffset2 = 1.0f / _ImageColumns;
				float fbrowsoffset2 = 1.0f / _ImageRow;
				// Speed of animation
				float fbspeed2 = _Time.y * _Speed;
				// UV Tiling (col and row offset)
				float2 fbtiling2 = float2(fbcolsoffset2, fbrowsoffset2);
				// UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
				// Calculate current tile linear index
				float fbcurrenttileindex2 = round( fmod( fbspeed2 + _StartFrame, fbtotaltiles2) );
				fbcurrenttileindex2 += ( fbcurrenttileindex2 < 0) ? fbtotaltiles2 : 0;
				// Obtain Offset X coordinate from current tile linear index
				float fblinearindextox2 = round ( fmod ( fbcurrenttileindex2, _ImageColumns ) );
				// Multiply Offset X by coloffset
				float fboffsetx2 = fblinearindextox2 * fbcolsoffset2;
				// Obtain Offset Y coordinate from current tile linear index
				float fblinearindextoy2 = round( fmod( ( fbcurrenttileindex2 - fblinearindextox2 ) / _ImageColumns, _ImageRow ) );
				// Reverse Y to get tiles from Top to Bottom
				fblinearindextoy2 = (int)(_ImageRow-1) - fblinearindextoy2;
				// Multiply Offset Y by rowoffset
				float fboffsety2 = fblinearindextoy2 * fbrowsoffset2;
				// UV Offset
				float2 fboffset2 = float2(fboffsetx2, fboffsety2);
				// Flipbook UV
				half2 fbuv2 = uv04 * fbtiling2 + fboffset2;
				// *** END Flipbook UV Animation vars ***
				float4 tex2DNode1 = tex2D( _ExpressionImage, fbuv2 );
				
				fixed4 c = ( _MainColor * tex2DNode1 );
				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=17900
-1667;225;1570;914;586.4302;697.2501;1;True;True
Node;AmplifyShaderEditor.SimpleTimeNode;31;-744.4017,61.57169;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;-804.9847,-42.13322;Inherit;False;Property;_StartFrame;Start Frame;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1012.158,-325.7035;Inherit;False;Property;_ImageColumns;Image Columns;2;0;Create;True;0;0;False;0;0;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-985.0138,-215.2233;Inherit;False;Property;_ImageRow;Image Row;1;0;Create;True;0;0;False;0;0;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-726.3765,-394.777;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-751.1291,-150.9589;Inherit;False;Property;_Speed;Speed;5;0;Create;True;0;0;False;0;0;15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;2;-391.0245,-279.3813;Inherit;False;0;0;6;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;1;-100.9057,-302.4214;Inherit;True;Property;_ExpressionImage;Expression Image;0;0;Create;True;0;0;False;0;-1;None;36829d8190f32414eb71505c249ce733;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;41;-0.3435059,-532.6447;Inherit;False;Property;_MainColor;Main Color;3;1;[HDR];Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;29;-69.69662,-55.27625;Inherit;False;Property;_Opacity;Opacity;6;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;278.6565,-460.6447;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;265.9095,-133.3653;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;62;484.2059,-321.3188;Float;False;True;-1;2;ASEMaterialInspector;0;8;Animation_Image (Legacy);0f8ba0101102bb14ebf021ddadce9b49;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;True;3;1;False;-1;10;False;-1;0;5;False;-1;10;False;-1;False;False;True;2;False;-1;False;False;True;2;False;-1;False;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;0;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;;0
WireConnection;2;0;4;0
WireConnection;2;1;7;0
WireConnection;2;2;8;0
WireConnection;2;3;9;0
WireConnection;2;4;34;0
WireConnection;2;5;31;0
WireConnection;1;1;2;0
WireConnection;40;0;41;0
WireConnection;40;1;1;0
WireConnection;39;0;1;4
WireConnection;39;1;29;0
WireConnection;62;0;40;0
ASEEND*/
//CHKSM=6463290A00209E3430A61A43ADEF875C09BC9DFD