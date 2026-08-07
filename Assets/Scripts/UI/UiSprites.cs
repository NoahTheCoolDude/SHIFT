using UnityEngine;

namespace Shift.UI
{
    /// <summary>
    /// Procedural sprites for the HUD. Generated rather than imported so Phase 0 UI adds no
    /// binary assets to the repo.
    /// </summary>
    internal static class UiSprites
    {
        private static Sprite _solid;

        /// <summary>A 1×1 white sprite, for bars and panels that only need a tint.</summary>
        public static Sprite Solid()
        {
            if (_solid != null) return _solid;

            Texture2D texture = NewTexture(1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _solid = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return _solid;
        }

        /// <summary>Antialiased filled circle.</summary>
        public static Sprite Circle(int size = 32)
        {
            Texture2D texture = NewTexture(size);
            Vector2 centre = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float radius = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), centre);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
