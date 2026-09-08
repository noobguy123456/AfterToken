// 爆炸焦痕（占位）：贴地暗色软边圆，带少量噪声让轮廓不机械；_Fade 由 EffectDriver 驱动线性淡出。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
Shader "AfterToken/ScorchDecal"
{
    Properties
    {
        _Fade ("Fade", Range(0, 1)) = 1
        _Color ("Color", Color) = (0.02, 0.02, 0.02, 0.85)
        _Edge ("Soft Edge", Range(0.01, 0.6)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha // 普通透明混合（暗色焦痕，不是发光体）
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Fade;
            float4 _Color;
            float _Edge;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash2(float2 p)
            {
                p = frac(p * float2(0.3183099, 0.3678794) + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            float vnoise(float2 x)
            {
                float2 i = floor(x);
                float2 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash2(i + float2(0, 0)), hash2(i + float2(1, 0)), f.x),
                    lerp(hash2(i + float2(0, 1)), hash2(i + float2(1, 1)), f.x),
                    f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float d = length(c) * 2.0;
                // 轮廓噪声扰动，避免正圆的塑料感
                float n = vnoise(i.uv * 7.0) - 0.5;
                d += n * 0.18;
                float a = (1.0 - smoothstep(1.0 - _Edge, 1.0, d)) * _Fade * _Color.a;
                return fixed4(_Color.rgb, a);
            }
            ENDCG
        }
    }
}
