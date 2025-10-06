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

namespace Lilly.RWAutoSellPatch
{
    // MainTabWindow_AutoSell


    [StaticConstructorOnStartup]
    public static class ModPatch
    {
        public static string harmonyId = "Lilly.RWAutoSell";
        public static MyHarmony harmony;
        public static bool onDebug = true;
        public static bool ASAITog = true;

        static ModPatch() 
        {
            if (harmony != null)
            {
                MyLog.Warning($"Patch Already",color: "00FF00FF");
                return;
            }

            MyLog.Message($"<color=#00FF00FF>Patch ST</color>");
            harmony = new MyHarmony(harmonyId);

            var patchType = typeof(ModPatch);
            //MyHarmonyPatch("DeinitAndRemoveMapPatch", typeof(Game), "DeinitAndRemoveMap", prefix: "DeinitAndRemoveMapPatch");
            harmony.Patch("LoadGameFromSaveFileNow", typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow", patchType, postfix: nameof(LoadGameFromSaveFileNowPatch));
            harmony.Patch("MapComponentTick", typeof(ASMapComp), "MapComponentTick", patchType, transpiler: nameof(UnCheckIsPlayerHome));
            // 지도 제한 제거
            harmony.Patch("MainTabWindow_AutoSell", typeof(MainTabWindow_AutoSell), "InitDia", patchType, transpiler: nameof(InitDiaTranspiler));
            harmony.Patch("MapGenerated", typeof(ASMapComp), "MapGenerated", patchType, postfix: nameof(MapGeneratedPostfix), transpiler: nameof(UnCheckIsPlayerHome));
            harmony.Patch("MapRemoved", typeof(ASMapComp), "MapRemoved", patchType, prefix: nameof(MapRemovedPrefix), transpiler: nameof(UnCheckIsPlayerHome));
            harmony.Patch("GetCount", typeof(FilterLowStack), "GetCount", patchType, transpiler: nameof(UnCheckIsPlayerHome2), parameters: new Type[] { typeof(ThingDef), typeof(Map), typeof(bool) });
            harmony.Patch("SpawnSetup", typeof(Pawn), "SpawnSetup", patchType, postfix: nameof(PawnSpawnSetup));
/*            harmony.Patch("MapTradables", typeof(ASLibTransferUtility), "MapTradables", patchType, transpiler: "MapTradablesTranspiler", parameters: new Type[] {
                typeof(Map),
                typeof(bool),
                typeof(TransferAsOneMode),
                typeof(Func<Pawn, bool>),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            });*/

            harmony.PatchAll();

            MyLog.Message($"<color=#00FF00FF>Patch ED</color>");
            
        }

        public static void ExposeData()
        {
            MyLog.ST();
            MyLog.Message($"<color=#00FF00FF>{Scribe.mode}</color>");
            Scribe_Values.Look(ref onDebug, "onDebug", false);
            Scribe_Values.Look(ref ASAITog, "ASAITog", true); 
            Scribe_Collections.Look(ref ruleListCpoy, "Rules", LookMode.Deep, new List<ASRule>());
            if (ruleListCpoy == null)
            {
                MyLog.Warning("ruleListCpoy null");
                ruleListCpoy = new List<ASRule>();
            }
            else if(Scribe.mode==LoadSaveMode.PostLoadInit)
            {
                MyLog.Message($"ruleListCpoy {ruleListCpoy.Count}");
            }
            MyLog.ED();
        }

        public static void DoSettingsWindowContents(Rect inRect, Listing_Standard listing)
        {
            listing.GapLine();
            listing.CheckboxLabeled($"Debug", ref onDebug);
            listing.CheckboxLabeled($"ASAITog", ref ASAITog);
            if(listing.ButtonText("목록 복사"))
            {
                if (Find.Maps?.Count > 0)
                {
                    var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
                    if (aSMapComp == null)
                    {
                        MyLog.Warning($"DeinitAndRemoveMap ASMapComp NULL");
                        return;
                    }
                    MyLog.Message($"copy aSMapComp.Rules.Count {aSMapComp.Rules.Count}", print: onDebug);
                    ruleListCpoy.Clear();
                    foreach (ASRule rule in aSMapComp.Rules)
                    {
                        ruleListCpoy.Add((ASRule)rule.DeepCopy());
                    }
                }
            }
            if (listing.ButtonText("목록 저장"))
            {
                if (Find.Maps?.Count > 0)
                {
                    var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
                    if (aSMapComp == null)
                    {
                        MyLog.Warning($"DeinitAndRemoveMap ASMapComp NULL");
                        return;
                    }
                    MyLog.Message($"paste aSMapComp.Rules.Count {aSMapComp.Rules.Count}", print: onDebug);
                    
                    foreach (ASRule rule in ruleListCpoy)
                    {
                        aSMapComp.Add(rule.DeepCopy());
                    }
                }
            }
        }

        public static List<ASRule> ruleListCpoy = new List<ASRule>();

        // 지도 제한 제거
        public static IEnumerable<CodeInstruction> InitDiaTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MyLog.Message($"InitDiaTranspiler ST", print: onDebug, "00FF00FF");

            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();

            for (int i = 0; i < codes.Count; i++)
            {
                // Faction 조건 제거 블록
                if (i + 9 < codes.Count &&
                    codes[i].opcode == OpCodes.Ldloc_S &&
                    codes[i + 1].opcode == OpCodes.Callvirt &&
                    codes[i + 1].operand is MethodInfo factionGetter1 &&
                    factionGetter1.Name == "get_Faction" &&
                    codes[i + 2].opcode == OpCodes.Brfalse_S &&
                    codes[i + 3].opcode == OpCodes.Ldloc_S &&
                    codes[i + 4].opcode == OpCodes.Callvirt &&
                    codes[i + 4].operand is MethodInfo factionGetter2 &&
                    factionGetter2.Name == "get_Faction" &&
                    codes[i + 5].opcode == OpCodes.Callvirt &&
                    codes[i + 5].operand is MethodInfo isPlayerGetter &&
                    isPlayerGetter.Name == "get_IsPlayer" &&
                    codes[i + 6].opcode == OpCodes.Brtrue_S &&
                    codes[i + 7].opcode == OpCodes.Ldloc_S &&
                    codes[i + 8].opcode == OpCodes.Callvirt &&
                    codes[i + 8].operand is MethodInfo factionGetter3 &&
                    factionGetter3.Name == "get_Faction" &&
                    codes[i + 9].opcode == OpCodes.Brtrue_S)
                {
                    // 레이블 보존
                    foreach (var label in codes[i].labels)
                        codes[i + 10].labels.Add(label);

                    MyLog.Message($"InitDiaTranspiler Succ", print: onDebug, "00FF00FF");

                    i += 9; // 조건 블록 전체 건너뜀
                    continue;
                }

                newCodes.Add(codes[i]);
            }


            MyLog.Message($"InitDiaTranspiler ED", print: onDebug, "00FF00FF");

            return newCodes;
        }

        // IsPlayerHome 조건 제거
        public static IEnumerable<CodeInstruction> UnCheckIsPlayerHome(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            MyLog.Message($"<color=#00FF00FF>IsPlayerHome ST</color> {original.Name}", print: onDebug);
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

                    MyLog.Message($"IsPlayerHome {original.Name} <color=#00FF00FF>succ</color>", print: onDebug);
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Message($"<color=#00FF00FF>IsPlayerHome ED</color> {original.Name}", print: onDebug);
            return newCodes;
        }

        // IsPlayerHome 조건 제거
        public static IEnumerable<CodeInstruction> MapTradablesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MyLog.Message($"<color=#00FF00FF>IsPlayerHome2 ST</color>", print: onDebug);

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

                    MyLog.Message($"IsPlayerHome2 <color=#00FF00FF>succ</color>", print: onDebug,color: "00FF00FF");
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Message($"<color=#00FF00FF>IsPlayerHome2 ED</color>", print: onDebug);
            return newCodes;
        }

        // IsPlayerHome 조건 제거
        public static IEnumerable<CodeInstruction> UnCheckIsPlayerHome2(IEnumerable<CodeInstruction> instructions)
        {
            MyLog.Message($"IsPlayerHome3 ST", print: onDebug);
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

                    MyLog.Message($"IsPlayerHome3 <color=#00FF00FF>succ</color>", print: onDebug);
                    continue; // IsPlayerHome 호출 제거
                }
                newCodes.Add(codes[i]);
            }

            MyLog.Message($"IsPlayerHome3 ED", print: onDebug);
            return newCodes;
        }
        
        public static List<IRule> ruleList = new List<IRule>();

        public static Map tmpmap;

        // 저장 불러오기 후 규칙 복원
        public static void LoadGameFromSaveFileNowPatch()
        {
            MyLog.Message($"LoadGameFromSaveFileNow ST", print: onDebug);

            var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
            if (aSMapComp == null)
            {
                MyLog.Warning($"LoadGameFromSaveFileNow ASMapComp NULL");
                return;
            }
            ruleList.Clear();
            foreach (var rule in aSMapComp.EnumerateRules())
            {
                ruleList.Add(rule.DeepCopy());
            }
            MyLog.Message($"LoadGameFromSaveFileNow ED {aSMapComp.Rules.Count}", print: onDebug);
        }

        // 새 지도 생성 후 규칙 복원
        public static void MapGeneratedPostfix(ASMapComp __instance)
        {
            MyLog.Message($"MapGenerated ST", print: onDebug);
            if (!__instance.map.IsPlayerHome)
            {
                ASMod.GetSingleton.mapComps.Add(__instance);
            }
            try
            {
                //MyLog.Warning($"map {__instance?.map}", print: onDebug);
                //MyLog.Warning($"Find.Maps.Count {Find.Maps?.Count} ", print: onDebug);

                var aSMapComp = __instance;
                if (aSMapComp == null)
                {
                    MyLog.Warning($"AddMap ASMapComp NULL");
                    return;
                }
                // 자동 협상자 설정
                aSMapComp.ASAITog=true;

                // 이시점엔 아직 폰 생성 안됨
                //MyLog.Message($"map.mapPawns.FreeColonists.Count {__instance.map.mapPawns.FreeColonists.Count}", print: onDebug);
                //foreach (Pawn pawn in __instance.map.mapPawns.FreeColonists)
                //{
                //    aSMapComp.SelectedNegogiators.Add(pawn.ThingID);
                //}
                //MyLog.Message($"SelectedNegogiators.Count {aSMapComp.SelectedNegogiators.Count}", print: onDebug);

                // 규칙 복원
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
                MyLog.Message($"aSMapComp.Rules.Count {aSMapComp.Rules.Count}", print: onDebug);
            }
            catch (Exception e)
            {
                MyLog.Error(e.ToString());
            }
            //try
            //{
            //    // 규칙 복원

            //    MyLog.Message($"aSMapComp.Rules.Count", print: onDebug);
            //}
            //catch (Exception e)
            //{
            //    MyLog.Error(e.ToString());
            //}
            MyLog.Message($"MapGenerated ED", print: onDebug);
        }
        
        public static void MapRemovedPrefix(ASMapComp __instance)
        {
            MyLog.Message($"MapRemoved ST", print: onDebug);
            if (!__instance.map.IsPlayerHome)
            {
                ASMod.GetSingleton.mapComps.Remove(__instance);
            }
            try
            {
                if (Find.Maps?.Count == 0)
                {
                    ruleList.Clear();
                    foreach (var rule in __instance.EnumerateRules())
                    {
                        ruleList.Add(rule.DeepCopy());
                    }
                }
                MyLog.Message($"__instance.Rules.Count {__instance?.Rules?.Count}", print: onDebug);
            }
            catch (Exception e)
            {
                MyLog.Error(e.ToString());
            }
            MyLog.Message($"MapRemoved ED", print: onDebug);
        }       

        public static void DeinitAndRemoveMapPatch(Map map)
        {
            MyLog.Message($"DeinitAndRemoveMap ST", print: onDebug);
            try
            {
                //MyLog.Warning($"map {map}", print: onDebug);

                //MyLog.Warning($"Find.Maps.Count {Find.Maps?.Count}", print: onDebug);

                if (Find.Maps?.Count > 0)
                {
                    var aSMapComp = ASMapComp.GetSingleton(Find.CurrentMap);
                    if (aSMapComp == null)
                    {
                        MyLog.Warning($"DeinitAndRemoveMap ASMapComp NULL");
                        return;
                    }
                    MyLog.Message($"aSMapComp.Rules.Count  {aSMapComp.Rules.Count}", print: onDebug);
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
            MyLog.Message($"DeinitAndRemoveMap ED", print: onDebug);
        }

        public static void PawnSpawnSetup(Pawn __instance, Map map)
        {
            if (__instance.Faction != null && __instance.Faction == Faction.OfPlayer)
            {
                //MyLog.Message($"PawnSpawnSetup ST", print: onDebug);
                try
                {
                    var aSMapComp = ASMapComp.GetSingleton(map);
                    if (aSMapComp == null)
                    {
                        MyLog.Warning($"PawnSpawnSetup ASMapComp NULL {__instance}");
                        return;
                    }
                    aSMapComp.SelectedNegogiators.Add(__instance.ThingID);
                    MyLog.Message($"PawnSpawnSetup Succ {__instance}");
                }
                catch (Exception e)
                {
                    MyLog.Error($"PawnSpawnSetup Exception {__instance}");
                    MyLog.Error(e.ToString());
                }
                //MyLog.Message($"PawnSpawnSetup ED", print: onDebug);
            }
        }

    }
}
