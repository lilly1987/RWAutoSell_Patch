using HarmonyLib;
using RimWorld;
using RWASFilterLib;
using RWAutoSell;
using RWAutoSell.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Lilly
{
    [StaticConstructorOnStartup]
    public static class RWAutoSell_Patch
    {
        public static string harmonyId = "Lilly.RWAutoSell";
        public static MyHarmony harmony;
        public static bool onDebug = true;
        public static bool ASAITog = true;

        static RWAutoSell_Patch() 
        {
            if (harmony != null)
            {
                MyLog.Warning($"Patch Already",color: "00FF00FF");
                return;
            }

            MyLog.Warning($"<color=#00FF00FF>Patch ST</color>");
            harmony = new MyHarmony(harmonyId);

            var patchType = typeof(RWAutoSell_Patch);
            //MyHarmonyPatch("DeinitAndRemoveMapPatch", typeof(Game), "DeinitAndRemoveMap", prefix: "DeinitAndRemoveMapPatch");
            harmony.Patch("LoadGameFromSaveFileNow", typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow", patchType, postfix: "LoadGameFromSaveFileNowPatch");
            harmony.Patch("MapComponentTick", typeof(ASMapComp), "MapComponentTick", patchType, transpiler: "Transpiler1");
            harmony.Patch("MapGenerated", typeof(ASMapComp), "MapGenerated", patchType, postfix: "MapGeneratedPostfix", transpiler: "Transpiler1");
            harmony.Patch("MapRemoved", typeof(ASMapComp), "MapRemoved", patchType, prefix: "MapRemovedPrefix", transpiler: "Transpiler1");
            harmony.Patch("GetCount", typeof(FilterLowStack), "GetCount", patchType, transpiler: "Transpiler3", parameters: new Type[] { typeof(ThingDef), typeof(Map), typeof(bool) });
            harmony.Patch("MapTradables", typeof(ASLibTransferUtility), "MapTradables", patchType, transpiler: "MapTradablesTranspiler", parameters: new Type[] {
                typeof(Map),
                typeof(bool),
                typeof(TransferAsOneMode),
                typeof(Func<Pawn, bool>),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            });

            harmony.PatchAll();

            MyLog.Warning($"<color=#00FF00FF>Patch ED</color>");
            
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref onDebug, "onDebug", false);
            Scribe_Values.Look(ref ASAITog, "ASAITog", true);
        }

        public static void DoSettingsWindowContents(Rect inRect, Listing_Standard listing)
        {
            listing.GapLine();
            listing.CheckboxLabeled($"Debug", ref onDebug);
            listing.CheckboxLabeled($"ASAITog", ref ASAITog);
        }

        public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            MyLog.Warning($"<color=#00FF00FF>IsPlayerHome ST</color> {original.Name}", print: onDebug);
            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();

            for (int i = 0; i < codes.Count; i++)
            {
                //MyLog.Warning($"IsPlayerHome {codes[i].opcode} / {codes[i].operand}", print: onDebug);
                // 찾기: map.IsPlayerHome 조건 분기
                if (i+3 < codes.Count &&
                    codes[i].opcode == OpCodes.Ldarg_0 &&                                                
                    codes[i+2].opcode == OpCodes.Callvirt &&
                    codes[i+2].operand is MethodInfo method &&
                    method.Name == "get_IsPlayerHome" &&
                    (codes[i + 3].opcode == OpCodes.Brfalse || 
                    codes[i + 3].opcode == OpCodes.Brfalse_S)
                    )
                {                
                    i= i + 3; // IsPlayerHome 호출 이후로 건너뜀

                    MyLog.Warning($"IsPlayerHome {original.Name} <color=#00FF00FF>succ</color>", print: onDebug);
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Warning($"<color=#00FF00FF>IsPlayerHome ED</color> {original.Name}", print: onDebug);
            return newCodes;
        }
        
        public static IEnumerable<CodeInstruction> MapTradablesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MyLog.Warning($"<color=#00FF00FF>IsPlayerHome2 ST</color>", print: onDebug);

            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();

            for (int i = 0; i < codes.Count; i++)
            {
                //MyLog.Warning($"IsPlayerHome2 {codes[i].opcode} / {codes[i].operand}", print: onDebug);
                // 찾기: map.IsPlayerHome 조건 분기
                if (i+4 < codes.Count &&
                    codes[i].opcode == OpCodes.Ldarg_0 &&                                                
                    codes[i+1].opcode == OpCodes.Callvirt &&
                    codes[i+1].operand is MethodInfo method &&
                    method.Name == "get_IsPlayerHome" &&
                    (codes[i + 2].opcode == OpCodes.Brtrue_S) &&
                    codes[i + 4].opcode == OpCodes.Ret
                    )
                {
                    //foreach (var label in codes[i].labels)
                    //    codes[i + 5].labels.Add(label);
                    codes[i + 5].labels.AddRange(codes[i].labels);

                    i = i + 4; // IsPlayerHome 호출 이후로 건너뜀

                    MyLog.Warning($"IsPlayerHome2 <color=#00FF00FF>succ</color>", print: onDebug,color: "00FF00FF");
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Warning($"<color=#00FF00FF>IsPlayerHome2 ED</color>", print: onDebug);
            return newCodes;
        }               

        public static IEnumerable<CodeInstruction> Transpiler3(IEnumerable<CodeInstruction> instructions)
        {
            MyLog.Warning($"IsPlayerHome3 ST", print: onDebug);
            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();

            for (int i = 0; i < codes.Count; i++)
            {
                // 찾기: map.IsPlayerHome 조건 분기
                if (i+4 < codes.Count &&
                    codes[i].opcode == OpCodes.Ldarg_2 &&                                                
                    codes[i+1].opcode == OpCodes.Callvirt &&
                    codes[i+1].operand is MethodInfo method &&
                    method.Name == "get_IsPlayerHome" &&
                    (codes[i + 2].opcode == OpCodes.Brfalse)
                    )
                {                
                    i= i + 2; // IsPlayerHome 호출 이후로 건너뜀

                    MyLog.Warning($"IsPlayerHome3 <color=#00FF00FF>succ</color>", print: onDebug);
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Warning($"IsPlayerHome3 ED", print: onDebug);
            return newCodes;
        }
        
        public static List<IRule> ruleList = new List<IRule>();

        public static Map tmpmap;

        public static void LoadGameFromSaveFileNowPatch()
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

        public static void MapGeneratedPostfix(ASMapComp __instance)
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
        
        public static void MapRemovedPrefix(ASMapComp __instance)
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

        public static void DeinitAndRemoveMapPatch(Map map)
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
