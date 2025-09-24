using Verse;

namespace Lilly
{
    public class RWAutoSell_Settings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            RWAutoSell_Patch.ExposeData();
        }
    }
}