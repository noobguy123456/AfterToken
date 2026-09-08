// 拾取光晕（占位）：贴地软边光圈 + 呼吸脉动，提示掉落物可拾取。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
// 呼吸用 _Time 自驱动（无需 EffectDriver 推进 _Progress），世界坐标做相位避免多个光圈同步闪烁。
Shader "AfterToken/PickupGlow"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 0.85, 0.45, 0.8)
        _PulseSpeed ("Pulse Speed", Float) = 2.5
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
                float3 worldPos : TEXCOORD1;
            };

            float4 _Tint;
            float _PulseSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0; // 中心距离：边缘中点=1
                // 软边光环带 + 环内微弱余光
                float ring = smoothstep(0.5, 0.68, d) * smoothstep(0.98, 0.82, d);
                float glow = saturate(1.0 - d) * 0.22;
                // 呼吸脉动，世界坐标做随机相位（同 NoteEntity 随机相位思路）
                float phase = (i.worldPos.x + i.worldPos.z) * 1.71;
                float pulse = 0.65 + 0.35 * sin(_Time.y * _PulseSpeed + phase);
                float a = (ring + glow) * pulse * _Tint.a;
                return fixed4(_Tint.rgb, a);
            }
            ENDCG
        }
    }
}
