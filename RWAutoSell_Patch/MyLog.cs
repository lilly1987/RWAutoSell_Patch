using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lilly
{
    public static class MyLog
    {
        //public static void Warning(string text) => Log.Warning($"Lilly - {text}");
        //public static void Error(string text) => Log.Error($"Lilly - {text}");
        //public static void Message(string text) => Log.Message($"Lilly - {text}");

        public static string MyText(string text, string memberName, int lineNumber, string className)
        {
            return $"<color=#0000FFFF>Lilly</color> <color=#00FF00FF>{className}</color>.<color=#FF8000FF>{memberName}</color> <color=#00FFFFFF>{lineNumber}</color> - {text}";
        }


        public static void Warning(string text,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            bool print=true
            )
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Warning(MyText(text, memberName, lineNumber, className));
        }


        public static void Error(string text,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            bool print = true)
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Error(MyText(text, memberName, lineNumber, className));
        }

        public static void Message(string text,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            bool print = true)
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Message(MyText(text, memberName, lineNumber, className));
        }

    }
}
