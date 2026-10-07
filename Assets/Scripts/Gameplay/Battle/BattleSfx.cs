using PixelDefense.Services.Audio;

namespace PixelDefense.Gameplay
{
    /// <summary>Resolved audio handles for battle sounds (ids match the generated audio library).</summary>
    public sealed class BattleSfx
    {
        public readonly int Tap;
        public readonly int Jump;
        public readonly int Land;
        public readonly int Shoot;
        public readonly int Pop;
        public readonly int Crunch;
        public readonly int Empty;
        public readonly int Poof;
        public readonly int Deny;
        public readonly int Reveal;
        public readonly int Roar;
        public readonly int Hurt;
        public readonly int Warning;
        public readonly int Heartbeat;
        public readonly int Chime;
        public readonly int Explosion;
        public readonly int Win;
        public readonly int Lose;
        public readonly int Freeze;
        public readonly int Bomb;
        public readonly int SlotAdd;
        public readonly int Whoosh;

        public BattleSfx(AudioService audio)
        {
            Tap = audio.Resolve("tap");
            Jump = audio.Resolve("jump");
            Land = audio.Resolve("land");
            Shoot = audio.Resolve("shoot");
            Pop = audio.Resolve("pop");
            Crunch = audio.Resolve("crunch");
            Empty = audio.Resolve("empty");
            Poof = audio.Resolve("poof");
            Deny = audio.Resolve("deny");
            Reveal = audio.Resolve("reveal");
            Roar = audio.Resolve("roar");
            Hurt = audio.Resolve("hurt");
            Warning = audio.Resolve("warning");
            Heartbeat = audio.Resolve("heartbeat");
            Chime = audio.Resolve("chime");
            Explosion = audio.Resolve("explosion");
            Win = audio.Resolve("win");
            Lose = audio.Resolve("lose");
            Freeze = audio.Resolve("freeze");
            Bomb = audio.Resolve("bomb");
            SlotAdd = audio.Resolve("slot");
            Whoosh = audio.Resolve("whoosh");
        }
    }
}
