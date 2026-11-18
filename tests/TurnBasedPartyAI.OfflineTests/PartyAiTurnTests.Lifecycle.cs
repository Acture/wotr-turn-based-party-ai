using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.Settings;
using TurnBased.Controllers;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    /// <summary>What happens to an AI-played turn when the player or the game changes things while it runs.</summary>
    public sealed partial class PartyAiTurnTests
    {
        /// <summary>Runs exactly one game frame.</summary>
        private static readonly TimeSpan OneFrame = TimeSpan.FromMilliseconds(1);

        private static TurnController CurrentTurn => Game.Instance.TurnBasedCombatController.CurrentTurn;

        private static bool IsAiPlayed(UnitEntityData unit) => (bool)unit.PreventDirectControl && !unit.IsDirectlyControllable;

        /// <summary>
        /// Runs frames one at a time until a turn matches <paramref name="found"/> and returns it, or returns null when a
        /// side has won or <paramref name="maxFrames"/> have run. The player ends each turn of <paramref name="playerEnds"/>
        /// as soon as it waits for them, as with the end-turn key.
        /// </summary>
        private static TurnController RunUntilTurn(OfflineBattle battle, Func<TurnController, bool> found, params UnitEntityData[] playerEnds)
            => RunUntilTurn(battle, found, 20000, playerEnds);

        private static TurnController RunUntilTurn(OfflineBattle battle, Func<TurnController, bool> found, int maxFrames, params UnitEntityData[] playerEnds)
        {
            for (var frame = 0; frame < maxFrames && !Decided(battle); frame++)
            {
                battle.Run(OneFrame);
                var turn = CurrentTurn;
                if (turn == null) continue;
                if (found(turn)) return turn;
                if (playerEnds.Contains(turn.Rider) && turn.Status == TurnController.TurnStatus.Preparing && turn.CanEndTurnAndNoActing())
                    turn.ForceToEnd();
            }
            return null;
        }

        private static int AiCommandsGiven(TurnLog log, UnitEntityData unit) => log.Given.Count(c => c.Executor == unit && c.AiAction != null);

        /// <summary>Ends combat at once, as the game's End Combat cutscene command does (CommandEndCombat).</summary>
        private static void EndCombat()
        {
            foreach (var unit in Game.Instance.State.Units.Where(u => u.IsInCombat).ToList())
            {
                unit.LeaveCombat();
                unit.Group.Memory.Clear();
            }
            Game.Instance.Player.UpdateIsInCombat();
        }

        /// <summary>
        /// Runs the skirmish with the companion p1's AI on until its AI-played turn runs, then switches its AI off with the
        /// party portrait's AI button (PartyCharacterVM.SetAi), which writes the switch and then shows what
        /// IsAIEnabled reads. Checks what the player sees at once.
        /// </summary>
        private static TurnController SwitchTheCompanionsAiOffDuringItsTurn(OfflineBattle battle, TurnLog log)
        {
            UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
            battle.Subscribe(log);
            var turn = RunUntilTurn(battle, t => t.Rider == companion && IsAiPlayed(companion), player);
            Assert.NotNull(turn);
            Assert.True(companion.IsAIEnabled);

            companion.IsAIEnabled = false;

            // The button shows the switch as off, and the player controls the companion for the rest of this turn.
            Assert.False(companion.IsAIEnabled);
            Assert.True(companion.IsDirectlyControllable);
            Assert.False((bool)companion.PreventDirectControl);
            Assert.Same(turn, CurrentTurn);
            return turn;
        }

        [Fact]
        public void SwitchingTheAiOffDuringItsTurnHandsTheTurnBackAtOnce()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(false, "p1"), 1);
            SwitchTheCompanionsAiOffDuringItsTurn(battle, new TurnLog());
        }

        /// <summary>
        /// The frames after the switch include the end of an attack by a unit the player now controls. On .NET Framework
        /// a turn-based attack by a unit the player controls ends the test process, also without this mod (wotr-testing),
        /// so this test runs on the game's Unity Mono only; scripts/Invoke-OfflineTests.ps1 leaves it out on netfx.
        /// </summary>
        [Fact]
        [Trait("Runtime", "UnityMono")]
        public void AfterTheAiIsSwitchedOffTheTurnGoesOnAsAPlayerTurn()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(false, "p1"), 1);
            UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
            var log = new TurnLog();
            var turn = SwitchTheCompanionsAiOffDuringItsTurn(battle, log);
            var aiCommands = AiCommandsGiven(log, companion);

            // The game's AI chose the companion's first action when the turn started, before the player could switch.
            // That action finishes; the game's AI gives the companion no further commands.
            for (var frame = 0; frame < 3000 && CurrentTurn == turn && !turn.CanEndTurnAndNoActing(); frame++)
                battle.Run(OneFrame);
            Assert.Same(turn, CurrentTurn);
            Assert.True(turn.CanEndTurnAndNoActing());
            Assert.True(companion.IsDirectlyControllable);

            // From here the turn is a turn of a unit the player controls: it waits for the player while the companion has
            // time left, and the game ends it when the companion has none. (In this battle the AI's first action is a full
            // attack, which takes the whole round.)
            var timeLeft = turn.GetRemainingTime(companion) > 0f || turn.HasExtraAction(companion);
            for (var frame = 0; frame < 150 && CurrentTurn == turn; frame++)
                battle.Run(OneFrame);
            Assert.Equal(timeLeft, CurrentTurn == turn);
            if (CurrentTurn == turn) turn.ForceToEnd();
            Assert.Equal(aiCommands, AiCommandsGiven(log, companion));

            // From then on the companion's turns wait for the player.
            var next = RunUntilTurn(battle, t => t != turn && t.Rider == companion, player);
            Assert.NotNull(next);
            Assert.True(companion.IsDirectlyControllable);
            Assert.Equal(TurnController.TurnStatus.Preparing, next.Status);
            Assert.Equal(aiCommands, AiCommandsGiven(log, companion));
        }

        [Fact]
        public void SwitchingTheAiOnDuringAPlayerTurnTakesEffectFromTheNextTurn()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(), 1);
            var party = Party(battle);
            var log = new TurnLog();
            battle.Subscribe(log);

            var turn = RunUntilTurn(battle, t => party.Contains(t.Rider) && t.Status == TurnController.TurnStatus.Preparing);
            Assert.NotNull(turn);
            var unit = turn.Rider;
            unit.IsAIEnabled = true;

            // The turn that already waits for the player stays the player's.
            battle.Run(TimeSpan.FromSeconds(5));
            Assert.Same(turn, CurrentTurn);
            Assert.True(unit.IsDirectlyControllable);
            Assert.Equal(TurnController.TurnStatus.Preparing, turn.Status);
            Assert.Equal(0, AiCommandsGiven(log, unit));
            turn.ForceToEnd();

            // Its next turn is the AI's.
            var others = party.Where(u => u != unit).ToArray();
            var next = RunUntilTurn(battle, t => t != turn && t.Rider == unit, others);
            Assert.NotNull(next);
            Assert.False(unit.IsDirectlyControllable);
            Assert.True((bool)unit.PreventDirectControl);
        }

        [Fact]
        public void EndingCombatDuringAnAiPlayedTurnGivesThePartyBackToThePlayer()
        {
            game.RequireGame();
            using var battle = OfflineBattle.Start(Skirmish(false, "p0", "p1"), 1);
            var party = Party(battle);
            var turn = RunUntilTurn(battle, t => party.Contains(t.Rider) && IsAiPlayed(t.Rider));
            Assert.NotNull(turn);

            // Combat ends at once, as the game's End Combat cutscene command ends it and as unloading the area for a save
            // that is being loaded makes the party leave combat (Game.HandleAreaBeginUnloading).
            EndCombat();

            Assert.False(Game.Instance.Player.IsInCombat);
            Assert.Null(CurrentTurn);
            Assert.All(party, u => Assert.False((bool)u.PreventDirectControl));
            Assert.All(party, u => Assert.True(u.IsDirectlyControllable));
        }

        /// <summary>What the game's EtudeBracketForceCombatMode does when its etude starts (true) and ends (false).</summary>
        private static void ForceTurnBasedCombat(bool force)
        {
            Game.Instance.Player.IsTurnBasedStateMandatory = force;
            Game.Instance.Player.MandatoryTurnBasedModeState = force;
            EventBus.RaiseEvent((ITurnBasedModeEnabledHandler h) => h.UpdateMandatoryTurnBasedModeState());
        }

        [Fact]
        public void WhenTheGameForcesTurnBasedCombatTheAiButtonDecides()
        {
            game.RequireGame();
            // A player who plays in real time switches the companion's AI off before the fight; the main character's
            // AI stays as the game sets it.
            var scenario = Skirmish(false);
            scenario.TurnBased = false;
            scenario.Units.First(u => u.Id == "p1").WithAI(false);
            using var battle = OfflineBattle.Start(scenario, 1);
            var setting = SettingsRoot.Game.TurnBased.EnableTurnBasedMode;
            Assert.False((bool)setting, "The session's turn-based setting is on; this test needs a player who plays in real time.");
            UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
            // The AI button shows the real-time switch: on by default for the main character, off for the companion.
            Assert.True(player.IsAIEnabled);
            Assert.False(companion.IsAIEnabled);

            ForceTurnBasedCombat(true);
            try
            {
                Assert.True(Game.Instance.Player.IsTurnBasedModeOn());
                var log = RunUntilBothHadATurn(battle);
                Assert.True(CombatController.IsInTurnBasedCombat());
                Assert.True(AiPlayed(log, player));
                Assert.False(AiPlayed(log, companion));
            }
            finally
            {
                // The fight ends before its etude does. Then the turn-based controller follows the setting again, as it
                // does when it starts (CombatController.Start); out of combat it leaves turn-based mode without pausing.
                EndCombat();
                ForceTurnBasedCombat(false);
                EventBus.RaiseEvent((ITurnBasedModeEnabledHandler h) => h.HandleTurnBasedModeStateChanged(setting));
            }
        }
    }
}
