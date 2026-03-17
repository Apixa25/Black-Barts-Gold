// ============================================================================
// BBGGlow.shader
// Black Bart's Gold — SDF-Based Additive UI Glow
// Path: Assets/Shaders/BBGGlow.shader
// ============================================================================
// Generates a smooth rectangular glow halo using a rounded-rectangle signed
// distance field. Applied to the glow layer of BBGButton / BBGPanel.
//
// Blend: SrcAlpha One (additive) — glow ADDS light to whatever is behind it.
// Vertex color (Image.color) controls overall intensity from C#.
// Built-in micro-shimmer keeps the glow alive even at rest.
//
// Key properties:
//   _GlowColor (HDR)  – base glow color (amber, cyan, magenta, etc.)
//   _GlowIntensity    – overall brightness multiplier
//   _GlowFalloff      – how quickly glow fades from the border (higher = tighter)
//   _CornerRadius      – rounded rectangle corner radius in UV space
//   _InnerPad          – how far in from UV edge the glow ring sits
//   _PulseSpeed        – speed of built-in shimmer
//   _PulseAmount       – strength of shimmer (0 = off)
// ============================================================================

Shader "BBG/UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _GlowColor ("Glow Color", Color) = (1, 0.7, 0.1, 1)
        _GlowIntensity ("Intensity", Range(0, 5)) = 1.5
        _GlowFalloff ("Falloff", Range(1, 30)) = 8.0
        _CornerRadius ("Corner Radius", Range(0, 0.25)) = 0.06
        _InnerPad ("Inner Padding", Range(0, 0.2)) = 0.04
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.12

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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "BBGGlow"
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

            half4  _GlowColor;
            half   _GlowIntensity;
            half   _GlowFalloff;
            half   _CornerRadius;
            half   _InnerPad;
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
                half2 uv = IN.texcoord - 0.5;
                half2 halfSize = half2(0.5 - _InnerPad, 0.5 - _InnerPad);
                half2 d = abs(uv) - halfSize + _CornerRadius;
                half sdf = length(max(d, 0.0)) - _CornerRadius + min(max(d.x, d.y), 0.0);

                half glowRaw = exp(-abs(sdf) * _GlowFalloff);

                half pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);
                half intensity = glowRaw * _GlowIntensity * pulse;

                fixed4 col;
                col.rgb = _GlowColor.rgb * intensity;
                col.a = saturate(intensity) * _GlowColor.a;

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
