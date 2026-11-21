using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    /// <summary>
    /// The mod's TurnRules, reached by reflection: the tests set what the mod's Mod Menu page sets (a mode for each kind
    /// of unit and for single characters). Mod Menu cannot build the page offline.
    /// </summary>
    internal sealed class ModRules
    {
        private readonly Type rules, mode, kind, main;

        public ModRules(WotrGameFixture game)
        {
            var mod = game.Session.ModAssembly;
            rules = mod.GetType("TurnBasedPartyAI.TurnRules", true);
            mode = mod.GetType("TurnBasedPartyAI.TurnMode", true);
            kind = mod.GetType("TurnBasedPartyAI.UnitKind", true);
            main = mod.GetType("TurnBasedPartyAI.Main", true);
        }

        private object Call(string method, params object[] arguments)
        {
            try
            {
                return rules.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        public void SetKindMode(string unitKind, string turnMode) => Call("SetKindMode", Enum.Parse(kind, unitKind), Enum.Parse(mode, turnMode));
        public void SetOverride(UnitEntityData unit, string turnMode) => Call("SetOverride", unit, turnMode == null ? null : Enum.Parse(mode, turnMode));
        public string OverrideOf(UnitEntityData unit) => Call("OverrideOf", unit)?.ToString();
        public string Classify(UnitEntityData unit) => Call("Classify", unit).ToString();
        public void SaveOverrides(string path) => Call("SaveOverrides", path);
        public int LoadOverrides(string path) => (int)Call("LoadOverrides", path);
        public string KindMode(string unitKind) => Call("KindMode", Enum.Parse(kind, unitKind)).ToString();
        public void SaveKindModes(string path) => Call("SaveKindModes", path);
        public int LoadKindModes(string path) => (int)Call("LoadKindModes", path);
        public void Reset() => Call("Reset");
        public string[] KindsInSettings(bool toyBoxLoaded) => ((Array)Call("KindsInSettings", toyBoxLoaded)).Cast<object>().Select(k => k.ToString()).ToArray();
        public bool ToyBoxLoaded => (bool)main.GetProperty("ToyBoxLoaded", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    }

    public sealed partial class PartyAiTurnTests
    {
        private static readonly AccessTools.FieldRef<UnitEntityData, bool> RealTimeSwitch = AccessTools.FieldRefAccess<UnitEntityData, bool>("m_AiEnabled");
        private static readonly AccessTools.FieldRef<UnitEntityData, bool> TurnBasedSwitch = AccessTools.FieldRefAccess<UnitEntityData, bool>("m_AiEnabledInTurnBased");

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TheAiButtonSwitchesRealTimeAndTurnBasedTogether(bool turnBasedSetting)
        {
            game.RequireGame();
            var scenario = Skirmish();
            scenario.TurnBased = turnBasedSetting;
            using var battle = OfflineBattle.Start(scenario, 1);
            var companion = battle.Unit("p1");
            // The game's defaults: on in real time, off in turn-based mode.
            Assert.True(RealTimeSwitch(companion));
            Assert.False(TurnBasedSwitch(companion));

            // The action bar's and the party portraits' AI button.
            companion.IsAIEnabled = false;
            Assert.False(RealTimeSwitch(companion));
            Assert.False(TurnBasedSwitch(companion));
            companion.IsAIEnabled = true;
            Assert.True(RealTimeSwitch(companion));
            Assert.True(TurnBasedSwitch(companion));
        }

        /// <summary>Runs until both party members have had a turn; the player ends each turn that waits for them.</summary>
        private static TurnLog RunUntilBothHadATurn(OfflineBattle battle)
        {
            var party = Party(battle);
            var log = new TurnLog();
            battle.Subscribe(log);
            var last = RunUntilTurn(battle, t => party.All(u => log.Turns.Any(x => x.Unit == u)), party);
            Assert.NotNull(last);
            return log;
        }

        private static bool AiPlayed(TurnLog log, UnitEntityData unit)
        {
            var turns = log.Turns.Where(t => t.Unit == unit).ToList();
            Assert.NotEmpty(turns);
            Assert.True(turns.All(t => t.Controllable) || turns.All(t => !t.Controllable), $"{unit.CharacterName}: some turns AI-played, some not.");
            return !turns[0].Controllable;
        }

        [Fact]
        public void AKindSetToAlwaysAiHasItsTurnsPlayedWithTheButtonOff()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            rules.SetKindMode("Companion", "AlwaysAi");
            try
            {
                using var battle = OfflineBattle.Start(Skirmish(), 1);
                Assert.All(Party(battle), u => Assert.False(u.IsAIEnabled));
                var log = RunUntilBothHadATurn(battle);
                Assert.True(AiPlayed(log, battle.Unit("p1")));
                Assert.False(AiPlayed(log, battle.Unit("p0")));
            }
            finally
            {
                rules.Reset();
            }
        }

        [Fact]
        public void AKindSetToNeverAiWaitsForThePlayerWithTheButtonOn()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            rules.SetKindMode("MainCharacter", "NeverAi");
            try
            {
                using var battle = OfflineBattle.Start(Skirmish(false, "p0", "p1"), 1);
                Assert.All(Party(battle), u => Assert.True(u.IsAIEnabled));
                var log = RunUntilBothHadATurn(battle);
                Assert.False(AiPlayed(log, battle.Unit("p0")));
                Assert.True(AiPlayed(log, battle.Unit("p1")));
            }
            finally
            {
                rules.Reset();
            }
        }

        [Fact]
        public void ACharactersOwnModeReplacesItsKinds()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            rules.SetKindMode("MainCharacter", "AlwaysAi");
            rules.SetKindMode("Companion", "NeverAi");
            try
            {
                using var battle = OfflineBattle.Start(Skirmish(), 1);
                UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
                rules.SetOverride(player, "FollowAiButton");
                rules.SetOverride(companion, "AlwaysAi");
                Assert.Equal("FollowAiButton", rules.OverrideOf(player));

                var log = RunUntilBothHadATurn(battle);
                // The main character's button is off.
                Assert.False(AiPlayed(log, player));
                Assert.True(AiPlayed(log, companion));
            }
            finally
            {
                rules.Reset();
            }
        }

        [Fact]
        public void UnitsBelongToTheKindTheGameMarksThemAs()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            using (var battle = OfflineBattle.Start(Skirmish(), 1))
            {
                Assert.Equal("MainCharacter", rules.Classify(battle.Unit("p0")));
                Assert.Equal("Companion", rules.Classify(battle.Unit("p1")));
            }

            // A summoned creature carries the game's summoned-unit buff (ContextActionSpawnMonster applies it), which
            // UnitEntityData.IsSummoned checks. The game never lets the player control a summon; here the buff is on a
            // companion, which the player controls, as ToyBox's "Make Controllable" option for summons makes them.
            var summoned = BlueprintRoot.Instance.SystemMechanics.SummonedUnitBuff;
            Assert.NotNull(summoned);
            var scenario = Skirmish();
            scenario.Units.First(u => u.Id == "p1").WithBuff(summoned.AssetGuid.ToString());
            rules.SetKindMode("Summon", "AlwaysAi");
            try
            {
                using var battle = OfflineBattle.Start(scenario, 1);
                var summon = battle.Unit("p1");
                Assert.True(summon.IsSummoned());
                Assert.Equal("Summon", rules.Classify(summon));
                Assert.False(summon.IsAIEnabled);
                var log = RunUntilBothHadATurn(battle);
                Assert.True(AiPlayed(log, summon));
            }
            finally
            {
                rules.Reset();
            }
        }

        /// <summary>
        /// Without a mod that lets the player control summons, a mode for them would change nothing: the settings page
        /// offers one only when ToyBox (Unity Mod Manager id "0ToyBox0") has loaded. The tests do not load ToyBox.
        /// </summary>
        [Fact]
        public void TheSettingsOfferAModeForSummonsOnlyWithToyBox()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            Assert.DoesNotContain(game.Session.Mods, m => m.Id == "0ToyBox0");
            Assert.False(rules.ToyBoxLoaded);
            Assert.Equal(new[] { "MainCharacter", "Companion", "Pet" }, rules.KindsInSettings(false));
            Assert.Equal(new[] { "MainCharacter", "Companion", "Pet", "Summon" }, rules.KindsInSettings(true));
        }

        [Fact]
        public void CharacterModesAreSavedAndReadBack()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            var path = Path.Combine(Path.GetTempPath(), "TurnBasedPartyAI-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                using var battle = OfflineBattle.Start(Skirmish(), 1);
                UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");
                rules.SetOverride(player, "NeverAi");
                rules.SetOverride(companion, "AlwaysAi");
                rules.SaveOverrides(path);
                File.AppendAllText(path, "not a line the mod writes\n");

                rules.Reset();
                Assert.Null(rules.OverrideOf(companion));
                Assert.Equal(1, rules.LoadOverrides(path));
                Assert.Equal("NeverAi", rules.OverrideOf(player));
                Assert.Equal("AlwaysAi", rules.OverrideOf(companion));

                rules.SetOverride(companion, null);
                Assert.Null(rules.OverrideOf(companion));
            }
            finally
            {
                rules.Reset();
                File.Delete(path);
            }
        }
    }
}
