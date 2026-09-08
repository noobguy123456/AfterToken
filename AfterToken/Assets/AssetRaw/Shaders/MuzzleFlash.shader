// 枪口火焰（占位）：贴地面片，星形闪光——白芯 + 沿射击方向（UV.y）的主焰 + 横向副焰。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
// 面片朝向与缩放动画由 EffectDriver（通用面片路径）驱动，本 shader 只负责形状与衰减。
Shader "AfterToken/MuzzleFlash"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Tint ("Tint", Color) = (1, 0.85, 0.4, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One // 加色混合，发光感
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

            float _Progress;
            float4 _Tint;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5; // UV.y 沿射击方向（面片 X+90° 平躺，纹理上方=枪口前方）
                float core = smoothstep(0.22, 0.0, length(c));
                // 主焰：沿射击方向拉长
                float flareV = smoothstep(0.16, 0.0, abs(c.x)) * smoothstep(0.5, 0.05, abs(c.y));
                // 副焰：横向短焰
                float flareH = smoothstep(0.10, 0.0, abs(c.y)) * smoothstep(0.4, 0.1, abs(c.x)) * 0.8;
                float a = saturate(core + flareV + flareH) * (1.0 - _Progress);
                // 白芯：中心提亮过曝
                float3 col = _Tint.rgb * (1.2 + core * 2.5);
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
