using UnityEngine;

namespace MCFight
{
    /// <summary> 单位渲染视图：从 UnitState 同步到 GameObject </summary>
    public class UnitView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private SpriteRenderer _hpBarRenderer;
        private float _maxHp;
        private int _team;
        private bool _dying;
        private float _deathTimer;
        private const float DEATH_DURATION = 1f;

        // 缓存的 HP 条纹理
        private static Texture2D _hpBarBgTex;
        private static Texture2D _hpBarFillTex;
        private static Sprite _hpBarBgSprite;
        private static Sprite _hpBarFillSprite;

        public SpriteRenderer hpBarRenderer
        {
            get => _hpBarRenderer;
            set => _hpBarRenderer = value;
        }

        public void Initialize(UnitState unit, MonsterDefSO def)
        {
            _sr = GetComponent<SpriteRenderer>();
            _maxHp = unit.MaxHp;
            _team = unit.Team;
            _dying = false;
            _deathTimer = 0;

            // 设置 HP 条
            if (_hpBarRenderer != null)
            {
                InitHpBarTextures();
                _hpBarRenderer.sprite = _hpBarFillSprite;
                _hpBarRenderer.drawMode = SpriteDrawMode.Simple;
            }
        }

        public void SyncFromState(ref UnitState state)
        {
            if (_dying) return;

            transform.position = new Vector3(state.X, state.Y, 0);

            if (_sr != null)
            {
                _sr.flipX = state.Facing < 0;
            }

            // HP 条
            if (_hpBarRenderer != null && _maxHp > 0)
            {
                float ratio = Mathf.Clamp01(state.Hp / _maxHp);
                float barWidth = 36f;
                float barHeight = 5f;
                float unitScale = transform.localScale.x;
                // HP 条用世界坐标尺寸，不受单位 scale 影响
                _hpBarRenderer.transform.localPosition = new Vector3(0, (state.Radius + 8) / unitScale, 0);
                _hpBarRenderer.transform.localScale = new Vector3(ratio * barWidth / unitScale, barHeight / unitScale, 1);
                _hpBarRenderer.color = _team == 0 ? Color.cyan : Color.red;
            }
        }

        public void PlayDeath()
        {
            if (_dying) return;
            _dying = true;
            _deathTimer = DEATH_DURATION;

            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 0.5f;
                _sr.color = c;
            }
            if (_hpBarRenderer != null)
                _hpBarRenderer.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_dying)
            {
                _deathTimer -= Time.deltaTime;
                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = Mathf.Lerp(0.5f, 0f, 1f - _deathTimer / DEATH_DURATION);
                    _sr.color = c;
                    // 下沉效果
                    var p = transform.position;
                    p.y -= 10f * Time.deltaTime;
                    transform.position = p;
                }
                if (_deathTimer <= 0)
                    Destroy(gameObject);
            }
        }

        static void InitHpBarTextures()
        {
            if (_hpBarFillTex == null)
            {
                _hpBarFillTex = new Texture2D(1, 1);
                _hpBarFillTex.SetPixel(0, 0, Color.white);
                _hpBarFillTex.Apply();
                _hpBarFillSprite = Sprite.Create(_hpBarFillTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            }
        }
    }
}
