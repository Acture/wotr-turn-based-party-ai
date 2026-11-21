using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Settings;
using Kingmaker.UnitLogic.Commands.Base;
using TurnBased.Controllers;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    [CollectionDefinition("battles")]
    public sealed class BattleCollection : ICollectionFixture<BattleGame> { }

    public sealed class BattleGame : WotrGameFixture { }

    /// <summary>Vanilla 2.7.0 blueprints (character names as the game shows them).</summary>
    internal static class Guids
    {
        // Pirate Madman, Dumb Monster Brain.
        public const string PirateMadman = "cc6a64ab9dad47d0868bd59d283ca7df";
        // Besmara's Initiate, CultistCleric_Brain_Standard.
        public const string BesmarasInitiate = "f7290e2af397445cb9edd82c1fbe5126";
        public const string Brimorak = "61e89e329440c0e46a4125d4667720ee";
        public const string FerociousSchir = "b6b4b773aa4f93045903b67b92167930";
        public const string MobsFaction = "0f539babafb47fe4586b719d02aff7c4";
        public const string KenabresBurning = "92180b58582ec5f43a756071cd339f52";
    }

    /// <summary>Turns as they start (with whether the player controls the unit then) and the commands units run.</summary>
    internal sealed class TurnLog : ITurnBasedModeHandler, IUnitRunCommandHandler, IUnitCommandActHandler
    {
        public readonly List<(UnitEntityData Unit, bool Controllable)> Turns = new();
        public readonly List<UnitCommand> Given = new();
        public readonly List<UnitCommand> Acted = new();
        public Action<UnitEntityData> OnTurn;

        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }

        public void HandleTurnStarted(UnitEntityData unit)
        {
            Turns.Add((unit, unit.IsDirectlyControllable));
            OnTurn?.Invoke(unit);
        }

        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck perceptionCheck) { }
        public void HandleUnitRunCommand(UnitCommand command) => Given.Add(command);
        public void HandleUnitCommandDidAct(UnitCommand command) => Acted.Add(command);
    }

    [Collection("battles")]
    public sealed partial class PartyAiTurnTests
    {
        private readonly BattleGame game;

        public PartyAiTurnTests(BattleGame game) => this.game = game;

        /// <summary>
        /// A turn-based skirmish of the game's own units: the main character p0 and the companion p1 against two demons.
        /// The party members named in <paramref name="aiOn"/> have their AI button switched on, as the player switches it
        /// (<c>BattleUnitSpec.WithAI</c>: the turn-based setting, since the battle is turn-based).
        /// </summary>
        private static BattleScenario Skirmish(bool limitedAI = false, params string[] aiOn)
        {
            var scenario = new BattleScenario("party-ai-skirmish")
            {
                MainCharacter = "p0",
                Area = Guids.KenabresBurning,
                StartTime = new TimeSpan(12, 0, 30),
                TurnBased = true,
                Difficulty = GameDifficultyOption.Normal,
                LimitedAI = limitedAI,
            };
            scenario.Unit("p0", "party", Guids.PirateMadman).At(47.2f, 40, -65.2f);
            scenario.Unit("p1", "party", Guids.BesmarasInitiate).At(46.5f, 40, -67f);
            scenario.Companions.Add("p1");
            scenario.Unit("e0", "enemies", Guids.Brimorak).WithFaction(Guids.MobsFaction).At(52f, 40, -68f);
            scenario.Unit("e1", "enemies", Guids.FerociousSchir).WithFaction(Guids.MobsFaction).At(52f, 40, -70f);
            foreach (var id in aiOn) scenario.Units.First(u => u.Id == id).WithAI(true);
            return scenario;
        }

        private static UnitEntityData[] Party(OfflineBattle battle) => new[] { battle.Unit("p0"), battle.Unit("p1") };

        private static UnitEntityData[] Enemies(OfflineBattle battle) => new[] { battle.Unit("e0"), battle.Unit("e1") };

        private static bool Decided(OfflineBattle battle)
            => !Party(battle).Any(u => u.Descriptor.State.IsConscious) || !Enemies(battle).Any(u => u.Descriptor.State.IsConscious);

        private static void FightUntilDecided(OfflineBattle battle, TimeSpan limit)
        {
            for (var elapsed = TimeSpan.Zero; elapsed < limit && !Decided(battle); elapsed += TimeSpan.FromSeconds(1))
                battle.Run(TimeSpan.FromSeconds(1));
            Assert.True(Decided(battle), $"No side won within {limit}.");
            // Let the game end combat and the turn that was running.
            battle.Run(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void TheGamesAiPlaysTheTurnsOfPartyMembersWhoseTurnBasedAiSwitchIsOn()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(false, "p0", "p1"), 1);
            var party = Party(battle);
            Assert.All(party, u => Assert.True(u.IsAIEnabled));
            // The switch alone does not take control from the player.
            Assert.All(party, u => Assert.True(u.IsDirectlyControllable));
            var log = new TurnLog();
            battle.Subscribe(log);

            FightUntilDecided(battle, TimeSpan.FromMinutes(5));

            var partyTurns = log.Turns.Where(t => party.Contains(t.Unit)).ToList();
            Assert.NotEmpty(partyTurns);
            // During its own turn the game counts the unit as one the player does not control, so it plays the turn.
            Assert.All(partyTurns, t => Assert.False(t.Controllable));
            Assert.Contains(log.Given, c => party.Contains(c.Executor) && c.AiAction != null);
            Assert.Contains(log.Acted, c => party.Contains(c.Executor));
            // Control returns when the turn ends.
            Assert.False(CombatController.IsInTurnBasedCombat());
            Assert.All(party, u => Assert.False((bool)u.PreventDirectControl));
        }

        [Fact]
        public void APartyMemberWhoseSwitchIsOffWaitsForThePlayerAsInTheGame()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(), 1);
            var party = Party(battle);
            // Off is the game's default for turn-based mode.
            Assert.All(party, u => Assert.False(u.IsAIEnabled));
            var log = new TurnLog();
            battle.Subscribe(log);

            battle.Run(TimeSpan.FromSeconds(30));

            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            Assert.NotNull(turn);
            Assert.Contains(turn.Rider, party);
            Assert.True(turn.Rider.IsDirectlyControllable);
            Assert.Equal(TurnController.TurnStatus.Preparing, turn.Status);
            Assert.DoesNotContain(log.Acted, c => party.Contains(c.Executor));
        }

        [Fact]
        public void TheSwitchIsPerCharacter()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(false, "p1"), 1);
            UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
            Assert.False(player.IsAIEnabled);
            Assert.True(companion.IsAIEnabled);
            var log = new TurnLog();
            battle.Subscribe(log);

            // The player ends each of p0's turns, as with the end-turn key, until both have had a turn.
            var waited = 0;
            for (var seconds = 0; seconds < 300 && !Decided(battle) && (waited == 0 || !log.Turns.Any(t => t.Unit == companion)); seconds++)
            {
                battle.Run(TimeSpan.FromSeconds(1));
                var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                if (turn?.Rider == player && turn.Status == TurnController.TurnStatus.Preparing)
                {
                    Assert.True(player.IsDirectlyControllable);
                    Assert.DoesNotContain(log.Acted, c => c.Executor == player);
                    waited++;
                    Assert.True(turn.CanEndTurnAndNoActing());
                    turn.ForceToEnd();
                }
            }

            Assert.True(waited > 0, "p0 never had a turn.");
            var companionTurns = log.Turns.Where(t => t.Unit == companion).ToList();
            Assert.NotEmpty(companionTurns);
            Assert.All(companionTurns, t => Assert.False(t.Controllable));
            Assert.All(log.Turns.Where(t => t.Unit == player), t => Assert.True(t.Controllable));
            Assert.Contains(log.Given, c => c.Executor == companion && c.AiAction != null);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheLimitedAiOptionLimitsThePartyAiAsInRealTime(bool limitedAI)
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(limitedAI, "p0", "p1"), 1);
            var party = Party(battle);
            var log = new TurnLog();
            // At the start of each party turn, for every action of the brain: whether the game's AI may choose it while
            // the mod holds the turn, and whether it may for the same unit under the player's direct control (the
            // party AI in real time), checked as AiBrainController checks it (DecisionContext.InitAction, then the
            // blueprint's IsActorValid).
            var checks = new List<(string Unit, string Action, bool UseOnLimitedAI, bool Held, bool Controlled)>();
            log.OnTurn = unit =>
            {
                if (!party.Contains(unit)) return;
                Assert.False(unit.IsDirectlyControllable);
                var context = new DecisionContext();
                context.InitUnit(unit);
                foreach (var action in unit.Brain.AvailableActions)
                {
                    if (action.Blueprint is not BlueprintAiAction blueprint) continue;
                    context.InitAction(action);
                    var held = blueprint.IsActorValid(context);
                    unit.PreventDirectControl.Release();
                    try
                    {
                        Assert.True(unit.IsDirectlyControllable);
                        checks.Add((unit.CharacterName, blueprint.name, blueprint.UseOnLimitedAI, held, blueprint.IsActorValid(context)));
                    }
                    finally
                    {
                        unit.PreventDirectControl.Retain();
                    }
                }
            };
            battle.Subscribe(log);

            FightUntilDecided(battle, TimeSpan.FromMinutes(5));

            Assert.NotEmpty(checks);
            Assert.All(checks, c => Assert.True(c.Held == c.Controlled, $"{c.Unit}: {c.Action} held {c.Held}, controlled {c.Controlled}."));
            if (limitedAI)
            {
                Assert.All(checks.Where(c => !c.UseOnLimitedAI), c => Assert.False(c.Held, $"{c.Unit} may use {c.Action} under Limited AI."));
                Assert.All(log.Given.Where(c => party.Contains(c.Executor) && c.AiAction?.Blueprint is BlueprintAiAction),
                    c => Assert.True(((BlueprintAiAction)c.AiAction.Blueprint).UseOnLimitedAI));
            }
            else
            {
                // Without the option the party AI uses actions that Limited AI excludes, so the check above is not vacuous.
                Assert.Contains(checks, c => !c.UseOnLimitedAI && c.Held);
            }
        }
    }
}
