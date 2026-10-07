namespace PixelDefense.EditorTools
{
    /// <summary>Where generated assets live. Re-running the builders overwrites these in place (GUIDs kept).</summary>
    internal static class AssetPaths
    {
        public const string Art = "Assets/Art";
        public const string Textures = "Assets/Art/Textures";
        public const string Materials = "Assets/Art/Materials";
        public const string Audio = "Assets/Audio";
        public const string Sfx = "Assets/Audio/Sfx";
        public const string Music = "Assets/Audio/Music";
        public const string Config = "Assets/Config";
        public const string Ui = "Assets/UI";
        public const string UiFonts = "Assets/UI/Fonts";
        public const string UiTextures = "Assets/UI/Textures";
        public const string Levels = "Assets/Levels";
        public const string Scene = "Assets/Scenes/Game.unity";

        public const string LevelPack = Levels + "/main.txt";
        public const string VisualConfig = Config + "/VisualConfig.asset";
        public const string GameConfig = Config + "/GameConfig.asset";
        public const string AudioLibrary = Config + "/AudioLibrary.asset";
        public const string PanelSettings = Ui + "/PanelSettings.asset";
        public const string Theme = Ui + "/PixelDefenseTheme.tss";
        public const string Uxml = Ui + "/Game.uxml";
        public const string PostProfile = "Assets/Settings/PixelDefensePost.asset";
    }
}
