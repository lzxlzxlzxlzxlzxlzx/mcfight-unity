using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 通用 VFX 精灵动画播放器。
    /// 从 Resources/VFX/ 加载命名 spritesheet，按帧播放并自动销毁。
    /// </summary>
    public class VFXSpriteView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Sprite[] _frames;
        private float _duration;
        private float _elapsed;
        private int _frameIdx;

        /// <summary>
        /// 在指定位置播放 VFX 精灵动画。
        /// </summary>
        /// <param name="sheetName">Resources/VFX/ 下的 spritesheet 名（不含扩展名）</param>
        /// <param name="x">世界坐标 X</param>
        /// <param name="y">世界坐标 Y</param>
        /// <param name="worldSize">精灵在世界中的目标尺寸（正方形边长，世界单位）</param>
        /// <param name="duration">总持续时间（秒）</param>
        /// <param name="rotation">Z 轴旋转角度（度）</param>
        /// <param name="useAsLength">true: worldSize 作为长度（水平），false: 作为整体大小</param>
        public static void Play(string sheetName, float x, float y, float worldSize, float duration, float rotation = 0f, bool useAsLength = false)
        {
            var frames = Resources.LoadAll<Sprite>($"VFX/{sheetName}_spritesheet");
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[VFXSpriteView] No sprites found: VFX/{sheetName}_spritesheet");
                return;
            }

            var go = new GameObject($"VFX_{sheetName}");
            go.transform.position = new Vector3(x, y, 0);
            go.transform.rotation = Quaternion.Euler(0, 0, rotation);

            var sv = go.AddComponent<VFXSpriteView>();
            sv._sr = go.AddComponent<SpriteRenderer>();
            sv._sr.sprite = frames[0];
            sv._sr.sortingOrder = 115;

            // Calculate scale: sprite is fw x fh at 100 ppu
            float fw = frames[0].rect.width;
            float fh = frames[0].rect.height;
            float spriteWorldW = fw / 100f;
            float spriteWorldH = fh / 100f;

            if (useAsLength)
            {
                // worldSize = horizontal length, height proportional
                float scaleX = worldSize / spriteWorldW;
                float scaleY = scaleX * (spriteWorldH / spriteWorldW);
                go.transform.localScale = new Vector3(scaleX, scaleY, 1);
            }
            else
            {
                // worldSize = overall size, fit to larger dimension
                float scale = Mathf.Max(worldSize / spriteWorldW, worldSize / spriteWorldH);
                go.transform.localScale = new Vector3(scale, scale, 1);
            }

            sv._frames = frames;
            sv._duration = duration;
            sv._elapsed = 0;
            sv._frameIdx = 0;

            Destroy(go, duration + 0.1f);
        }

        void Update()
        {
            if (_frames == null || _frames.Length == 0) return;
            _elapsed += Time.deltaTime;

            float frameDuration = _duration / _frames.Length;
            int newFrame = Mathf.Min((int)(_elapsed / frameDuration), _frames.Length - 1);
            if (newFrame != _frameIdx)
            {
                _frameIdx = newFrame;
                _sr.sprite = _frames[_frameIdx];
            }

            // Fade out in last 25%
            float t = _elapsed / _duration;
            if (t > 0.75f)
            {
                float alpha = 1f - (t - 0.75f) / 0.25f;
                var c = _sr.color;
                c.a = alpha;
                _sr.color = c;
            }
        }
    }
}
