using Verse;

namespace Lilly.RWAutoSellPatch
{
    public class Settings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            ModPatch.ExposeData();
        }
    }
}