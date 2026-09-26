// The shield's plasma (docs/ANIMATIONS.md section 6, docs/DIRECTION_ARTISTIQUE.md §6.7: an unstable, crackling field
// that flickers as it takes a hit). Additive and unlit: brighter at the edges, where the eye looks through more of the
// field (a Fresnel term), crossed by a crackle that scrolls (the texture Art/Effects/Effets.fbm/Plasma.png), and
// flickering now and then. The game tints it with the seat colour and fades it through _BaseColor, as it does the
// placeholder glow (ShieldBubble), so it plugs into the theme's Shield Material without code.
Shader "Vortex/Plasma"
{
    Properties
    {
        [HDR] _BaseColor("Couleur (donnée par le jeu)", Color) = (1, 1, 1, 1)
        _BaseMap("Grésillement", 2D) = "white" {}
        _Intensity("Éclat", Float) = 5
        _Rim("Bord : netteté", Range(0.5, 8)) = 2.5
        _Core("Voile au centre", Range(0, 1)) = 0.12
        _Scroll("Défilement (xy, zw)", Vector) = (0.04, 0.11, -0.07, 0.05)
        _Flicker("Clignotement", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Plasma"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Intensity;
                float _Rim;
                float _Core;
                float4 _Scroll;
                float _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(position.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float facing = saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                float rim = pow(1.0 - facing, _Rim);

                // Two layers of the crackle scrolling apart: where they meet, the field sparks.
                float a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + _Time.y * _Scroll.xy).r;
                float b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * 1.7 + _Time.y * _Scroll.zw).r;
                float crackle = saturate(a * b * 2.5);

                // Now and then the field falters for a moment.
                float tick = floor(_Time.y * 20.0);
                float falter = step(0.82, frac(sin(tick * 12.9898) * 43758.5453));
                float flicker = 1.0 - (_Flicker * falter);

                float light = ((rim * (0.55 + crackle)) + (_Core * crackle)) * flicker * _Intensity;
                return half4(_BaseColor.rgb * light, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
