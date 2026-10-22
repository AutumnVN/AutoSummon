using TerrariaModder.Core.Config;

namespace AutoSummon
{
    public class AutoSummonConfig : ModConfig
    {
        public override int Version => 1;

        [Client, Label("Inventory column for Summoning Minions"), Description("Choose which inventory column to use for summoning minions (1-10)"), Range(1, 10)]
        public int MinionColumnIndex { get; set; } = 10;

        [Client, Label("Mute Summon Use Sounds"), Description("Enable this to mute the sound effects when summoning minions from the configured inventory column")]
        public bool MuteSummonUseSounds { get; set; }
    }
}