using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Settings;
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
    /// done. The flag is released when the turn ends or is dropped, or when the player switches the unit's AI off
    /// during the turn.
    /// </summary>
    internal static class PartyAiTurns
    {
        private sealed class ReferenceComparer : IEqualityComparer<UnitEntityData>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(UnitEntityData x, UnitEntityData y) => ReferenceEquals(x, y);
            public int GetHashCode(UnitEntityData obj) => RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>The units whose flag the mod holds, with the turn it holds it for.</summary>
        private static readonly Dictionary<UnitEntityData, TurnController> HeldBy = new Dictionary<UnitEntityData, TurnController>(ReferenceComparer.Instance);

        /// <summary>Whether the mod hands this unit's current turn to the game's AI.</summary>
        internal static bool IsHeld(UnitEntityData unit) => unit != null && HeldBy.ContainsKey(unit);

        internal static void Begin(TurnController turn)
        {
            // The AI button is the turn-based switch only while the player's turn-based setting is on. A fight in
            // which the game forces turn-based combat with that setting off (EtudeBracketForceCombatMode) shows and
            // writes the real-time switch, which is on by default; those turns stay the player's, as in the game.
            if (!(bool)SettingsRoot.Game.TurnBased.EnableTurnBasedMode) return;
            Hold(turn, turn.Rider);
            Hold(turn, turn.Mount);
        }

        private static void Hold(TurnController turn, UnitEntityData unit)
        {
            // IsAIEnabled reads the turn-based switch while the turn-based setting is on, and includes the game's
            // BlueprintRoot.CompanionsAI.
            if (unit == null || HeldBy.ContainsKey(unit) || !unit.IsInCombat || !unit.IsDirectlyControllable || !unit.IsAIEnabled) return;
            unit.PreventDirectControl.Retain();
            HeldBy.Add(unit, turn);
        }

        internal static void End(TurnController turn)
        {
            if (HeldBy.Count == 0) return;
            foreach (var unit in HeldBy.Where(e => ReferenceEquals(e.Value, turn)).Select(e => e.Key).ToList())
                Release(unit);
        }

        /// <summary>The player switched the unit's AI off during its own AI-played turn: the rest of the turn is the player's.</summary>
        internal static void HandBack(UnitEntityData unit)
        {
            if (IsHeld(unit)) Release(unit);
        }

        private static void Release(UnitEntityData unit)
        {
            HeldBy.Remove(unit);
            unit.PreventDirectControl.Release();
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

    /// <summary>
    /// A turn that is replaced or dropped without ending: combat ends, the player leaves turn-based mode, or the area is
    /// unloaded (loading a save, leaving to the main menu), which ends combat and stops the turn-based controller.
    /// </summary>
    [HarmonyPatch(typeof(TurnController), nameof(TurnController.Dispose))]
    internal static class TurnDisposePatch
    {
        [HarmonyPostfix]
        private static void Postfix(TurnController __instance) => PartyAiTurns.End(__instance);
    }

    /// <summary>
    /// The AI button switched off during the unit's own AI-played turn. Both of the game's AI buttons (the action bar
    /// and the party portraits) write <c>UnitEntityData.IsAIEnabled</c> and then show what it reads, which is always on
    /// for a unit the player does not control. Releasing the flag here gives the rest of the turn to the player at once,
    /// and the button shows the switch as off. The game's AI does not choose actions in turn-based combat for a unit
    /// the player controls; an action it has already started finishes.
    /// </summary>
    [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.IsAIEnabled), MethodType.Setter)]
    internal static class AiSwitchPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnitEntityData __instance, bool value)
        {
            if (!value) PartyAiTurns.HandBack(__instance);
        }
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
