// 爆炸火球：Unlit + 顶点噪声位移（翻腾轮廓）+ FBM 噪声侵蚀（dissolve）+ 黑体辐射色带。
// 项目使用 Built-in 渲染管线（GraphicsSettings.m_CustomRenderPipeline 为空）。
// 网格用球体，片元裁剪 y<0 只保留上半球（俯视视角只需要上半球）。
// 噪声为 hash-based value noise，无需噪声贴图。
Shader "AfterToken/ExplosionFireball"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 3
        _ColorHot ("Hot Color", Color) = (4, 2.5, 0.8, 1)
        _ColorMid ("Mid Color", Color) = (2.5, 0.8, 0.1, 1)
        _ColorCold ("Cold Color", Color) = (0.6, 0.08, 0.02, 1)
    }
    SubShader
    {
        // 不透明 + clip 侵蚀：避免半透明 overdraw 与排序问题（提案 §5）
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 posOS : TEXCOORD0;
            };

            float _Progress;
            float _NoiseScale;
            float4 _ColorHot;
            float4 _ColorMid;
            float4 _ColorCold;

            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(hash3(i + float3(0, 0, 0)), hash3(i + float3(1, 0, 0)), f.x),
                         lerp(hash3(i + float3(0, 1, 0)), hash3(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(hash3(i + float3(0, 0, 1)), hash3(i + float3(1, 0, 1)), f.x),
                         lerp(hash3(i + float3(0, 1, 1)), hash3(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float t = _Progress * 6.0; // 时间推进噪声场，形成翻腾
                float3 pos = v.vertex.xyz;
                float n = fbm(pos * _NoiseScale + float3(0.0, t, 0.0));
                // 位移幅度随进度先升后降（爆炸中段最剧烈）
                float amp = 0.18 * sin(_Progress * 3.14159265);
                pos += v.normal * (n - 0.5) * 2.0 * amp;
                o.posOS = pos;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(i.posOS.y + 0.001); // 只保留上半球

                float n = fbm(i.posOS * _NoiseScale * 1.7 + float3(0.0, _Progress * 6.0, 0.0));
                // 侵蚀阈值随进度上升，火球被噪声吃掉
                float threshold = _Progress * _Progress * 1.1;
                clip(n - threshold);

                // 黑体辐射色带：亮黄 → 橙 → 暗红
                float4 col = lerp(_ColorHot, _ColorMid, saturate(_Progress * 2.0));
                col = lerp(col, _ColorCold, saturate(_Progress * 2.0 - 1.0));
                // 侵蚀边缘提亮，模拟炽热边
                float edge = smoothstep(threshold, threshold + 0.15, n);
                col.rgb *= lerp(1.6, 0.8, edge);
                return col;
            }
            ENDCG
        }
    }
}
