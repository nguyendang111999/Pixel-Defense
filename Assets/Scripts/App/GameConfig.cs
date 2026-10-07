using PixelDefense.Core;
using PixelDefense.Gameplay;
using PixelDefense.Services.Audio;
using UnityEngine;

namespace PixelDefense.App
{
    /// <summary>Root configuration: rules tunables, level pack, visuals, audio and the meta economy.</summary>
    [CreateAssetMenu(menuName = "Pixel Defense/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        public const int TargetFrameRate = 60;

        [Header("Content")]
        public TextAsset LevelPack;
        public VisualConfig Visuals;
        public AudioLibrary Audio;

        [Header("Rules")]
        public BattleSettings Battle = new BattleSettings();

        [Header("Economy")]
        public int StartCoins = 150;
        public int StartFreeze = 2;
        public int StartBomb = 2;
        public int StartSlot = 1;
        public int StartPick = 2;
        public int FreezePrice = 100;
        public int BombPrice = 150;
        public int SlotPrice = 200;
        public int PickPrice = 120;
        public int BaseReward = 20;
        public int RewardPerStar = 10;

        [Header("Progression")]
        [Tooltip("After the last level, replay this many final levels in a loop.")]
        public int LoopLastLevels = 10;

        [Header("Testing (Editor and development builds)")]
        [Tooltip("Game speeds the autoplay bot's speed button cycles through.")]
        public float[] BotSpeeds = { 1f, 2f, 4f };

        [Tooltip("Seconds the result popup stays up before the bot moves on.")]
        public float BotResultPause = 1.5f;

        [Tooltip("Losses in a row on one level before the bot skips ahead.")]
        public int BotRetries = 2;
    }
}
