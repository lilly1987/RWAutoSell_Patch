using HarmonyLib;
using RWASFilterLib;
using RWAutoSell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Lilly
{
    [StaticConstructorOnStartup]
    public static class RWAutoSell_Patch
    {
        public static string harmonyId = "Lilly.RWAutoSell";
        public static Harmony harmony;
        public static bool onDebug = false;
        public static bool ASAITog = true;

        static RWAutoSell_Patch() 
        {
            if (harmony == null)
            {
                MyLog.Warning($"Patch ST");
                try
                {
                    harmony = new Harmony(harmonyId);
                    harmony.PatchAll();
                    MyLog.Warning($"{harmonyId} Patch Succ");
                }
                catch (System.Exception e)
                {
                    MyLog.Error($"{harmonyId} Patch Fail");
                    MyLog.Error(e.ToString());
                    MyLog.Error($"{harmonyId} Patch Fail");
                }
                MyLog.Warning($"Patch ED");
            }
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref onDebug, "onDebug", false);
            Scribe_Values.Look(ref ASAITog, "ASAITog", true);
        }

        public static void DoSettingsWindowContents(Rect inRect, Listing_Standard listing)
        {
            listing.CheckboxLabeled($"Debug", ref onDebug);
            listing.CheckboxLabeled($"ASAITog", ref ASAITog);
        }

        public static List<IRule> ruleList = new List<IRule>();

        public static Map tmpmap;

        [HarmonyPatch(typeof(SavedGameLoaderNow), nameof(SavedGameLoaderNow.LoadGameFromSaveFileNow))]
        public static class Patch_LoadGame
        {
            [HarmonyPostfix]
            public static void Patch()
            {
                MyLog.Warning($"LoadGameFromSaveFileNow ST", print: onDebug);

                var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
                if (aSMapComp == null)
                {
                    MyLog.Error($"LoadGameFromSaveFileNow ASMapComp NULL");
                    return;
                }
                ruleList.Clear();
                foreach (var rule in aSMapComp.EnumerateRules())
                {
                    ruleList.Add(rule.DeepCopy());
                }
                MyLog.Warning($"LoadGameFromSaveFileNow ED {aSMapComp.Rules.Count}", print: onDebug);
            }
        }

        [HarmonyPatch(typeof(ASMapComp), nameof(ASMapComp.MapGenerated))]
        public static class Patch_MapGenerated
        {

            [HarmonyPostfix]
            public static void Postfix(ASMapComp __instance)
            {
                MyLog.Warning($"MapGenerated ST", print: onDebug);
                if (!__instance.map.IsPlayerHome)
                {
                    ASMod.GetSingleton.mapComps.Add(__instance);
                }
                try
                {
                    MyLog.Warning($"map {__instance?.map}", print: onDebug);
                    MyLog.Warning($"Find.Maps.Count {Find.Maps?.Count} ", print: onDebug);

                    var aSMapComp = __instance;
                    if (aSMapComp == null)
                    {
                        MyLog.Error($"AddMap ASMapComp NULL");
                        return;
                    }
                    aSMapComp.ASAITog=true;
                    foreach (Pawn pawn in __instance.map.mapPawns.FreeColonists)
                    {
                        aSMapComp.SelectedNegogiators.Add(pawn.ThingID);
                    }
                    if (Find.Maps.Count == 1)
                    {
                        tmpmap = __instance.map;
                        foreach (var rule in ruleList)
                        {
                            aSMapComp.Add(rule.DeepCopy());
                        }
                    }
                    else if (Find.Maps.Count > 1)
                    {
                        foreach (var rule in ASMapComp.GetSingleton(Find.Maps[Find.Maps.Count - 2]).EnumerateRules())
                        {
                            aSMapComp.Add(rule.DeepCopy());
                        }
                    }
                    MyLog.Warning($"aSMapComp.Rules.Count {aSMapComp.Rules.Count}", print: onDebug);
                }
                catch (Exception e)
                {
                    MyLog.Error(e.ToString());
                }
                MyLog.Warning($"MapGenerated ED", print: onDebug);
            }
        }

        [HarmonyPatch(typeof(ASMapComp), nameof(ASMapComp.MapRemoved))]
        public static class MapRemoved
        {
            [HarmonyPrefix]
            public static void Prefix(ASMapComp __instance)
            {
                if (!__instance.map.IsPlayerHome)
                {
                    ASMod.GetSingleton.mapComps.Remove(__instance);
                }
                try
                {
                    MyLog.Warning($"MapRemoved ST", print: onDebug);
                    if (Find.Maps?.Count == 0)
                    {
                        ruleList.Clear();
                        foreach (var rule in __instance.EnumerateRules())
                        {
                            ruleList.Add(rule.DeepCopy());
                        }
                    }
                    MyLog.Warning($"__instance.Rules.Count {__instance?.Rules?.Count}", print: onDebug);
                }
                catch (Exception e)
                {
                    MyLog.Error(e.ToString());
                }
                MyLog.Warning($"MapRemoved ED", print: onDebug);
            }
        }

        //[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]// 정상이지만 중복이라 제거
        public static class Patch_RemoveMap
        {
            [HarmonyPrefix]
            //[HarmonyPostfix]
            public static void Patch(Map map)
            {
                MyLog.Warning($"DeinitAndRemoveMap ST", print: onDebug);
                try
                {
                    MyLog.Warning($"map {map}", print: onDebug);

                    MyLog.Warning($"Find.Maps.Count {Find.Maps?.Count}", print: onDebug);

                    if (Find.Maps?.Count > 0)
                    {
                        var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
                        if (aSMapComp == null)
                        {
                            MyLog.Error($"DeinitAndRemoveMap ASMapComp NULL");
                            return;
                        }
                        MyLog.Warning($"aSMapComp.Rules.Count  {aSMapComp.Rules.Count}", print: onDebug);
                        ruleList.Clear();
                        foreach (var rule in aSMapComp.EnumerateRules())
                        {
                            ruleList.Add(rule.DeepCopy());
                        }
                    }
                }
                catch (Exception e)
                {
                    MyLog.Error(e.ToString());
                }
                MyLog.Warning($"DeinitAndRemoveMap ED", print: onDebug);
            }
        }

    }
}
