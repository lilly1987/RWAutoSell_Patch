using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lilly
{
    public class RWAutoSellLillyModUI : Mod
    {
        public static RWAutoSellLillySettings settings;

        public RWAutoSellLillyModUI(ModContentPack content) : base(content)
        {
            settings = GetSettings<RWAutoSellLillySettings>();// 주의. MainSettings의 patch가 먼저 실행됨      
        }

        Vector2 scrollPosition;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            var rect = new Rect(0, 0, inRect.width - 16, 1000);
            
            Widgets.BeginScrollView(inRect, ref scrollPosition, rect);

            Listing_Standard listing = new Listing_Standard();

            listing.Begin(rect);

            RWAutoSellLillyPatch.DoSettingsWindowContents(inRect, listing);

            listing.End();
            
            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "RWAutoSell_Patch".Translate();
        }
    }
}
