using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;

namespace TurnBasedPartyAI
{
    /// <summary>
    /// Party members whose turn the game's AI plays in turn-based combat.
    ///
    /// The game keeps a separate AI switch for turn-based mode (the AI button on the action bar and the party portraits
    /// writes it while turn-based mode is on; <c>UnitEntityData.IsAIEnabled</c> reads it), but in 2.7.0 nothing acts
    /// on it: the turn of a unit the player controls waits for the player. When such a turn starts and the unit's
    /// switch is on, this mod retains the game's own <c>UnitEntityData.PreventDirectControl</c> for that turn. The game
    /// then plays the turn as it plays the turn of a party member the player does not control (the game itself uses
    /// this flag in story scenes, for fleeing units and during resurrection): its <c>TurnController</c> lets
    /// <c>AiBrainController</c> choose the unit's actions with the unit's own brain and ends the turn when the AI is
    /// done. The flag is released when the turn ends.
    /// </summary>
    internal static class PartyAiTurns
    {
        private sealed class ReferenceComparer : IEqualityComparer<UnitEntityData>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(UnitEntityData x, UnitEntityData y) => ReferenceEquals(x, y);
            public int GetHashCode(UnitEntityData obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private static readonly Dictionary<TurnController, List<UnitEntityData>> HeldByTurn = new Dictionary<TurnController, List<UnitEntityData>>();
        private static readonly HashSet<UnitEntityData> Held = new HashSet<UnitEntityData>(ReferenceComparer.Instance);

        /// <summary>Whether the mod hands this unit's current turn to the game's AI.</summary>
        internal static bool IsHeld(UnitEntityData unit) => unit != null && Held.Contains(unit);

        internal static void Begin(TurnController turn)
        {
            Hold(turn, turn.Rider);
            Hold(turn, turn.Mount);
        }

        private static void Hold(TurnController turn, UnitEntityData unit)
        {
            // IsAIEnabled reads the turn-based switch while turn-based mode is on, and includes the game's
            // BlueprintRoot.CompanionsAI.
            if (unit == null || Held.Contains(unit) || !unit.IsInCombat || !unit.IsDirectlyControllable || !unit.IsAIEnabled) return;
            unit.PreventDirectControl.Retain();
            Held.Add(unit);
            if (!HeldByTurn.TryGetValue(turn, out var units)) HeldByTurn[turn] = units = new List<UnitEntityData>();
            units.Add(unit);
        }

        internal static void End(TurnController turn)
        {
            if (!HeldByTurn.TryGetValue(turn, out var units)) return;
            HeldByTurn.Remove(turn);
            foreach (var unit in units)
            {
                unit.PreventDirectControl.Release();
                Held.Remove(unit);
            }
        }
    }

    [HarmonyPatch(typeof(TurnController), nameof(TurnController.Start))]
    internal static class TurnStartPatch
    {
        [HarmonyPrefix]
        private static void Prefix(TurnController __instance) => PartyAiTurns.Begin(__instance);
    }

    [HarmonyPatch(typeof(TurnController), "End")]
    internal static class TurnEndPatch
    {
        [HarmonyPostfix]
        private static void Postfix(TurnController __instance) => PartyAiTurns.End(__instance);
    }

    /// <summary>A turn that is replaced or dropped without ending, for example when combat ends.</summary>
    [HarmonyPatch(typeof(TurnController), nameof(TurnController.Dispose))]
    internal static class TurnDisposePatch
    {
        [HarmonyPostfix]
        private static void Postfix(TurnController __instance) => PartyAiTurns.End(__instance);
    }

    /// <summary>
    /// The game's Limited AI option restricts the AI of party members the player controls to the actions marked
    /// <c>UseOnLimitedAI</c> (<c>BlueprintAiAction.IsActorValid</c> checks <c>IsDirectlyControllable</c>). While the mod
    /// holds a party member's turn the game counts it as not controlled, so the same restriction is applied here and
    /// the party AI chooses in turn-based combat as it does in real time.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintAiAction), nameof(BlueprintAiAction.IsActorValid))]
    internal static class LimitedAiPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BlueprintAiAction __instance, DecisionContext context, ref bool __result)
        {
            if (__result && AiBrainController.LimitedAI && !__instance.UseOnLimitedAI
                && context.Unit != null && context.Unit.IsPlayerFaction && PartyAiTurns.IsHeld(context.Unit))
                __result = false;
        }
    }
}
