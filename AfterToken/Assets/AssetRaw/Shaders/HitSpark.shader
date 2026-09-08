// 命中火花（占位）：贴地面片，径向火花条带随进度向外扩散衰减 + 命中瞬间中心白闪。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
// 命中敌人/场景共用本 shader，颜色靠材质 _Tint 区分（橙红火花 / 灰白碎屑）。
Shader "AfterToken/HitSpark"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Tint ("Tint", Color) = (1, 0.5, 0.15, 1)
        _Spokes ("Spoke Count Half", Float) = 3
        _Sharp ("Spoke Sharpness", Float) = 14
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
            float _Spokes;
            float _Sharp;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = (i.uv - 0.5) * 2.0; // 中心距离 r：边缘中点=1
                float r = length(c);
                float ang = atan2(c.y, c.x);
                // 径向火花条带：abs(cos) 形成 2*_Spokes 根放射条
                float spokes = pow(abs(cos(ang * _Spokes)), _Sharp);
                // 扩散前锋：条带只出现在前锋后方的尾迹区
                float front = _Progress * 1.05;
                float band = smoothstep(front, front - 0.45, r) * smoothstep(0.0, 0.12, r);
                // 命中瞬间中心白闪（前 50% 进度快速熄灭）
                float core = smoothstep(0.35, 0.0, r) * saturate(1.0 - _Progress * 2.0);
                float a = saturate(spokes * band * 1.2 + core) * (1.0 - _Progress * _Progress);
                float3 col = _Tint.rgb * (1.0 + core * 2.0);
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
