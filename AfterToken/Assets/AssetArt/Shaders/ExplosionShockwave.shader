// 爆炸冲击波（地面圆环）：贴地面片，亮环前锋 + alpha 随进度衰减。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
// 外扩动画由 ExplosionEffectDriver 驱动 localScale，本 shader 只负责圆环形状与淡出。
Shader "AfterToken/ExplosionShockwave"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Color ("Color", Color) = (3, 1.2, 0.3, 1)
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.15
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
            float4 _Color;
            float _RingWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 面片 UV 归一到中心距离 d（边缘中点=1，角>1）
                float d = length(i.uv - 0.5) * 2.0;
                // 亮环前锋：靠近外缘的窄带
                float band = 1.0 - saturate(abs(d - 0.92) / _RingWidth);
                // 环内侧一点微弱余光
                float glow = saturate(1.0 - d) * 0.25;
                float alpha = (band + glow) * (1.0 - _Progress);
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
