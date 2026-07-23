using UnityEngine;
using System.Collections.Generic;

namespace MCFight
{
    public class AttackRangeView : MonoBehaviour
    {
        private float _life = 0.4f;
        private float _elapsed = 0f;
        private SpriteRenderer _sr;
        private Material _mat;
        private static Material _sharedMat;

        public enum Shape { Circle, Cone, Ring, Cross }

        public static AttackRangeView Create(
            Shape shape, float size, float angle, Vector2 pos, int team, float duration = 0.4f)
        {
            var go = new GameObject("AttackRange");
            var view = go.AddComponent<AttackRangeView>();
            view._life = duration;
            view._sr = go.AddComponent<SpriteRenderer>();
            view._sr.sortingOrder = 60;

            if (_sharedMat == null)
            {
                _sharedMat = new Material(Shader.Find("Sprites/Default"));
                _sharedMat.SetInt("_ZWrite", 0);
                _sharedMat.renderQueue = 3000;
            }
            view._sr.material = _sharedMat;

            Color teamColor = team == 0 ? new Color(0.3f, 0.6f, 1f, 0.6f) : new Color(1f, 0.4f, 0.3f, 0.6f);
            Color ringColor = team == 0 ? new Color(0.4f, 0.7f, 1f, 0.9f) : new Color(1f, 0.5f, 0.3f, 0.9f);

            switch (shape)
            {
                case Shape.Circle:
                    view._sr.sprite = MakeCircleSprite((int)size, teamColor, ringColor);
                    break;
                case Shape.Cone:
                    view._sr.sprite = MakeConeSprite((int)size, angle, teamColor, ringColor);
                    break;
                case Shape.Ring:
                    view._sr.sprite = MakeRingSprite((int)size, ringColor);
                    break;
                case Shape.Cross:
                    view._sr.sprite = MakeCrossSprite((int)size, (int)(size * 0.15f), ringColor);
                    break;
            }

            view._sr.color = Color.white;
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = Vector3.one;
            Destroy(go, duration + 0.1f);
            return view;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            if (t >= 1f) t = 1f;

            // 缩放脉冲：先放大后缩小
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 0.5f) * 0.15f;
            transform.localScale = new Vector3(scale, scale, 1);

            // 淡出
            float alpha = 1f - t;
            _sr.color = new Color(1, 1, 1, alpha);
            Destroy(gameObject, _life + 0.05f);
        }

        static Sprite MakeCircleSprite(int radius, Color fill, Color ring)
        {
            int padding = 6;
            int size = Mathf.Max(4, radius * 2 + padding * 2);
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = size / 2;
            var pixels = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float dNorm = d / radius;
                    if (d <= radius - 3)
                    {
                        pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * (1f - dNorm * 0.4f));
                    }
                    else if (d <= radius)
                    {
                        float t = (d - (radius - 3)) / 3f;
                        pixels[y * size + x] = Color.Lerp(fill, ring, t);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }

        static Sprite MakeConeSprite(int length, float angleDeg, Color fill, Color ring)
        {
            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
            int size = Mathf.Max(4, length * 2 + 20);
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = 0; // 锥形顶点在底部中心
            var pixels = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(Mathf.Abs(dx), dy);
                    if (d <= 1) { pixels[y * size + x] = ring; continue; }
                    if (d <= length && angle <= halfAngle)
                    {
                        float dNorm = d / length;
                        pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * (1f - dNorm * 0.5f));
                    }
                    else if (d <= length && angle <= halfAngle + 0.05f)
                    {
                        pixels[y * size + x] = ring;
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 1);
        }

        static Sprite MakeRingSprite(int radius, Color ring)
        {
            int size = Mathf.Max(8, radius * 2 + 8);
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = size / 2;
            var pixels = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    bool inRing = d >= radius - 4 && d <= radius;
                    pixels[y * size + x] = inRing ? ring : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }

        static Sprite MakeCrossSprite(int length, int width, Color color)
        {
            int size = length * 2 + 4;
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = size / 2;
            int hw = width / 2;
            var pixels = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    bool h = Mathf.Abs(y - cy) <= hw && Mathf.Abs(x - cx) <= length / 2;
                    bool v = Mathf.Abs(x - cx) <= hw && Mathf.Abs(y - cy) <= length / 2;
                    pixels[y * size + x] = (h || v) ? color : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }
    }
}