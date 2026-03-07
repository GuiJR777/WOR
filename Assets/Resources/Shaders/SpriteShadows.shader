// Purpose: Sprite shader that casts and receives 3D shadows in URP.
Shader "Custom/SpriteShadow" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [PerRendererData] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [PerRendererData] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.1
        _ShadowMin("Shadow Minimum Light", Range(0,1)) = 0.35
    }

    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RendererColor;
                float4 _Flip;
                half _Cutoff;
                half _ShadowMin;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionOS = input.positionOS;
                positionOS.xy *= _Flip.xy;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.uv = input.uv;
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 c = tex * input.color;

                Light mainLight = GetMainLight(input.shadowCoord);
                half shadowAttenuation = saturate(mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                half lighting = lerp(_ShadowMin, 1.0h, shadowAttenuation);

                c.rgb *= lighting;
                return c;
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RendererColor;
                float4 _Flip;
                half _Cutoff;
                half _ShadowMin;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alphaTint : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetShadowPosition(float3 positionWS) {
                float3 normalWS = TransformObjectToWorldDir(float3(0.0, 0.0, -1.0));
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionOS = input.positionOS;
                positionOS.xy *= _Flip.xy;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
                output.positionCS = GetShadowPosition(vertexInput.positionWS);
                output.uv = input.uv;
                output.alphaTint = input.color.a * _Color.a * _RendererColor.a;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                alpha *= input.alphaTint;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow fullforwardshadows nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #include "UnitySprites.cginc"

        struct Input {
            float2 uv_MainTex;
            fixed4 color;
        };

        void vert(inout appdata_full v, out Input o) {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = SampleSpriteTexture(IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb * c.a;
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Sprites/Default"
}
