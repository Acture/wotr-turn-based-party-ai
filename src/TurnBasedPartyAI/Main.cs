using HarmonyLib;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            new Harmony(modEntry.Info.Id).PatchAll(typeof(Main).Assembly);
            return true;
        }
    }
}
