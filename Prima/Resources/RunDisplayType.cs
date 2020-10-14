namespace Prima.Resources
{
    public enum RunDisplayTypeBA
    {
        FR,
        OP,
        OZ,
        RC,
        EX,
    }

    public enum RunDisplayTypeCastrum
    {
        None,
        LL,
    }

    public static class RunDisplayTypes
    {
        private static readonly Color None = new Color
        {
            RGB = new int[3],
            Hex = "#FFFFFF",
        };
        private static readonly Color FR = new Color
        {
            RGB = new[] { 159, 197, 232 },
            Hex = "#9fc5e8",
        };
        private static readonly Color OP = new Color
        {
            RGB = new[] { 255, 229, 153 },
            Hex = "#ffe599",
        };
        private static readonly Color OZ = new Color
        {
            RGB = new[] { 249, 203, 156 },
            Hex = "#f9cb9c",
        };
        private static readonly Color RC = new Color
        {
            RGB = new[] { 234, 153, 153 },
            Hex = "#ea9999",
        };
        private static readonly Color EX = new Color
        {
            RGB = new[] { 204, 102, 255 },
            Hex = "#cc66ff",
        };

        public static Color GetColor(RunDisplayTypeBA type)
        {
            return type switch
            {
                RunDisplayTypeBA.FR => FR,
                RunDisplayTypeBA.OP => OP,
                RunDisplayTypeBA.OZ => OZ,
                RunDisplayTypeBA.RC => RC,
                RunDisplayTypeBA.EX => EX,
                _ => None,
            };
        }

        public static Color GetColorCastrum()
        {
            return new Color
            {
                RGB = new[] { 252, 133, 133 },
                Hex = "#fc8585",
            };
        }
    }

    public struct Color
    {
        public int[] RGB;
        public string Hex;
    }
}
