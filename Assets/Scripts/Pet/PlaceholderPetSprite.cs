using UnityEngine;

namespace HouchiPeto.Pet
{
    /// <summary>
    /// 本番のドット絵ができるまでの仮のペト。16×16 のドット絵をコードで作る。
    /// </summary>
    public static class PlaceholderPetSprite
    {
        // 上の行から順に書く。'.' は透明
        private static readonly string[] Body =
        {
            "................",
            "................",
            "..oo........oo..",
            ".obbo......obbo.",
            ".obbboooooobbbo.",
            ".obbbbbbbbbbbbo.",
            "obbbbbbbbbbbbbbo",
            "obbebbbbbbbbebbo",
            "obbebbbbbbbbebbo",
            "obbbbbbppbbbbbbo",
            "obbbbbbbbbbbbbbo",
            ".obbbbbbbbbbbbo.",
            "..oooooooooooo..",
        };

        private static readonly string[] LegsA =
        {
            "...oo.....oo....",
            "...oo.....oo....",
            "................",
        };

        private static readonly string[] LegsB =
        {
            "....oo.....oo...",
            "....oo.....oo...",
            "................",
        };

        private static Color ColorOf(char c)
        {
            switch (c)
            {
                case 'o': return new Color32(46, 36, 32, 255);    // 輪郭
                case 'e': return new Color32(46, 36, 32, 255);    // 目
                case 'b': return new Color32(245, 232, 210, 255); // 体
                case 'p': return new Color32(240, 140, 150, 255); // 鼻
                default: return new Color(0, 0, 0, 0);
            }
        }

        /// <summary>歩きの2コマ。[0] が足を開いた絵、[1] が足を閉じた絵</summary>
        public static Sprite[] CreateWalkFrames()
        {
            return new[] { Create(LegsA), Create(LegsB) };
        }

        private static Sprite Create(string[] legs)
        {
            const int size = 16;
            var rows = new string[Body.Length + legs.Length];
            Body.CopyTo(rows, 0);
            legs.CopyTo(rows, Body.Length);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            for (int row = 0; row < size; row++)
            {
                string line = row < rows.Length ? rows[row] : "";
                int y = size - 1 - row; // テクスチャは下が y=0
                for (int x = 0; x < size; x++)
                {
                    char c = x < line.Length ? line[x] : '.';
                    pixels[y * size + x] = ColorOf(c);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // 足元（下端の中央）を基準点にする。16px = 1 ユニット
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
        }
    }
}
