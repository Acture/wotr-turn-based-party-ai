using System;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    public static class Main
    {
        /// <summary>The single characters' modes (<see cref="TurnRules"/>), in the mod's folder; not in saves.</summary>
        private const string OverridesFileName = "CharacterModes.txt";

        /// <summary>The kinds' modes (<see cref="TurnRules"/>), in the mod's folder.</summary>
        private const string KindModesFileName = "KindModes.txt";

        internal static UnityModManager.ModEntry Entry;
        internal static UnityModManager.ModEntry.ModLogger Logger;

        /// <summary>Whether the kinds' modes came from the mod's file when it loaded (see <see cref="ModMenuSettings"/>).</summary>
        internal static bool KindModesSaved;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Entry = modEntry;
            Logger = modEntry.Logger;
            LoadSettings();
            var harmony = new Harmony(modEntry.Info.Id);
            try
            {
                harmony.PatchAll(typeof(Main).Assembly);
                modEntry.OnGUI = UmmSettings.OnGUI;
                return true;
            }
            catch (Exception e)
            {
                // PatchAll stops at the first patch that does not apply and keeps the ones before it. A turn whose start
                // is patched but whose end is not would keep a party member out of the player's control, so the mod
                // removes all of its patches and Unity Mod Manager reports it as not loaded.
                harmony.UnpatchAll(harmony.Id);
                modEntry.Logger.LogException(e);
                return false;
            }
        }

        private static void LoadSettings()
        {
            try
            {
                TurnRules.OverridesFile = Path.Combine(Entry.Path, OverridesFileName);
                var skipped = TurnRules.LoadOverrides(TurnRules.OverridesFile);
                if (skipped > 0) Logger.Warning($"{OverridesFileName}: skipped {skipped} lines that could not be read.");
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
            try
            {
                TurnRules.KindModesFile = Path.Combine(Entry.Path, KindModesFileName);
                KindModesSaved = File.Exists(TurnRules.KindModesFile);
                var skipped = TurnRules.LoadKindModes(TurnRules.KindModesFile);
                if (skipped > 0) Logger.Warning($"{KindModesFileName}: skipped {skipped} lines that could not be read.");
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }

        internal static void SaveOverrides()
        {
            if (TurnRules.OverridesFile == null) return;
            try
            {
                TurnRules.SaveOverrides(TurnRules.OverridesFile);
            }
            catch (Exception e)
            {
                Logger?.LogException(e);
            }
        }

        internal static void SaveKindModes()
        {
            if (TurnRules.KindModesFile == null) return;
            try
            {
                TurnRules.SaveKindModes(TurnRules.KindModesFile);
                KindModesSaved = true;
            }
            catch (Exception e)
            {
                Logger?.LogException(e);
            }
        }

        /// <summary>Shows a kind's mode chosen in the Unity Mod Manager panel on the Mod Menu page, if there is one.</summary>
        internal static void ShowOnModMenuPage(UnitKind kind, TurnMode mode)
        {
            if (!ModMenuLoaded) return;
            try
            {
                ShowOnModMenuPageWithModMenu(kind, mode);
            }
            catch (Exception e)
            {
                Logger?.LogException(e);
            }
        }

        // Kept apart, so that Mod Menu's types are loaded only when Mod Menu is there.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowOnModMenuPageWithModMenu(UnitKind kind, TurnMode mode) => ModMenuSettings.Show(kind, mode);

        /// <summary>Whether Mod Menu has loaded. The mod works without it; its settings are in Unity Mod Manager's window too.</summary>
        internal static bool ModMenuLoaded => Loaded("ModMenu");

        /// <summary>
        /// Whether ToyBox has loaded (its Unity Mod Manager id is "0ToyBox0"). The settings page offers a mode for summons
        /// only then (<see cref="TurnRules.KindsInSettings"/>). Every mod has loaded before the page is added.
        /// </summary>
        internal static bool ToyBoxLoaded => Loaded("0ToyBox0");

        private static bool Loaded(string id) => UnityModManager.FindMod(id) is { } mod && mod.Started && !mod.ErrorOnLoading;
    }

    /// <summary>
    /// Adds the mod's page to Mod Menu once the game's settings and texts exist, as Mod Menu expects. A failure leaves the
    /// mod working with its defaults.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    internal static class ModMenuRegistration
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!Main.ModMenuLoaded) return;
            try
            {
                Register();
            }
            catch (Exception e)
            {
                Main.Logger?.Error("Mod Menu settings could not be added; the settings are in Unity Mod Manager's window.");
                Main.Logger?.LogException(e);
            }
        }

        // Kept out of Postfix, so that Mod Menu's types are loaded only when Mod Menu is there.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Register() => ModMenuSettings.Register(Main.Entry);
    }
}
