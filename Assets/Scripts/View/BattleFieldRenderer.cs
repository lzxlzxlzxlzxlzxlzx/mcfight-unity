using UnityEngine;

namespace MCFight
{
    /// <summary> 战场背景渲染器：使用竞技场背景图 </summary>
    public class BattleFieldRenderer : MonoBehaviour
    {
        private static Sprite _bgSprite;

        void Start()
        {
            CreateBackground();
        }

        void CreateBackground()
        {
            // Load background sprite
            if (_bgSprite == null)
                _bgSprite = Resources.Load<Sprite>("Sprites/UI/battlefield_bg");
            
            if (_bgSprite == null)
            {
                // Fallback: try direct path
                _bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/battlefield_bg.jpg");
            }

            if (_bgSprite != null)
            {
                var bgGo = new GameObject("ArenaBackground");
                bgGo.transform.position = new Vector3(640f, 360f, 0);
                var bgSr = bgGo.AddComponent<SpriteRenderer>();
                bgSr.sprite = _bgSprite;
                bgSr.sortingOrder = -100;
                // Sprite is 1536x1024 at 100 pixelsPerUnit = 15.36x10.24 world units
                // Need to cover 1280x720 world units
                float spriteWorldW = _bgSprite.rect.width / _bgSprite.pixelsPerUnit;
                float spriteWorldH = _bgSprite.rect.height / _bgSprite.pixelsPerUnit;
                float scaleX = 1280f / spriteWorldW;
                float scaleY = 720f / spriteWorldH;
                bgGo.transform.localScale = new Vector3(scaleX, scaleY, 1);
            }
            else
            {
                Debug.LogWarning("[BattleFieldRenderer] Background sprite not found, using solid color");
                CreateFallbackBackground();
            }
        }

        void CreateFallbackBackground()
        {
            var tex = new Texture2D(4, 4);
            tex.filterMode = FilterMode.Point;
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, new Color(0.08f, 0.08f, 0.12f, 1f));
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);

            var bgGo = new GameObject("FieldBackground");
            bgGo.transform.position = new Vector3(640f, 360f, 0);
            bgGo.transform.localScale = new Vector3(6000f, 4000f, 1f);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            bgSr.sortingOrder = -100;
        }
    }
}
