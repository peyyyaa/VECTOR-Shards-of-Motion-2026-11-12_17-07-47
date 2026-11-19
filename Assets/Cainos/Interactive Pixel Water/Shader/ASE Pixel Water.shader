// Made with Amplify Shader Editor v1.9.9.14
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cainos/Interactive Pixel Water/Pixel Water"
{
	Properties
	{
		_WaterColorShallow( "Water Color Shallow", Color ) = ( 0.7450981, 0.9333333, 0.827451, 0 )
		_WaterColorDeep( "Water Color Deep", Color ) = ( 0.4431373, 0.6117647, 0.5215687, 0.3764706 )
		_UnderwaterTintShallow( "Underwater Tint Shallow", Color ) = ( 0.9098039, 0.9921569, 0.9686275, 1 )
		_UnderwaterTintDeep( "Underwater Tint Deep", Color ) = ( 0.3568628, 0.5843138, 0.4509804, 1 )
		_SurfaceColorUpper( "Surface Color Upper", Color ) = ( 0.8901961, 0.9921569, 1, 0.1960784 )
		_SurfaceColorLower( "Surface Color Lower", Color ) = ( 0.2039216, 0.3686275, 0.2941177, 0.03921569 )
		_SurfaceThicknessUpper( "Surface Thickness Upper", Float ) = 1
		_SurfaceThicknessLower( "Surface Thickness Lower", Float ) = 1
		_SurfaceDistortionMul( "Surface Distortion Mul", Float ) = 5
		_DistortionSpeed( "Distortion Speed", Float ) = 1
		_DistortionScale( "Distortion Scale", Float ) = 0.7
		_DistortionStrength( "Distortion Strength", Float ) = 0.5
		_BlurAmountShallow( "Blur Amount Shallow", Float ) = 2
		_BlurAmountDeep( "Blur Amount Deep", Float ) = 12
		_LightShaftColor( "Light Shaft Color", Color ) = ( 0.2235294, 0.3215686, 0.2745098, 0.3921569 )
		_LightShaftScale( "Light Shaft Scale", Float ) = 1.4
		_LightShaftPower( "Light Shaft Power", Float ) = 2
		_LightShaftTilt( "Light Shaft Tilt", Float ) = -0.2
		_LightShaftDepth( "Light Shaft Depth", Float ) = 2
		_LightShaftSpeed( "Light Shaft Speed", Float ) = 0.7
		_AmbientWaveMul( "Ambient Wave Mul", Float ) = 1
		_AmbientWaveSpeed( "Ambient Wave Speed", Vector ) = ( 0, 0, 0, 0 )
		_AmbientWaveFrequency( "Ambient Wave Frequency", Vector ) = ( 0, 0, 0, 0 )
		_AmbientWaveAmptitude( "Ambient Wave Amptitude", Vector ) = ( 0, 0, 0, 0 )
		_StencilReference( "Stencil Reference", Int ) = 123


		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Lit" "ShaderGraphShader"="true" }

	LOD 0

		Cull Off

		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
		ENDHLSL

		
		Pass
		{
			Name "Sprite Lit"
            Tags { "LightMode"="Universal2D" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19914
			#define ASE_SRP_VERSION 170300


			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_0
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_1
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_2
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_3

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_SCREENPOSITION
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITELIT

			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if ( UNITY_VERSION < 60030000 )
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#if USE_SHAPE_LIGHT_TYPE_0
			SHAPE_LIGHT(0)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_1
			SHAPE_LIGHT(1)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_2
			SHAPE_LIGHT(2)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_3
			SHAPE_LIGHT(3)
			#endif
			#endif

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			half4 _RendererColor;

			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float2 voronoihash35( float2 p )
			{
			
				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
				return frac( sin( p ) *43758.5453);
			}
			
			float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0;
				float F2 = 8.0; float2 mg = 0; int i, j;
				for ( j = -1; j <= 1; j++ )
				{
					for ( i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash35( n + g );
						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
						float d = 0.5 * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						} else if( d<F2 ) {
							F2 = d;
			
						}
					}
				}
				return F1;
			}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);
				if (radius < 1.0)
				{
					return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));
				}
				int sampleRadius = (int)min(radius, 4.0);
				float spacing = radius / (float)sampleRadius;
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				[unroll]
				for (int x = -4; x <= 4; ++x)
				{
					[unroll]
					for (int y = -4; y <= 4; ++y)
					{
						if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)
						{
							float2 offset = TexelSize * float2(x, y) * spacing;
							col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));
						}
					}
				}
				float samplesPerAxis = (float)(sampleRadius * 2 + 1);
				return col / (samplesPerAxis * samplesPerAxis);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord5.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord5.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor * unity_SpriteColor;
				o.screenPosition = vertexInput.positionNDC;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float4 screenPos = IN.ase_texcoord4;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord5.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float4 Mask = float4(1,1,1,1);
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.texCoord0.xy);
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture);
			#endif

			#if !defined( _DISABLE_COLOR_TINT )
				Color *= IN.color;
			#endif

				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, Mask, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.texCoord0.xy, half2(IN.screenPosition.xy / IN.screenPosition.w), inputData);

			#if defined(DEBUG_DISPLAY)
				SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);
			#endif

				return CombinedShapeLightShared(surfaceData, inputData);
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Normal"
            Tags { "LightMode"="NormalsRendering" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19914
			#define ASE_SRP_VERSION 170300


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

			#define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITENORMAL

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 tangentWS : TEXCOORD3;
				float3 bitangentWS : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float2 voronoihash35( float2 p )
			{
			
				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
				return frac( sin( p ) *43758.5453);
			}
			
			float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0;
				float F2 = 8.0; float2 mg = 0; int i, j;
				for ( j = -1; j <= 1; j++ )
				{
					for ( i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash35( n + g );
						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
						float d = 0.5 * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						} else if( d<F2 ) {
							F2 = d;
			
						}
					}
				}
				return F1;
			}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);
				if (radius < 1.0)
				{
					return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));
				}
				int sampleRadius = (int)min(radius, 4.0);
				float spacing = radius / (float)sampleRadius;
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				[unroll]
				for (int x = -4; x <= 4; ++x)
				{
					[unroll]
					for (int y = -4; y <= 4; ++y)
					{
						if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)
						{
							float2 offset = TexelSize * float2(x, y) * spacing;
							col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));
						}
					}
				}
				float samplesPerAxis = (float)(sampleRadius * 2 + 1);
				return col / (samplesPerAxis * samplesPerAxis);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord5 = screenPos;
				o.ase_texcoord6.xyz = ase_positionWS;
				
				o.ase_texcoord7.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord6.w = 0;
				o.ase_texcoord7.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.texCoord0 = v.uv0;
				o.color = v.color * unity_SpriteColor;
				o.positionCS = vertexInput.positionCS;

				float3 normalWS = TransformObjectToWorldNormal(v.normal);
				o.normalWS = -GetViewForwardDir();
				float4 tangentWS = float4( TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
				o.tangentWS = normalize(tangentWS);
				half crossSign = (tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
				o.bitangentWS = crossSign * cross(normalWS, tangentWS.xyz) * tangentWS.w;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord5;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord6.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord7.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

				Color = half4(1.0,1.0,1.0, Color.a);

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return NormalsRenderingShared(Color, Normal, IN.tangentWS.xyz, IN.bitangentWS, IN.normalWS);
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Forward"
            Tags { "LightMode"="UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19914
			#define ASE_SRP_VERSION 170300


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#if ( UNITY_VERSION >= 60010000 )
				#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			half4 _RendererColor;

			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float2 voronoihash35( float2 p )
			{
			
				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
				return frac( sin( p ) *43758.5453);
			}
			
			float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0;
				float F2 = 8.0; float2 mg = 0; int i, j;
				for ( j = -1; j <= 1; j++ )
				{
					for ( i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash35( n + g );
						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
						float d = 0.5 * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						} else if( d<F2 ) {
							F2 = d;
			
						}
					}
				}
				return F1;
			}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);
				if (radius < 1.0)
				{
					return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));
				}
				int sampleRadius = (int)min(radius, 4.0);
				float spacing = radius / (float)sampleRadius;
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				[unroll]
				for (int x = -4; x <= 4; ++x)
				{
					[unroll]
					for (int y = -4; y <= 4; ++y)
					{
						if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)
						{
							float2 offset = TexelSize * float2(x, y) * spacing;
							col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));
						}
					}
				}
				float samplesPerAxis = (float)(sampleRadius * 2 + 1);
				return col / (samplesPerAxis * samplesPerAxis);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord4.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord4.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor * unity_SpriteColor;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float4 screenPos = IN.ase_texcoord3;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord4.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
			#endif

			#if !defined( _DISABLE_COLOR_TINT )
				Color *= IN.color;
			#endif

				return Color;
			}

			ENDHLSL
		}

		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19914
			#define ASE_SRP_VERSION 170300


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float2 voronoihash35( float2 p )
			{
			
				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
				return frac( sin( p ) *43758.5453);
			}
			
			float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0;
				float F2 = 8.0; float2 mg = 0; int i, j;
				for ( j = -1; j <= 1; j++ )
				{
					for ( i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash35( n + g );
						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
						float d = 0.5 * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						} else if( d<F2 ) {
							F2 = d;
			
						}
					}
				}
				return F1;
			}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);
				if (radius < 1.0)
				{
					return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));
				}
				int sampleRadius = (int)min(radius, 4.0);
				float spacing = radius / (float)sampleRadius;
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				[unroll]
				for (int x = -4; x <= 4; ++x)
				{
					[unroll]
					for (int y = -4; y <= 4; ++y)
					{
						if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)
						{
							float2 offset = TexelSize * float2(x, y) * spacing;
							col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));
						}
					}
				}
				float samplesPerAxis = (float)(sampleRadius * 2 + 1);
				return col / (samplesPerAxis * samplesPerAxis);
			}
			

			VertexOutput vert(VertexInput v )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.ase_texcoord.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord = screenPos;
				o.ase_texcoord1.xyz = ase_positionWS;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord2.zw = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord1.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord2.zw.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.ase_texcoord2.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float AlphaClipThreshold = 0.5;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return half4(_ObjectId, _PassValue, 1.0, 1.0);
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }

			Cull Off

            HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19914
			#define ASE_SRP_VERSION 170300


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	#define ASE_NEEDS_VERT_POSITION
        	#define ASE_NEEDS_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float2 voronoihash35( float2 p )
			{
			
				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
				return frac( sin( p ) *43758.5453);
			}
			
			float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0;
				float F2 = 8.0; float2 mg = 0; int i, j;
				for ( j = -1; j <= 1; j++ )
				{
					for ( i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash35( n + g );
						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
						float d = 0.5 * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						} else if( d<F2 ) {
							F2 = d;
			
						}
					}
				}
				return F1;
			}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);
				if (radius < 1.0)
				{
					return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));
				}
				int sampleRadius = (int)min(radius, 4.0);
				float spacing = radius / (float)sampleRadius;
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				[unroll]
				for (int x = -4; x <= 4; ++x)
				{
					[unroll]
					for (int y = -4; y <= 4; ++y)
					{
						if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)
						{
							float2 offset = TexelSize * float2(x, y) * spacing;
							col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));
						}
					}
				}
				float samplesPerAxis = (float)(sampleRadius * 2 + 1);
				return col / (samplesPerAxis * samplesPerAxis);
			}
			

			VertexOutput vert(VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.ase_texcoord.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord = screenPos;
				o.ase_texcoord1.xyz = ase_positionWS;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord2.zw = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord1.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord2.zw.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.ase_texcoord2.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float AlphaClipThreshold = 0.5;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return unity_SelectionID;
			}

            ENDHLSL
        }
		
	}
	

	

	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19914
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":31,"pos":[-3344,-2704],"params":["Inherit","False","2788.886","2008.525","","51","194","193","165","164","10","7","104","102","168","9","167","6","52","126","51","46","125","45","42","41","39","40","151","152","11","143","195","196","204","205","228","239","258","259","260","261","266","267","307","308","309","310","316","317","346","347","349","350","358","359","366","PARAMETER","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":152,"pos":[-3264,-1104],"params":["Inherit","False","Property","_SurfaceThicknessLower","Surface Thickness Lower","7","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":34,"pos":[-3296,-528],"params":["Inherit","False","2716.396","1388.472","","19","121","133","153","145","138","147","148","149","132","131","130","129","14","116","13","15","12","154","155","SURFACE MASK","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":151,"pos":[-2976,-1104],"params":["Inherit","False","P Surface Thickness Lower","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":138,"pos":[-3264,336],"params":["Inherit","False","11","P Surface Thickness Upper","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":153,"pos":[-3280,256],"params":["Inherit","False","151","P Surface Thickness Lower","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":143,"pos":[-3264,-1200],"params":["Inherit","False","Property","_SurfaceThicknessUpper","Surface Thickness Upper","6","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":129,"pos":[-3168,112],"params":["Inherit","False","0","-1","2","","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":145,"pos":[-3040,288],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":11,"pos":[-2992,-1200],"params":["Inherit","False","P Surface Thickness Upper","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":12,"pos":[-3248,-304],"params":["Inherit","False","0","-1","2","","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":15,"pos":[-3248,-176],"params":["Inherit","False","11","P Surface Thickness Upper","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":130,"pos":[-2896,112],"params":["Inherit","False","FLOAT2","1","0","FLOAT2","0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":131,"pos":[-2896,240],"params":["Inherit","False","2","2","0","FLOAT","0.03125","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":148,"pos":[-2864,400],"params":["Inherit","False","121","M Surface Mask 01","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":13,"pos":[-2976,-304],"params":["Inherit","False","FLOAT2","1","0","FLOAT2","0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":116,"pos":[-2976,-176],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.03125","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor","id":132,"pos":[-2720,144],"params":["Inherit","True","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":149,"pos":[-2624,400],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":40,"pos":[-2640,-2592],"params":["Inherit","False","Constant","_DistortionDirection","Distortion Direction","9","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":62,"pos":[-3280,2368],"params":["Inherit","False","2701.329","1082.82","","19","53","35","54","73","48","47","36","49","61","44","37","78","77","106","123","124","127","128","369","DISTORTION","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor","id":14,"pos":[-2736,-288],"params":["Inherit","True","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":147,"pos":[-2416,288],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":39,"pos":[-2368,-2592],"params":["Inherit","False","P Distortion Direction","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":121,"pos":[-2320,-192],"params":["Inherit","False","M Surface Mask 01","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":133,"pos":[-2288,192],"params":["Inherit","False","M Surface Mask 02","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":41,"pos":[-2624,-2352],"params":["Inherit","False","Property","_DistortionSpeed","Distortion Speed","9","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":77,"pos":[-3152,2640],"params":["Inherit","False","39","P Distortion Direction","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":45,"pos":[-2624,-2256],"params":["Inherit","False","Property","_DistortionScale","Distortion Scale","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0.7","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":154,"pos":[-1952,-32],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":42,"pos":[-2352,-2352],"params":["Inherit","False","P Distortion Speed","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":78,"pos":[-2912,2640],"params":["Inherit","False","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":37,"pos":[-2976,2720],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":316,"pos":[-1792,-1616],"params":["Inherit","False","Property","_LightShaftSpeed","Light Shaft Speed","19","0","Create","True","0","0","0","False","0","False","Object","-1","","0.7","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":125,"pos":[-3264,-1008],"params":["Inherit","False","Property","_SurfaceDistortionMul","Surface Distortion Mul","8","0","Create","True","0","0","0","False","0","False","Object","-1","","5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":46,"pos":[-2352,-2256],"params":["Inherit","False","P Distortion Scale","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":349,"pos":[-1760,-1200],"params":["Inherit","False","Property","_LightShaftTilt","Light Shaft Tilt","17","0","Create","True","0","0","0","False","0","False","Object","-1","","-0.2","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":368,"pos":[-96,3424],"params":["Inherit","False","2785.836","1946.747","","41","296","344","364","345","332","360","343","331","306","342","320","330","312","299","348","341","300","325","321","336","338","319","339","326","315","324","355","353","318","314","328","322","327","298","356","354","323","370","371","375","376","LIGHT SHAFT","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":155,"pos":[-1792,-32],"params":["Inherit","False","M Surface Mask Combined","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":317,"pos":[-1536,-1616],"params":["Inherit","False","P Light Shaft Speed","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":51,"pos":[-2624,-2128],"params":["Inherit","False","Property","_DistortionStrength","Distortion Strength","11","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":44,"pos":[-2768,2656],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":61,"pos":[-2784,2496],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":126,"pos":[-2992,-1008],"params":["Inherit","False","P Surface Distortion Mul","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":49,"pos":[-2928,2832],"params":["Inherit","False","42","P Distortion Speed","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":47,"pos":[-2704,2928],"params":["Inherit","False","46","P Distortion Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":350,"pos":[-1536,-1200],"params":["Inherit","False","P Light Shaft Tilt","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":52,"pos":[-2352,-2128],"params":["Inherit","False","P Distortion Strength","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScaleAndOffsetNode, AmplifyShaderEditor","id":36,"pos":[-2576,2608],"params":["Inherit","False","3","0","FLOAT3","0,0,0","False","1","FLOAT","1","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":48,"pos":[-2640,2768],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":124,"pos":[-2480,3184],"params":["Inherit","False","155","M Surface Mask Combined","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":127,"pos":[-2464,3104],"params":["Inherit","False","126","P Surface Distortion Mul","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":323,"pos":[-48,3872],"params":["Inherit","False","317","P Light Shaft Speed","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":369,"pos":[-2480,2880],"params":["Inherit","False","2","2","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":354,"pos":[-32,4320],"params":["Inherit","False","350","P Light Shaft Tilt","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":356,"pos":[-64,3680],"params":["Inherit","False","350","P Light Shaft Tilt","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":292,"pos":[-162,1486],"params":["Inherit","False","2852","1411","Comment","27","282","280","283","281","277","288","290","284","278","285","275","286","264","257","263","265","256","247","291","251","248","268","250","287","245","243","242","AMBIENT WAVE","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":182,"pos":[-3328,1088],"params":["Inherit","False","2762.934","1091.725","Comment","13","181","179","178","183","197","198","200","201","207","211","212","213","224","WATER COLOR & UNDER WATER COLOR","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":73,"pos":[-2208,2544],"params":["Inherit","False","Constant","_Float0","Float 0","7","0","Create","True","0","0","0","False","0","False","Object","-1","","0.01","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":54,"pos":[-2304,2624],"params":["Inherit","False","52","P Distortion Strength","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":123,"pos":[-2192,3072],"params":["Inherit","False","3","0","FLOAT","1","False","1","FLOAT","5","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":322,"pos":[144,3776],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":328,"pos":[208,3872],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-0.75","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":314,"pos":[192,4448],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":318,"pos":[128,4544],"params":["Inherit","False","317","P Light Shaft Speed","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":167,"pos":[-3280,-2448],"params":["Inherit","False","Property","_WaterColorDeep","Water Color Deep","1","0","Create","True","0","0","0","False","0","False","Object","-1","","0.4431373,0.6117647,0.5215687,0.3764706","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":6,"pos":[-3280,-2656],"params":["Inherit","False","Property","_WaterColorShallow","Water Color Shallow","0","0","Create","False","0","0","0","False","0","False","Object","-1","","0.7450981,0.9333333,0.827451,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.VoronoiNode, AmplifyShaderEditor","id":35,"pos":[-2304,2736],"params":["Inherit","True","0","0","1","0","1","False","3","False","False","False","4","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","3","FLOAT","0","FLOAT2","1","FLOAT2","2"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":308,"pos":[-1776,-1520],"params":["Inherit","False","Property","_LightShaftScale","Light Shaft Scale","15","0","Create","True","0","0","0","False","0","False","Object","-1","","1.4","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":298,"pos":[-32,4128],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":355,"pos":[288,3584],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":327,"pos":[-32,3472],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":353,"pos":[288,4240],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":282,"pos":[-112,2256],"params":["Inherit","False","228","P Ambient Wave Speed","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":207,"pos":[-3232,1344],"params":["Inherit","False","1","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":53,"pos":[-2016,2688],"params":["Inherit","False","4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":324,"pos":[368,3840],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":315,"pos":[384,4464],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":9,"pos":[-3008,-2640],"params":["Inherit","False","P Water Color Shallow","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":168,"pos":[-2976,-2448],"params":["Inherit","False","P Water Color Deep","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":102,"pos":[-2576,-1728],"params":["Inherit","False","Property","_BlurAmountShallow","Blur Amount Shallow","12","0","Create","True","0","0","0","False","0","False","Object","-1","","2","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":204,"pos":[-2576,-1616],"params":["Inherit","False","Property","_BlurAmountDeep","Blur Amount Deep","13","0","Create","True","0","0","0","False","0","False","Object","-1","","12","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":307,"pos":[-1552,-1520],"params":["Inherit","False","P Light Shaft Scale","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":346,"pos":[-1776,-1312],"params":["Inherit","False","Property","_LightShaftDepth","Light Shaft Depth","18","0","Create","True","0","0","0","False","0","False","Object","-1","","2","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":375,"pos":[448,3504],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":376,"pos":[464,4144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":112,"pos":[-3328,3792],"params":["Inherit","False","1992.883","1045.441","Comment","13","88","84","105","107","100","109","86","99","101","203","206","214","388","UNDER WATER BLUR","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":283,"pos":[160,2256],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":280,"pos":[160,2144],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":211,"pos":[-2928,1392],"params":["Inherit","False","M Depth","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":179,"pos":[-2112,1376],"params":["Inherit","False","168","P Water Color Deep","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":178,"pos":[-2112,1296],"params":["Inherit","False","9","P Water Color Shallow","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":212,"pos":[-2112,1456],"params":["Inherit","False","211","M Depth","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":347,"pos":[-1504,-1312],"params":["Inherit","False","P Light Shaft Depth","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":326,"pos":[592,3824],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":339,"pos":[544,3952],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":319,"pos":[560,4352],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":338,"pos":[560,4528],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":193,"pos":[-3248,-2208],"params":["Inherit","False","Property","_UnderwaterTintShallow","Underwater Tint Shallow","2","0","Create","True","0","0","0","False","0","False","Object","-1","","0.9098039,0.9921569,0.9686275,1","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":195,"pos":[-3248,-1952],"params":["Inherit","False","Property","_UnderwaterTintDeep","Underwater Tint Deep","3","0","Create","True","0","0","0","False","0","False","Object","-1","","0.3568628,0.5843138,0.4509804,1","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":106,"pos":[-1792,2688],"params":["Inherit","False","M Distortion Offset","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":104,"pos":[-2352,-1728],"params":["Inherit","False","P Blur Amount Shallow","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":205,"pos":[-2352,-1616],"params":["Inherit","False","P Blur Amount Deep","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":321,"pos":[624,4080],"params":["Inherit","False","307","P Light Shaft Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":336,"pos":[608,4672],"params":["Inherit","False","307","P Light Shaft Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":309,"pos":[-1776,-1424],"params":["Inherit","False","Property","_LightShaftPower","Light Shaft Power","16","0","Create","True","0","0","0","False","0","False","Object","-1","","2","4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":281,"pos":[368,2256],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":181,"pos":[-1808,1328],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":310,"pos":[-1552,-1424],"params":["Inherit","False","P Light Shaft Power","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":101,"pos":[-2912,4112],"params":["Inherit","False","Constant","_Float2","Float 2","8","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":109,"pos":[-3200,4592],"params":["Inherit","False","106","M Distortion Offset","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":105,"pos":[-2656,4384],"params":["Inherit","False","104","P Blur Amount Shallow","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":206,"pos":[-2656,4464],"params":["Inherit","False","205","P Blur Amount Deep","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":214,"pos":[-2624,4560],"params":["Inherit","False","211","M Depth","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor","id":99,"pos":[-2944,4192],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":325,"pos":[736,3824],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":300,"pos":[752,4416],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":341,"pos":[752,4928],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":348,"pos":[704,5088],"params":["Inherit","False","347","P Light Shaft Depth","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":196,"pos":[-2944,-1952],"params":["Inherit","False","P Underwater Tint Deep","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":194,"pos":[-2960,-2192],"params":["Inherit","False","P Underwater Tint Shallow","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":7,"pos":[-3264,-1680],"params":["Inherit","False","Property","_SurfaceColorUpper","Surface Color Upper","4","0","Create","True","0","0","0","False","0","False","Object","-1","","0.8901961,0.9921569,1,0.1960784","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":370,"pos":[848,3984],"params":["Inherit","False","2","2","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":371,"pos":[848,4560],"params":["Inherit","False","2","2","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":86,"pos":[-3218,4144],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":69,"pos":[114,-656],"params":["Inherit","False","2928.262","1673.109","","17","297","244","68","67","65","160","191","192","63","161","162","163","157","156","159","184","363","FINAL","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":258,"pos":[-1840,-2448],"params":["Inherit","False","Property","_AmbientWaveSpeed","Ambient Wave Speed","21","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":261,"pos":[-1840,-2016],"params":["Inherit","False","Property","_AmbientWaveAmptitude","Ambient Wave Amptitude","23","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":259,"pos":[-1824,-2240],"params":["Inherit","False","Property","_AmbientWaveFrequency","Ambient Wave Frequency","22","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":288,"pos":[544,2272],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":290,"pos":[464,2416],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":183,"pos":[-1568,1344],"params":["Inherit","False","M Water Color","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":197,"pos":[-2112,1680],"params":["Inherit","False","194","P Underwater Tint Shallow","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":198,"pos":[-2112,1760],"params":["Inherit","False","196","P Underwater Tint Deep","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":213,"pos":[-2096,1840],"params":["Inherit","False","211","M Depth","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":100,"pos":[-2688,4160],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":107,"pos":[-2896,4384],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":203,"pos":[-2384,4432],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","10","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":299,"pos":[976,4432],"params":["Inherit","False","Simplex2D","True","False","2","0","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":312,"pos":[976,4064],"params":["Inherit","False","310","P Light Shaft Power","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":330,"pos":[992,4544],"params":["Inherit","False","310","P Light Shaft Power","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":320,"pos":[960,3888],"params":["Inherit","False","Simplex2D","True","False","2","0","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":342,"pos":[976,4976],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":358,"pos":[-1760,-1088],"params":["Inherit","False","Property","_LightShaftColor","Light Shaft Color","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2235294,0.3215686,0.2745098,0.3921569","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":164,"pos":[-3264,-1456],"params":["Inherit","False","Property","_SurfaceColorLower","Surface Color Lower","5","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2039216,0.3686275,0.2941177,0.03921569","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":10,"pos":[-3008,-1664],"params":["Inherit","False","P Surface Color Upper","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":388,"pos":[-2816,3904],"params":["Inherit","True","Global","_BehindWaterTex","Behind Water Tex","25","0","Create","True","0","0","0","False","0","False","","-1","None","","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":228,"pos":[-1552,-2432],"params":["Inherit","False","P Ambient Wave Speed","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":239,"pos":[-1552,-2016],"params":["Inherit","False","P Ambient Wave Amptitude","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":260,"pos":[-1536,-2240],"params":["Inherit","False","P Ambient Wave Frequency","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":284,"pos":[768,2368],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":278,"pos":[576,2592],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":285,"pos":[528,2784],"params":["Inherit","False","239","P Ambient Wave Amptitude","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":359,"pos":[-1520,-1088],"params":["Inherit","False","P Light Shaft Color","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":184,"pos":[626,384],"params":["Inherit","False","183","M Water Color","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":159,"pos":[626,464],"params":["Inherit","False","10","P Surface Color Upper","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":156,"pos":[626,544],"params":["Inherit","False","121","M Surface Mask 01","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":200,"pos":[-1776,1728],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":306,"pos":[1216,3968],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":331,"pos":[1232,4448],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":343,"pos":[1104,4976],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":165,"pos":[-3008,-1456],"params":["Inherit","False","P Surface Color Lower","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":84,"pos":[-2400,4128],"params":["Inherit","False","float radius = floor(max(clamp(Blur, 0.0, 128.0) - 1.0, 0.0) * 0.5);\n\nif (radius < 1.0)\n{\n    return tex2Dlod(_BehindWaterTex, float4(UV, 0.0, 0.0));\n}\n\nint sampleRadius = (int)min(radius, 4.0);\nfloat spacing = radius / (float)sampleRadius;\nfloat4 col = float4(0.0, 0.0, 0.0, 0.0);\n\n[unroll]\nfor (int x = -4; x <= 4; ++x)\n{\n    [unroll]\n    for (int y = -4; y <= 4; ++y)\n    {\n        if (abs(x) <= sampleRadius && abs(y) <= sampleRadius)\n        {\n            float2 offset = TexelSize * float2(x, y) * spacing;\n            col += tex2Dlod(_BehindWaterTex, float4(UV + offset, 0.0, 0.0));\n        }\n    }\n}\n\nfloat samplesPerAxis = (float)(sampleRadius * 2 + 1);\nreturn col / (samplesPerAxis * samplesPerAxis);","4","Create","4","False","GrabScreen","FLOAT4","0,0,0,0","In","","Inherit","False","False","TexelSize","FLOAT2","0.001,0.001","In","","Inherit","False","False","UV","FLOAT2","0,0","In","","Inherit","False","False","Blur","FLOAT","1","In","","Inherit","False","Gaussian Blur","False","False","0","","False","4","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0.001,0.001","False","2","FLOAT2","0,0","False","3","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":266,"pos":[-1809.884,-2576.872],"params":["Inherit","False","Property","_AmbientWaveMul","Ambient Wave Mul","20","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":286,"pos":[848,2640],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":264,"pos":[640,1712],"params":["Inherit","False","228","P Ambient Wave Speed","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":257,"pos":[704,1904],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":263,"pos":[608,1536],"params":["Inherit","False","239","P Ambient Wave Amptitude","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":265,"pos":[608,1632],"params":["Inherit","False","260","P Ambient Wave Frequency","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":256,"pos":[704,1808],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":275,"pos":[928,2384],"params":["Inherit","False","Simplex2D","True","False","2","0","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":157,"pos":[946,416],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":163,"pos":[898,656],"params":["Inherit","False","165","P Surface Color Lower","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":162,"pos":[866,736],"params":["Inherit","False","133","M Surface Mask 02","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":88,"pos":[-2064,4144],"params":["Inherit","False","M Under Water Blur","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":360,"pos":[1408,4576],"params":["Inherit","False","359","P Light Shaft Color","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":332,"pos":[1472,4176],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":345,"pos":[1248,4976],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":364,"pos":[1648,4624],"params":["Inherit","False","Alpha Split","-1","","2","07dab7960105b86429ac8eebd729ed6d","0","1","2","COLOR","0,0,0,0","False","2","FLOAT3","0","FLOAT","6"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":201,"pos":[-1536,1744],"params":["Inherit","False","M Under Water Color","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":267,"pos":[-1552,-2560],"params":["Inherit","False","P Ambient Wave Mul","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":247,"pos":[1584,2144],"params":["Inherit","False","1","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":291,"pos":[1152,2416],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":251,"pos":[960,1632],"params":["Inherit","False","float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);\nfloat wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);\nfloat wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);\n\nreturn wave1 + wave2 + wave3;","1","Create","5","False","WaveAmplitude","FLOAT4","0,0,0,0","In","","Inherit","False","False","WaveFrequency","FLOAT4","0,0,0,0","In","","Inherit","False","False","WaveSpeed","FLOAT4","0,0,0,0","In","","Inherit","False","False","Time","FLOAT","0","In","","Inherit","False","True","VertexPos","FLOAT3","0,0,0","In","","Inherit","False","Ambient Wave Sum","False","False","0","","False","5","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT","0","False","4","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":161,"pos":[1218,560],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":63,"pos":[1106,192],"params":["Inherit","False","88","M Under Water Blur","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":192,"pos":[1106,272],"params":["Inherit","False","201","M Under Water Color","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":344,"pos":[1904,4528],"params":["Inherit","False","4","4","0","FLOAT","0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":248,"pos":[1808,2208],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":268,"pos":[1712,2432],"params":["Inherit","False","267","P Ambient Wave Mul","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":250,"pos":[1744,2288],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":287,"pos":[1360,2000],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":191,"pos":[1378,208],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":160,"pos":[1378,384],"params":["Inherit","False","Alpha Split","-1","","3","07dab7960105b86429ac8eebd729ed6d","0","1","2","COLOR","0,0,0,0","False","2","FLOAT3","0","FLOAT","6"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":296,"pos":[2096,4528],"params":["Inherit","False","M Light Shaft Color Final","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":245,"pos":[2064,2128],"params":["Inherit","False","4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":65,"pos":[1650,272],"params":["Inherit","False","3","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","2","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":297,"pos":[1616,480],"params":["Inherit","False","296","M Light Shaft Color Final","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":243,"pos":[2256,2128],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":363,"pos":[1888,288],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":242,"pos":[2416,2128],"params":["Inherit","False","M Ambient Wave Offset","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":67,"pos":[2096,272],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor","id":128,"pos":[-2448,3280],"params":["Inherit","False","374","108","New Note","","1,1,1,1","Given water surface a higher distortion value","0","0"]}
{"type":"AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor","id":224,"pos":[-3280,1136],"params":["Inherit","False","742.6313","164.6506","New Note","","1,1,1,1","Use y of uv on channel 1 to get water depth info.","0","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":277,"pos":[256,2592],"params":["Inherit","False","260","P Ambient Wave Frequency","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":68,"pos":[2272,272],"params":["Inherit","False","COLOR","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","1","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":244,"pos":[2192,464],"params":["Inherit","False","242","M Ambient Wave Offset","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.IntNode, AmplifyShaderEditor","id":366,"pos":[-1072,-2560],"params":["Inherit","False","Property","_StencilReference","Stencil Reference","24","0","Create","True","0","0","0","True","0","False","Object","-1","","123","0","False","0","0","0","1","INT","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":389,"pos":[2512,272],"params":["Float","False","True","-1","3","UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI","0","17","Cainos/Interactive Pixel Water/Pixel Water","199187dac283dbe4a8cb1ea611d70c58","True","Sprite Lit","0","0","Sprite Lit","7","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","5","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","ShaderGraphShader=true","True","0","True","14","all","0","False","True","2","5","False","","10","False","","3","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","True","True","True","0","True","_StencilReference","255","False","","255","False","","0","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=Universal2D","False","False","0","","0","0","Standard","5","Alpha Clipping","0","0","Disable Color Tint","1","0","Vertex Position","1","0","Debug Display","0","0","External Alpha","0","0","0","5","True","True","True","True","True","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":390,"pos":[2512,272],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI","0","1","New Amplify Shader","199187dac283dbe4a8cb1ea611d70c58","True","Sprite Normal","0","1","Sprite Normal","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","5","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","ShaderGraphShader=true","True","0","True","14","all","0","False","True","2","5","False","","10","False","","3","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","0","True","","255","False","","255","False","","0","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=NormalsRendering","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":391,"pos":[2512,272],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI","0","1","New Amplify Shader","199187dac283dbe4a8cb1ea611d70c58","True","Sprite Forward","0","2","Sprite Forward","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","5","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","ShaderGraphShader=true","True","0","True","14","all","0","False","True","2","5","False","","10","False","","3","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","0","True","","255","False","","255","False","","0","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalForward","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":392,"pos":[2512,272],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI","0","1","New Amplify Shader","199187dac283dbe4a8cb1ea611d70c58","True","SceneSelectionPass","0","3","SceneSelectionPass","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","5","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","ShaderGraphShader=true","True","0","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=SceneSelectionPass","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":393,"pos":[2512,272],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI","0","1","New Amplify Shader","199187dac283dbe4a8cb1ea611d70c58","True","ScenePickingPass","0","4","ScenePickingPass","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","5","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","ShaderGraphShader=true","True","0","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Picking","False","False","0","","0","0","Standard","0","False","0"]}
{"wire":[151,0,152,0]}
{"wire":[145,0,153,0]}
{"wire":[145,1,138,0]}
{"wire":[11,0,143,0]}
{"wire":[130,0,129,0]}
{"wire":[131,1,145,0]}
{"wire":[13,0,12,0]}
{"wire":[116,0,15,0]}
{"wire":[132,0,130,1]}
{"wire":[132,1,131,0]}
{"wire":[149,0,148,0]}
{"wire":[14,0,13,1]}
{"wire":[14,1,116,0]}
{"wire":[147,0,132,0]}
{"wire":[147,1,149,0]}
{"wire":[39,0,40,0]}
{"wire":[121,0,14,0]}
{"wire":[133,0,147,0]}
{"wire":[154,0,121,0]}
{"wire":[154,1,133,0]}
{"wire":[42,0,41,0]}
{"wire":[78,0,77,0]}
{"wire":[46,0,45,0]}
{"wire":[155,0,154,0]}
{"wire":[317,0,316,0]}
{"wire":[44,0,78,0]}
{"wire":[44,1,37,0]}
{"wire":[126,0,125,0]}
{"wire":[350,0,349,0]}
{"wire":[52,0,51,0]}
{"wire":[36,0,61,0]}
{"wire":[36,2,44,0]}
{"wire":[48,0,37,0]}
{"wire":[48,1,49,0]}
{"wire":[369,1,47,0]}
{"wire":[123,1,127,0]}
{"wire":[123,2,124,0]}
{"wire":[328,0,323,0]}
{"wire":[35,0,36,0]}
{"wire":[35,1,48,0]}
{"wire":[35,2,369,0]}
{"wire":[355,0,327,2]}
{"wire":[355,1,356,0]}
{"wire":[353,0,298,2]}
{"wire":[353,1,354,0]}
{"wire":[53,0,73,0]}
{"wire":[53,1,54,0]}
{"wire":[53,2,35,0]}
{"wire":[53,3,123,0]}
{"wire":[324,0,322,0]}
{"wire":[324,1,328,0]}
{"wire":[315,0,314,0]}
{"wire":[315,1,318,0]}
{"wire":[9,0,6,0]}
{"wire":[168,0,167,0]}
{"wire":[307,0,308,0]}
{"wire":[375,0,327,1]}
{"wire":[375,1,355,0]}
{"wire":[376,0,298,1]}
{"wire":[376,1,353,0]}
{"wire":[283,0,282,0]}
{"wire":[211,0,207,2]}
{"wire":[347,0,346,0]}
{"wire":[326,0,375,0]}
{"wire":[326,1,324,0]}
{"wire":[339,0,324,0]}
{"wire":[319,0,376,0]}
{"wire":[319,1,315,0]}
{"wire":[338,0,315,0]}
{"wire":[106,0,53,0]}
{"wire":[104,0,102,0]}
{"wire":[205,0,204,0]}
{"wire":[281,0,280,0]}
{"wire":[281,1,283,3]}
{"wire":[181,0,178,0]}
{"wire":[181,1,179,0]}
{"wire":[181,2,212,0]}
{"wire":[310,0,309,0]}
{"wire":[325,0,326,0]}
{"wire":[325,1,339,0]}
{"wire":[300,0,319,0]}
{"wire":[300,1,338,0]}
{"wire":[196,0,195,0]}
{"wire":[194,0,193,0]}
{"wire":[370,1,321,0]}
{"wire":[371,1,336,0]}
{"wire":[288,0,281,0]}
{"wire":[288,1,281,0]}
{"wire":[183,0,181,0]}
{"wire":[100,0,101,0]}
{"wire":[100,1,99,0]}
{"wire":[107,0,86,0]}
{"wire":[107,1,109,0]}
{"wire":[203,0,105,0]}
{"wire":[203,1,206,0]}
{"wire":[203,2,214,0]}
{"wire":[299,0,300,0]}
{"wire":[299,1,371,0]}
{"wire":[320,0,325,0]}
{"wire":[320,1,370,0]}
{"wire":[342,0,341,2]}
{"wire":[342,1,348,0]}
{"wire":[10,0,7,0]}
{"wire":[388,1,86,0]}
{"wire":[228,0,258,0]}
{"wire":[239,0,261,0]}
{"wire":[260,0,259,0]}
{"wire":[284,0,288,0]}
{"wire":[284,1,290,0]}
{"wire":[359,0,358,0]}
{"wire":[200,0,197,0]}
{"wire":[200,1,198,0]}
{"wire":[200,2,213,0]}
{"wire":[306,0,320,0]}
{"wire":[306,1,312,0]}
{"wire":[331,0,299,0]}
{"wire":[331,1,330,0]}
{"wire":[343,0,342,0]}
{"wire":[165,0,164,0]}
{"wire":[84,0,388,0]}
{"wire":[84,1,100,0]}
{"wire":[84,2,107,0]}
{"wire":[84,3,203,0]}
{"wire":[286,0,285,0]}
{"wire":[275,0,284,0]}
{"wire":[275,1,278,3]}
{"wire":[157,0,184,0]}
{"wire":[157,1,159,0]}
{"wire":[157,2,156,0]}
{"wire":[88,0,84,0]}
{"wire":[332,0,306,0]}
{"wire":[332,1,331,0]}
{"wire":[345,0,343,0]}
{"wire":[364,2,360,0]}
{"wire":[201,0,200,0]}
{"wire":[267,0,266,0]}
{"wire":[291,0,275,0]}
{"wire":[291,1,286,3]}
{"wire":[251,0,263,0]}
{"wire":[251,1,265,0]}
{"wire":[251,2,264,0]}
{"wire":[251,3,256,0]}
{"wire":[251,4,257,0]}
{"wire":[161,0,157,0]}
{"wire":[161,1,163,0]}
{"wire":[161,2,162,0]}
{"wire":[344,0,332,0]}
{"wire":[344,1,360,0]}
{"wire":[344,2,364,6]}
{"wire":[344,3,345,0]}
{"wire":[248,0,247,2]}
{"wire":[287,0,251,0]}
{"wire":[287,1,291,0]}
{"wire":[191,0,63,0]}
{"wire":[191,1,192,0]}
{"wire":[160,2,161,0]}
{"wire":[296,0,344,0]}
{"wire":[245,0,287,0]}
{"wire":[245,1,248,0]}
{"wire":[245,2,250,1]}
{"wire":[245,3,268,0]}
{"wire":[65,0,191,0]}
{"wire":[65,1,160,0]}
{"wire":[65,2,160,6]}
{"wire":[243,1,245,0]}
{"wire":[363,0,65,0]}
{"wire":[363,1,297,0]}
{"wire":[242,0,243,0]}
{"wire":[67,0,363,0]}
{"wire":[68,0,67,0]}
{"wire":[68,1,67,1]}
{"wire":[68,2,67,2]}
{"wire":[389,0,68,0]}
{"wire":[389,4,244,0]}
ASEEND*/
//CHKSM=F2BEBD30AD7360FA42B95C955A3447233EBB9F76