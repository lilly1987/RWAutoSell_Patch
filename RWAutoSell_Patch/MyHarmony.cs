using HarmonyLib;
using Lilly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lilly
{
    public class MyHarmony : Harmony
    {
        public MyHarmony(string harmonyId) : base(harmonyId)
        {
        }

        public void Patch(
            string label,
            Type original_type,
            string original_name,
            Type patch_type,
            string prefix = null,
            string postfix = null,
            string transpiler = null,
            string finalizer = null,
            Type[] parameters = null,
            Type[] generics = null)
        {
            var original = AccessTools.Method(original_type, original_name, parameters, generics);
            if (original == null)
            {
                MyLog.Error($"{Id} {label} Patch Fail : {original_type}.{original_name} not found");
                return;
            }

            HarmonyMethod prefix1 = NewHarmonyMethod(label, patch_type, prefix, "prefix");
            HarmonyMethod postfix1 = NewHarmonyMethod(label, patch_type, postfix, "postfix");
            HarmonyMethod transpiler1 = NewHarmonyMethod(label, patch_type, transpiler, "transpiler");
            HarmonyMethod finalizer1 = NewHarmonyMethod(label, patch_type, finalizer, "finalizer");
            if (prefix1 == null && postfix1 == null && transpiler1 == null && finalizer1 == null)
            {
                MyLog.Error($"{Id} {label} Patch Fail : {patch_type} not found");
                return;
            }
            try
            {
                Patch(
                    original,
                    prefix: prefix1,
                    postfix: postfix1,
                    transpiler: transpiler1,
                    finalizer: finalizer1
                );
                MyLog.Warning($"{Id}/{label}/Patch/<color=#00FF00FF>Succ</color>");
            }
            catch (System.Exception e)
            {
                MyLog.Error($"{Id} {label} Patch Fail");
                MyLog.Error(e.ToString());
                MyLog.Error($"{Id} {label} Patch Fail");
            }


        }

        private HarmonyMethod NewHarmonyMethod(string label, Type patch_type, string name,string kind)
        {
            if (patch_type == null) return null;
            if (name == null) return null;
            HarmonyMethod m =null;

            try
            {
                m = AccessTools.Method(patch_type, name);
            }
            catch (Exception e)
            {
                MyLog.Error($"{Id}/{label}/{patch_type}/{name}/{kind}/Patch Fail");
                MyLog.Error(e.ToString());
                MyLog.Error($"{Id}/{label}/{patch_type}/{name}/{kind}/Patch Fail");
            }
            return m;
        }

        public new void PatchAll()
        {
            try
            {
                base.PatchAll();
                MyLog.Warning($"{Id} Patch Succ");
            }
            catch (System.Exception e)
            {
                MyLog.Error($"{Id} Patch Fail");
                MyLog.Error(e.ToString());
                MyLog.Error($"{Id} Patch Fail");
            }
        }

        public void TyrAction(string label, Action action)
        {
            try
            {
                action();
                MyLog.Warning($"{Id} {label} action Succ");
            }
            catch (System.Exception e)
            {
                MyLog.Error($"{Id} {label} action Fail");
                MyLog.Error(e.ToString());
                MyLog.Error($"{Id} {label} action Fail");
            }
        }
    }
}
