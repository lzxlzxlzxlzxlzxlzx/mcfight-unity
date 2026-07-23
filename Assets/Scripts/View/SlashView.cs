using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 扇形斩击特效：加载 slash_spritesheet 的 4 帧动画，
    /// 以施法者位置为起点，朝目标方向播放扇形挥砍效果。
    /// </summary>
    public class SlashView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private static Sprite[] _frames;
        private float _animTimer;
        private int _frameIdx;
        private float _duration;
        private float _elapsed;
        private float _worldScale;
        private bool _playing;

        static Sprite[] GetFrames()
        {
            if (_frames == null)
                _frames = Resources.LoadAll<Sprite>("VFX/slash_spritesheet");
            return _frames;
        }

        /// <summary>
        /// 在指定位置播放扇形斩击特效。
        /// </summary>
        /// <param name="originX">施法者 X</param>
        /// <param name="originY">施法者 Y</param>
        /// <param name="dirX">斩击方向 X（归一化）</param>
        /// <param name="dirY">斩击方向 Y（归一化）</param>
        /// <param name="range">斩击范围（世界单位）</param>
        /// <param name="duration">持续时间（秒）</param>
        public static void Play(float originX, float originY, float dirX, float dirY, float range, float duration = 0.6f)
        {
            var frames = GetFrames();
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning("[SlashView] No slash frames loaded");
                return;
            }

            var go = new GameObject("SlashVFX");
            var sv = go.AddComponent<SlashView>();
            sv._sr = go.AddComponent<SpriteRenderer>();
            sv._sr.sprite = frames[0];
            sv._sr.sortingOrder = 110;

            // Sprite is 234x196, pivot at left-center (0, 0.5)
            // Scale so that the sprite width = range in world units
            float spriteWorldW = 234f / 100f;  // 2.34 world units
            float spriteWorldH = 196f / 100f; // 1.96 world units
            float scaleX = range / spriteWorldW;
            // Height scales with range proportionally
            float scaleY = range / spriteWorldW * (spriteWorldH / spriteWorldW);
            sv._worldScale = 1f;
            sv._sr.transform.localScale = new Vector3(scaleX, scaleY, 1);

            // Position at the caster, rotate to face the target direction
            sv._sr.transform.position = new Vector3(originX, originY, 0);
            float angle = Mathf.Atan2(dirY, dirX) * Mathf.Rad2Deg;
            sv._sr.transform.rotation = Quaternion.Euler(0, 0, angle);

            sv._duration = duration;
            sv._elapsed = 0f;
            sv._frameIdx = 0;
            sv._animTimer = 0f;
            sv._playing = true;

            Destroy(go, duration + 0.1f);
        }

        void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            _animTimer += Time.deltaTime;

            var frames = GetFrames();
            if (frames == null || frames.Length == 0) return;

            // Cycle through frames
            float frameDuration = _duration / frames.Length;
            int newFrame = Mathf.Min((int)(_elapsed / frameDuration), frames.Length - 1);
            if (newFrame != _frameIdx)
            {
                _frameIdx = newFrame;
                _sr.sprite = frames[_frameIdx];
            }

            // Fade out in the last 30% of duration
            float t = _elapsed / _duration;
            if (t > 0.7f)
            {
                float alpha = 1f - (t - 0.7f) / 0.3f;
                var c = _sr.color;
                c.a = alpha;
                _sr.color = c;
            }

            if (t >= 1f)
                _playing = false;
        }
    }
}
