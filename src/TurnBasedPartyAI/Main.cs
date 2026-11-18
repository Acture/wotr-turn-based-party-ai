using System;
using HarmonyLib;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            try
            {
                harmony.PatchAll(typeof(Main).Assembly);
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
    }
}
