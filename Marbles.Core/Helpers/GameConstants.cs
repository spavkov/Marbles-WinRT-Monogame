using Microsoft.Xna.Framework;

namespace Marbles.Core.Helpers
{
    public static class GameConstants
    {
        public static int MarbleRadius;

        static GameConstants()
        {
            MarbleRadius = 50;
            MarbleCellSize = 82;
            ScoreoidApiKey = "a6995444344632ccee065c5462c3b4ef625362c9";
            ScoreoidGameId = "162ebf7038";
        }

        public static int MarbleCellSize;

        public static int ColumnCount = 8;
        public static int RowsCount = 7;

        public static string ScoreoidApiKey;

        public static string ScoreoidGameId;

        public static int MarbleTextureWidth = 60;
        public static int MarbleTextureHeight = 60;
        public static string RollingBackgroundTileName = @"Backgrounds\zutiTile";

        public static string VirtualKeyboardFont = @"SpriteFonts\BasicLight";
        public static string MenuFont = @"SpriteFonts\WhiteRabbitDropshadow";
        public static string GuiFontLarge = @"SpriteFonts\WhiteRabbitDropshadow";

        public static string ScoreChangeVisualizationsFont = @"SpriteFonts\BebasSpriteFont";
        public static string HighScoresListFont = @"SpriteFonts\BasicLight";

        public static string FeedbackEmail = "MarblesGameFeedback@roboblob.com";
        public static string ExceptionsEmail = "MarblesGameExceptions@roboblob.com";
        public static int MaxUsernameLenght = 11;
        public static Color GuiTextColor = Color.FromNonPremultiplied(252, 109, 38, 255);
    }
}