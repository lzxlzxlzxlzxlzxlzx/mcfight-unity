using UnityEngine;

namespace MCFight
{
    /// <summary> 投射物渲染视图 </summary>
    public class ProjectileView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private TrailRenderer _trail;
        private static Sprite[] _projFrames;
        private static Sprite[] _waveFrames;
        private float _animTimer;
        private int _frameIdx;
        private float _projSize;
        private bool _isWave;

        static Sprite[] GetProjFrames()
        {
            if (_projFrames == null)
                _projFrames = Resources.LoadAll<Sprite>("VFX/projectile_spritesheet");
            return _projFrames;
        }

        static Sprite[] GetWaveFrames()
        {
            if (_waveFrames == null)
                _waveFrames = Resources.LoadAll<Sprite>("VFX/soundwave_spritesheet");
            return _waveFrames;
        }

        public void Init(ProjectileData data)
        {
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            if (_trail == null)
            {
                var trailGo = new GameObject("Trail");
                trailGo.transform.SetParent(transform, false);
                _trail = trailGo.AddComponent<TrailRenderer>();
                _trail.time = 0.15f;
                _trail.startWidth = 4f;
                _trail.endWidth = 0f;
            }

            _isWave = (data.Kind == ProjectileKind.ForsakenSonic);
            Color teamColor = data.Team == 0 ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.45f, 0.3f);
            _projSize = 30f;

            switch (data.Kind)
            {
                case ProjectileKind.HarbWither:
                case ProjectileKind.HarbHoming:
                    teamColor = new Color(0.6f, 0.3f, 0.9f); _projSize = 50f; break;
                case ProjectileKind.HarbLaser:
                    teamColor = new Color(1f, 0.3f, 0.2f); _projSize = 36f; break;
                case ProjectileKind.RevenantBone:
                    teamColor = new Color(0.9f, 0.85f, 0.7f); _projSize = 44f; break;
                case ProjectileKind.ForsakenSonic:
                    teamColor = new Color(0.3f, 0.85f, 0.95f, 0.7f); _projSize = 60f; _trail.startWidth = 0; break;
                case ProjectileKind.IceBomb:
                    teamColor = new Color(0.5f, 0.85f, 1f); _projSize = 50f; break;
                case ProjectileKind.ProwlerMissile:
                    teamColor = new Color(0.9f, 0.4f, 1f); _projSize = 40f; break;
            }

            // Pick spritesheet: soundwave for ForsakenSonic, projectile for others
            var frames = _isWave ? GetWaveFrames() : GetProjFrames();
            if (frames != null && frames.Length > 0)
            {
                _sr.sprite = frames[0];
                float spriteWorldW = frames[0].rect.width / 100f;
                float spriteWorldH = frames[0].rect.height / 100f;
                float scale = _projSize / Mathf.Max(spriteWorldW, spriteWorldH);
                transform.localScale = new Vector3(scale, scale, 1);
            }
            else
            {
                if (_dotSprite == null)
                {
                    var tex = new Texture2D(4, 4);
                    for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
                    tex.Apply();
                    _dotSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1);
                }
                _sr.sprite = _dotSprite;
                transform.localScale = new Vector3(_projSize, _projSize, 1);
            }

            _sr.color = teamColor;
            _sr.sortingOrder = 150;
            transform.position = new Vector3(data.X, data.Y, 0);
            _animTimer = 0;
            _frameIdx = 0;
            if (!_isWave)
            {
                _trail.startColor = teamColor;
                _trail.endColor = new Color(teamColor.r, teamColor.g, teamColor.b, 0);
            }
        }

        static Sprite _dotSprite;

        public void Sync(ProjectileData data)
        {
            transform.position = new Vector3(data.X, data.Y, 0);
            float angle = Mathf.Atan2(data.DirY, data.DirX) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            var frames = _isWave ? GetWaveFrames() : GetProjFrames();
            if (frames != null && frames.Length > 0)
            {
                _animTimer += Time.deltaTime;
                float frameDur = 1f / 30f;
                if (_animTimer >= frameDur)
                {
                    _animTimer -= frameDur;
                    _frameIdx = (_frameIdx + 1) % frames.Length;
                    _sr.sprite = frames[_frameIdx];
                }
            }
        }
    }

    /// <summary> 区域效果渲染视图 </summary>
    public class EffectView : MonoBehaviour
    {
        private SpriteRenderer _fillSr;
        private SpriteRenderer _ringSr;
        private AreaEffectData _data;
        private float _initTime;
        private bool _isActive = false;

        public void Init(AreaEffectData data)
        {
            _data = data;
            _initTime = Time.time;

            // Shockwave: skip EffectView entirely, AttackRangeView handles it
            if (data.Type == AreaEffectType.Shockwave)
            {
                gameObject.SetActive(false);
                return;
            }

            _isActive = true;
            if (_fillSr == null)
                _fillSr = gameObject.AddComponent<SpriteRenderer>();

            // Load appropriate sprite based on area type
            switch (data.Type)
            {
                case AreaEffectType.LavaPatch:
                    _fillSr.sprite = LoadAreaSprite("VFX/lava_circle");
                    _fillSr.color = new Color(1f, 0.5f, 0.1f, 0.8f);
                    break;
                case AreaEffectType.FrostZone:
                    _fillSr.sprite = LoadAreaSprite("VFX/icemist_spritesheet");
                    _fillSr.color = new Color(0.4f, 0.75f, 1f, 0.6f);
                    break;
                case AreaEffectType.PollutionZone:
                    _fillSr.sprite = MakeCircleSprite((int)data.Radius);
                    _fillSr.color = new Color(0.35f, 0.85f, 0.25f, 0.3f);
                    break;
                case AreaEffectType.SandTornado:
                    _fillSr.sprite = LoadAreaSprite("VFX/sandstorm_spritesheet");
                    _fillSr.color = new Color(1f, 1f, 1f, 0.8f);
                    break;
                default:
                    _fillSr.sprite = MakeCircleSprite((int)data.Radius);
                    _fillSr.color = new Color(1f, 1f, 1f, 0.1f);
                    break;
            }
            _fillSr.sortingOrder = 50;
            transform.position = new Vector3(data.X, data.Y, 0);

            // Scale sprite to match radius
            if (_fillSr.sprite != null)
            {
                float spriteWorldSize = Mathf.Max(_fillSr.sprite.rect.width, _fillSr.sprite.rect.height) / 100f;
                float scale = (data.Radius * 2f) / spriteWorldSize;
                transform.localScale = new Vector3(scale, scale, 1);
            }
        }

        static Sprite LoadAreaSprite(string path)
        {
            if (path.Contains("_spritesheet"))
            {
                var sprites = Resources.LoadAll<Sprite>(path);
                if (sprites != null && sprites.Length > 0) return sprites[0];
            }
            return Resources.Load<Sprite>(path);
        }

        public void Sync(AreaEffectData data)
        {
            if (!_isActive) return;
            _data = data;
            transform.position = new Vector3(data.X, data.Y, 0);

            if (data.Type == AreaEffectType.SandTornado)
                transform.rotation = Quaternion.Euler(0, 0, data.OrbitAngle * Mathf.Rad2Deg);

            if (data.Type == AreaEffectType.LavaPatch || data.Type == AreaEffectType.FrostZone || data.Type == AreaEffectType.PollutionZone)
            {
                float pulse = 0.15f + Mathf.Sin((Time.time - _initTime) * 3f) * 0.05f;
                _fillSr.color = new Color(_fillSr.color.r, _fillSr.color.g, _fillSr.color.b, pulse);
                if (data.Remaining < 2f)
                    _fillSr.color = new Color(_fillSr.color.r, _fillSr.color.g, _fillSr.color.b, pulse * (data.Remaining / 2f));
            }
        }

        public static Sprite MakeCircleSprite(int radius)
        {
            int size = Mathf.Max(4, radius * 2);
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = size / 2;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = d <= radius ? (1f - d / radius) * 0.5f + 0.1f : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }

        public static Sprite MakeRingSprite(int radius)
        {
            int size = Mathf.Max(8, radius * 2);
            var tex = new Texture2D(size, size);
            int cx = size / 2, cy = size / 2;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    bool inRing = d >= radius - 4 && d <= radius;
                    tex.SetPixel(x, y, inRing ? Color.white : new Color(0, 0, 0, 0));
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }
    }

    /// <summary> 光束渲染视图 </summary>
    public class BeamView : MonoBehaviour
    {
        private SpriteRenderer _coreSr;
        private static Sprite[] _beamFrames;
        private static Material _beamMat;
        private Color _baseColor;
        private float _animTimer;
        private int _frameIdx;
        private const float FRAME_RATE = 20f;

        static Sprite[] GetBeamFrames()
        {
            if (_beamFrames == null)
            {
                var sprites = Resources.LoadAll<Sprite>("VFX/beam_spritesheet");
                if (sprites != null && sprites.Length > 0)
                    _beamFrames = sprites;
            }
            return _beamFrames;
        }

        static Material GetBeamMat()
        {
            if (_beamMat == null)
            {
                var shader = Shader.Find("Sprites/BeamAdditive");
                if (shader != null)
                    _beamMat = new Material(shader);
            }
            return _beamMat;
        }

        public void Init(ActiveBeamData data)
        {
            if (_coreSr == null)
                _coreSr = gameObject.AddComponent<SpriteRenderer>();

            // Load beam frames from sprite sheet
            var frames = GetBeamFrames();
            bool hasFrames = frames != null && frames.Length > 0;

            // Color tint based on beam kind
            switch (data.Kind)
            {
                case BeamKind.Tremor:
                    _baseColor = new Color(1f, 0.8f, 0.3f, 1f);
                    break;
                case BeamKind.HarbingerDeath:
                    _baseColor = new Color(1f, 0.3f, 0.3f, 1f);
                    break;
                case BeamKind.ProwlerRay:
                    _baseColor = new Color(0.9f, 0.5f, 1f, 1f);
                    break;
                default:
                    _baseColor = new Color(1f, 1f, 1f, 0.9f);
                    break;
            }

            if (hasFrames)
            {
                _coreSr.sprite = frames[0];
                // Use default sprite shader since PNG now has proper alpha
                // No need for custom additive shader
            }
            else
            {
                _coreSr.sprite = GetBeamSprite();
            }

            _coreSr.color = _baseColor;
            _coreSr.sortingOrder = 120;
            _animTimer = 0;
            _frameIdx = 0;
            UpdateTransform(data);
        }

        public void Sync(ActiveBeamData data)
        {
            UpdateTransform(data);

            // Animate sprite frames
            _animTimer += Time.deltaTime;
            var frames = GetBeamFrames();
            if (frames != null && frames.Length > 0 && _animTimer >= 1f / FRAME_RATE)
            {
                _animTimer -= 1f / FRAME_RATE;
                _frameIdx = (_frameIdx + 1) % frames.Length;
                _coreSr.sprite = frames[_frameIdx];
            }

            // Pulse
            float pulse = 0.85f + Mathf.Sin(Time.time * 15f) * 0.15f;
            if (data.Remaining < 0.5f)
            {
                float fade = data.Remaining * 2f;
                _coreSr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * fade * pulse);
            }
            else
            {
                _coreSr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * pulse);
            }
        }

        void UpdateTransform(ActiveBeamData data)
        {
            float len = Mathf.Max(1f, data.Length);
            float hw = Mathf.Max(2f, data.HalfWidth);
            var pos = new Vector3(data.OriginX, data.OriginY, 0);
            float angle = Mathf.Atan2(data.DirY, data.DirX) * Mathf.Rad2Deg;
            _coreSr.transform.position = pos;
            _coreSr.transform.rotation = Quaternion.Euler(0, 0, angle);
            // Sprite: 500x500 at 100 pixelsPerUnit, pivot at (0, 0.5) = left edge
            float spriteWorldSize = 500f / 100f; // = 5 world units
            float scaleX = len / spriteWorldSize;
            float scaleY = (hw * 2f) / spriteWorldSize;
            _coreSr.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }

        // Fallback: procedural beam sprite
        static Sprite _fallbackSprite;
        static Sprite GetBeamSprite()
        {
            if (_fallbackSprite == null)
            {
                int w = 64, h = 16;
                var tex = new Texture2D(w, h);
                tex.filterMode = FilterMode.Bilinear;
                for (int x = 0; x < w; x++)
                {
                    float fx = (float)x / (w - 1);
                    float alphaX = fx < 0.05f ? fx / 0.05f : (fx > 0.95f ? (1f - fx) / 0.05f : 1f);
                    for (int y = 0; y < h; y++)
                    {
                        float fy = (float)y / (h - 1) - 0.5f;
                        float alphaY = Mathf.Max(0f, 1f - Mathf.Abs(fy) * 2f);
                        alphaY = alphaY * alphaY * alphaY * alphaY;
                        tex.SetPixel(x, y, new Color(1, 1, 1, alphaX * alphaY));
                    }
                }
                tex.Apply();
                _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1);
                _fallbackSprite.hideFlags = HideFlags.DontSave;
            }
            return _fallbackSprite;
        }
    }

    /// <summary> 伤害数字飘字视图 </summary>
    public class DamageNumberView : MonoBehaviour
    {
        private TextMesh _tm;
        private float _life = 0.7f;
        private float _elapsed = 0;
        private Vector3 _startPos;
        private float _baseScale = 1f;

        public void Init(float damage, DamageCategory category, Vector3 pos)
        {
            if (_tm == null)
            {
                _tm = gameObject.AddComponent<TextMesh>();
                _tm.fontSize = 20;
                _tm.characterSize = 0.8f;
                _tm.anchor = TextAnchor.LowerCenter;
            }

            string text = Mathf.RoundToInt(damage).ToString();
            if (damage >= 50) text += "!";

            Color color = Color.white;
            switch (category)
            {
                case DamageCategory.Melee: color = Color.white; break;
                case DamageCategory.Ranged: color = new Color(1f, 0.9f, 0.3f); break;
                case DamageCategory.Beam: color = new Color(1f, 0.5f, 0.1f); break;
                case DamageCategory.Explosion: color = new Color(1f, 0.2f, 0.1f); break;
                case DamageCategory.True: color = new Color(0.8f, 0.2f, 0.9f); break;
            }

            _tm.text = text;
            _tm.color = color;
            _baseScale = damage >= 50 ? 1.4f : 1f;

            _startPos = pos + new Vector3(Random.Range(-8, 8), 0, 0);
            transform.position = _startPos;
            _elapsed = 0;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.position = _startPos + new Vector3(0, 25 * t, 0);
            float scale = _baseScale * (t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) * 0.3f);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            _tm.color = new Color(_tm.color.r, _tm.color.g, _tm.color.b, 1f - t);
        }
    }
}
