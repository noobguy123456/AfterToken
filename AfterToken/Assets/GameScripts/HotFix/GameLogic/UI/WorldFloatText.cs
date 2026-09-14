using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 世界空间飘字工具（上飘 + 面向相机 + 淡出销毁）。
    /// 从 BuildingPlacementSystem 的私有实现抽为公共静态入口，供好感度提示/记忆好奇提示等复用。
    /// 用 legacy Text（OS 字体回退可显中文；世界空间 TMP 字库覆盖不全时中文会显示方框）。
    /// </summary>
    public static class WorldFloatText
    {
        /// <summary>在指定世界位置显示飘字。cam 传 null 时自动取 Camera.main。</summary>
        public static void Show(string message, Vector3 worldPos, Color color)
        {
            var cam = Camera.main;
            var go = new GameObject("WorldFloatText");
            go.transform.position = worldPos + Vector3.up * 2.5f;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 80f);
            rect.localScale = Vector3.one * 0.01f; // 世界空间 UI 标准缩放（600px ≈ 6m 宽）

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 56;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var anim = go.AddComponent<Anim>();
            anim.Init(text, cam);
        }

        private class Anim : MonoBehaviour
        {
            private UnityEngine.UI.Text _text;
            private Camera _cam;
            private float _timer;
            private const float LifeTime = 1.2f;

            public void Init(UnityEngine.UI.Text text, Camera cam)
            {
                _text = text;
                _cam = cam;
            }

            private void Update()
            {
                _timer += Time.deltaTime;
                transform.position += Vector3.up * (0.8f * Time.deltaTime);
                if (_cam != null)
                {
                    transform.LookAt(_cam.transform);
                    transform.Rotate(0f, 180f, 0f); // 翻转让文字正面朝相机
                }
                if (_text != null)
                {
                    Color c = _text.color;
                    c.a = Mathf.Clamp01(1f - _timer / LifeTime);
                    _text.color = c;
                }
                if (_timer >= LifeTime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
