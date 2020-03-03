namespace Prima.Scheduler.Resources
{
    public static class RunDisplayTypes
    {
        public enum RunDisplayType
        {
            FR,
            OP,
            OZ,
            RC,
            EX,
        }

        public static Color None = new Color
        {
            RGB = new int[3],
            Hex = "#FFFFFF",
        };
        public static Color FR = new Color
        {
            RGB = new int[3] { 159, 197, 232 },
            Hex = "#9fc5e8",
        };
        public static Color OP = new Color
        {
            RGB = new int[3] { 255, 229, 153 },
            Hex = "#ffe599",
        };
        public static Color OZ = new Color
        {
            RGB = new int[3] { 249, 203, 156 },
            Hex = "#f9cb9c",
        };
        public static Color RC = new Color
        {
            RGB = new int[3] { 234, 153, 153 },
            Hex = "#ea9999",
        };
        public static Color EX = new Color
        {
            RGB = new int[3] { 204, 102, 255 },
            Hex = "#cc66ff",
        };

        public static Color GetColor(RunDisplayType type)
        {
            return type switch
            {
                RunDisplayType.FR => FR,
                RunDisplayType.OP => OP,
                RunDisplayType.OZ => OZ,
                RunDisplayType.RC => RC,
                RunDisplayType.EX => EX,
                _ => None,
            };
        }
    }

    public struct Color
    {
        public int[] RGB;
        public string Hex;
    }
}
