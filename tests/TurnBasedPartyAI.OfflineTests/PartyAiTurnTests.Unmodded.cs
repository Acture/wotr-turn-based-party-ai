using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using TurnBased.Controllers;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    /// <summary>
    /// What a battle shows of its course: every turn as it starts, every command a unit gets and acts on, and at the end
    /// each unit's hit points, state and position and the game time. Units are named by their scenario ids, so the
    /// traces of two builds of a scenario compare.
    /// </summary>
    internal sealed class BattleTrace : ITurnBasedModeHandler, IUnitRunCommandHandler, IUnitCommandActHandler
    {
        private readonly OfflineBattle battle;
        public readonly List<string> Lines = new();

        public BattleTrace(OfflineBattle battle) => this.battle = battle;

        private string Name(UnitEntityData unit)
            => unit == null ? "-" : battle.Units.FirstOrDefault(u => u.Unit == unit)?.Id ?? unit.Blueprint.name;

        private string Describe(UnitCommand command)
        {
            var ability = command is UnitUseAbility use ? use.Ability?.Blueprint?.name : null;
            var target = command.Target == null ? "-" : command.Target.Unit != null ? Name(command.Target.Unit) : command.Target.Point.ToString("F2");
            return $"{Name(command.Executor)} {command.GetType().Name} {ability} ai={command.AiAction?.Blueprint?.name} -> {target}";
        }

        public void HandleSurpriseRoundStarted() => Lines.Add("surprise round");
        public void HandleRoundStarted(int round) => Lines.Add($"round {round}");
        public void HandleTurnStarted(UnitEntityData unit) => Lines.Add($"turn {Name(unit)} controllable={unit.IsDirectlyControllable}");
        public void HandleUnitControlChanged(UnitEntityData unit) => Lines.Add($"control {Name(unit)}");
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck perceptionCheck) => Lines.Add($"not surprised {Name(unit)}");
        public void HandleUnitRunCommand(UnitCommand command) => Lines.Add("run " + Describe(command));
        public void HandleUnitCommandDidAct(UnitCommand command) => Lines.Add("act " + Describe(command));

        public void Finish()
        {
            foreach (var entry in battle.Units)
            {
                var unit = entry.Unit;
                Lines.Add($"end {entry.Id} hp={unit.HPLeft} conscious={unit.Descriptor.State.IsConscious} dead={unit.Descriptor.State.IsDead} at {unit.Position.ToString("F2")}");
            }
            Lines.Add("time " + Game.Instance.Player.GameTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
        }
    }

    public sealed partial class PartyAiTurnTests
    {
        /// <summary>
        /// The skirmish with every AI switch off: the player ends each party turn as soon as it waits for them, the
        /// demons' AI fights until a side has won.
        /// </summary>
        private static List<string> SkirmishWithEverySwitchOff()
        {
            using var battle = OfflineBattle.Start(Skirmish(), 1);
            var party = Party(battle);
            Assert.All(party, u => Assert.False(u.IsAIEnabled));
            var trace = new BattleTrace(battle);
            battle.Subscribe(trace);
            Assert.Null(RunUntilTurn(battle, _ => false, 30000, party));
            Assert.True(Decided(battle), "No side won.");
            trace.Finish();
            return trace.Lines;
        }

        private static bool PatchedBy(string owner)
            => Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m)?.Owners.Contains(owner) == true);

        [Fact]
        public void WithEverySwitchOffABattleRunsExactlyAsWithoutTheMod()
        {
            game.RequireGame();
            var modId = game.Session.ModId;
            Assert.True(PatchedBy(modId));

            var withMod = SkirmishWithEverySwitchOff();
            // The same build again first: the comparison below means something only if a battle repeats exactly.
            Assert.Equal(withMod, SkirmishWithEverySwitchOff());

            List<string> withoutMod;
            var harmony = new Harmony(modId);
            harmony.UnpatchAll(modId);
            try
            {
                Assert.False(PatchedBy(modId));
                withoutMod = SkirmishWithEverySwitchOff();
            }
            finally
            {
                // As the mod's entry method patches when Unity Mod Manager loads it.
                harmony.PatchAll(game.Session.ModAssembly);
            }
            Assert.True(PatchedBy(modId));

            Assert.Contains(withMod, l => l.StartsWith("turn p", StringComparison.Ordinal));
            Assert.Contains(withMod, l => l.StartsWith("act e", StringComparison.Ordinal));
            Assert.Equal(withoutMod, withMod);
        }
    }
}
