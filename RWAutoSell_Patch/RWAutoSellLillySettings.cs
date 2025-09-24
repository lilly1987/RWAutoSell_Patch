using Verse;

namespace Lilly
{
    public class RWAutoSellLillySettings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            RWAutoSellLillyPatch.ExposeData();
        }
    }
}