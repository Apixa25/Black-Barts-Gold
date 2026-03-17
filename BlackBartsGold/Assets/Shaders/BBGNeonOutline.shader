// ============================================================================
// BBGNeonOutline.shader
// Black Bart's Gold — Neon Border Outline for UI Panels
// Path: Assets/Shaders/BBGNeonOutline.shader
// ============================================================================
// Generates a thin glowing border with a bright core and soft outer glow.
// Applied to the glow layer of BBGPanel for the western-steampunk neon effect.
//
// Blend: SrcAlpha OneMinusSrcAlpha (standard alpha) — the bright core is
// nearly opaque; the softer glow fades transparently.
//
// Key properties:
//   _NeonColor (HDR) – border color (cyan, amber, magenta, etc.)
//   _BorderWidth     – distance from UV edge where the core line sits
//   _CoreBrightness  – intensity of the bright core line
//   _CoreSharpness   – how tight the core line is
//   _GlowIntensity   – intensity of the softer outer glow
//   _GlowFalloff     – falloff speed for outer glow
//   _PulseSpeed      – animation speed (0 = static)
//   _PulseAmount     – strength of pulse
// ============================================================================

Shader "BBG/UI/NeonOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _NeonColor ("Neon Color", Color) = (0, 0.9, 1, 1)
        _BorderWidth ("Border Width", Range(0.005, 0.15)) = 0.035
        _CoreBrightness ("Core Brightness", Range(0, 5)) = 2.5
        _CoreSharpness ("Core Sharpness", Range(10, 200)) = 60.0
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.8
        _GlowFalloff ("Glow Falloff", Range(2, 40)) = 12.0
        _CornerRadius ("Corner Radius", Range(0, 0.2)) = 0.04
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 0.4)) = 0.1

        // --- Unity UI stencil / clipping ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "BBGNeonOutline"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                half2  texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            half4  _NeonColor;
            half   _BorderWidth;
            half   _CoreBrightness;
            half   _CoreSharpness;
            half   _GlowIntensity;
            half   _GlowFalloff;
            half   _CornerRadius;
            half   _PulseSpeed;
            half   _PulseAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // SDF: distance from rounded-rect edge in UV space
                half2 uv = IN.texcoord - 0.5;
                half2 halfSize = half2(0.5, 0.5) - _CornerRadius;
                half2 d = abs(uv) - halfSize;
                half sdf = length(max(d, 0.0)) - _CornerRadius + min(max(d.x, d.y), 0.0);

                // sdf < 0 = inside, sdf > 0 = outside
                // Border sits at sdf = -_BorderWidth (just inside the edge)
                half borderDist = abs(sdf + _BorderWidth);

                // Bright core (tight exponential peak at the border line)
                half core = exp(-borderDist * _CoreSharpness) * _CoreBrightness;

                // Softer outer glow (broader falloff)
                half glow = exp(-borderDist * _GlowFalloff) * _GlowIntensity;

                // Animated pulse
                half pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);

                half intensity = (core + glow) * pulse;

                fixed4 col;
                col.rgb = _NeonColor.rgb * intensity;
                col.a = saturate(intensity) * _NeonColor.a;

                col *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
