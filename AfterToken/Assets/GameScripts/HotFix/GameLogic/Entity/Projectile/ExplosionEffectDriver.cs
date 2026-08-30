using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 爆炸特效驱动：火球（噪声侵蚀半球）+ 冲击波（地面圆环）。
    /// 时间轴见 docs/Proposal/combat/explosion-shader-proposal.md §3.3：
    /// 闪光 0~0.08s 膨胀到 60% → 膨胀到 100%（0.3s）→ 噪声侵蚀消散（0.5s）。
    /// 自驱动自毁，不需要外部每帧 Tick。
    /// 视觉网格运行时构建（球体+面片占位），正式美术资源接入后替换为 prefab。
    /// </summary>
    public class ExplosionEffectDriver : MonoBehaviour
    {
        private const float DURATION = 0.5f;

        private static Material s_fireballMat;
        private static Material s_shockwaveMat;
        private static bool s_shaderWarned;

        private Renderer _fireball;
        private Transform _fireballTf;
        private Renderer _shockwave;
        private Transform _shockwaveTf;
        private MaterialPropertyBlock _mpb;
        private float _t;
        private float _radius;

        /// <summary>
        /// 在指定位置生成一个爆炸特效。position 为地面坐标（y 取 0 层）。
        /// </summary>
        public static ExplosionEffectDriver Create(Vector3 position, float radius, Transform parent)
        {
            if (!EnsureMaterials())
            {
                return null;
            }

            var go = new GameObject("ExplosionEffect");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.position = new Vector3(position.x, 0f, position.z);

            var driver = go.AddComponent<ExplosionEffectDriver>();
            driver._radius = radius;
            driver.Build();
            return driver;
        }

        private static bool EnsureMaterials()
        {
            if (s_fireballMat == null)
            {
                var shader = Shader.Find("AfterToken/ExplosionFireball");
                if (shader != null)
                {
                    s_fireballMat = new Material(shader);
                }
            }
            if (s_shockwaveMat == null)
            {
                var shader = Shader.Find("AfterToken/ExplosionShockwave");
                if (shader != null)
                {
                    s_shockwaveMat = new Material(shader);
                }
            }

            if (s_fireballMat == null || s_shockwaveMat == null)
            {
                if (!s_shaderWarned)
                {
                    s_shaderWarned = true;
                    TEngine.Log.Warning("[ExplosionEffectDriver] 爆炸 shader 未找到，爆炸无视觉（打包时需将 shader 加入 Always Included Shaders）");
                }
                return false;
            }
            return true;
        }

        private void Build()
        {
            // 火球：球体网格，shader 片元裁剪 y<0 只留上半球
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Fireball";
            var ballCol = ball.GetComponent<Collider>();
            if (ballCol != null)
            {
                Destroy(ballCol); // 纯视觉，不参与碰撞
            }
            _fireballTf = ball.transform;
            _fireballTf.SetParent(transform, false);
            _fireballTf.localPosition = Vector3.zero;
            _fireball = ball.GetComponent<Renderer>();
            _fireball.sharedMaterial = s_fireballMat;

            // 冲击波：贴地面片（y=0.05 防 z-fighting），法线朝上
            var wave = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wave.name = "Shockwave";
            var waveCol = wave.GetComponent<Collider>();
            if (waveCol != null)
            {
                Destroy(waveCol);
            }
            _shockwaveTf = wave.transform;
            _shockwaveTf.SetParent(transform, false);
            _shockwaveTf.localPosition = new Vector3(0f, 0.05f, 0f);
            _shockwaveTf.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _shockwave = wave.GetComponent<Renderer>();
            _shockwave.sharedMaterial = s_shockwaveMat;

            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float p = Mathf.Clamp01(_t / DURATION);

            // 火球膨胀曲线：0~0.16 快速到 60%，随后缓增至 100%
            float grow = p < 0.16f
                ? Mathf.Lerp(0.1f, 0.6f, p / 0.16f)
                : Mathf.Lerp(0.6f, 1f, (p - 0.16f) / 0.84f);
            // 球体网格直径 1，放大到爆炸直径
            _fireballTf.localScale = Vector3.one * (_radius * 2f * grow);

            // 冲击波快速外扩并越过火球 footprint（火球是不透明半球，同心同速会被完全遮住），
            // ease-out 到 1.5 倍半径，俯视下圆环始终领先火球轮廓可见
            float waveP = Mathf.Sqrt(p);
            float waveScale = _radius * 2f * Mathf.Lerp(0.4f, 1.5f, waveP);
            _shockwaveTf.localScale = new Vector3(waveScale, waveScale, 1f);

            _mpb.SetFloat("_Progress", p);
            _fireball.SetPropertyBlock(_mpb);
            _shockwave.SetPropertyBlock(_mpb);

            if (_t >= DURATION)
            {
                Destroy(gameObject);
            }
        }
    }
}
