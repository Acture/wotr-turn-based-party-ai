using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

namespace TurnBasedPartyAI
{
    /// <summary>The mod's texts in English and Simplified Chinese, put into the game's current localization pack.</summary>
    internal static class Strings
    {
        private static readonly List<(LocalizedString String, string English, string Chinese)> All = new List<(LocalizedString, string, string)>();

        private static bool Chinese => LocalizationManager.CurrentPack?.Locale == Locale.zhCN;

        internal static string Text(string english, string chinese) => Chinese ? chinese : english;

        internal static LocalizedString Create(string key, string english, string chinese)
        {
            var text = new LocalizedString { Key = key };
            All.Add((text, english, chinese));
            Register(text, english, chinese);
            return text;
        }

        private static void Register(LocalizedString text, string english, string chinese)
            => LocalizationManager.CurrentPack?.PutString(text.Key, Text(english, chinese));

        /// <summary>A language change loads a new pack without the mod's texts.</summary>
        internal static void RegisterAll()
        {
            foreach (var (text, english, chinese) in All) Register(text, english, chinese);
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged")]
    internal static class LocaleChangedPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => Strings.RegisterAll();
    }
}
