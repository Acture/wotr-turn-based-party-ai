using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    /// <summary>
    /// The mod's PlayerSettings, reached by reflection: what the controls of its Unity Mod Manager panel and its Mod Menu
    /// page do. Also where the mod keeps its files, so that a test can keep them out of the mod's folder.
    /// </summary>
    internal sealed class ModSettings
    {
        private readonly Type settings, rules, main, mode, kind;

        public ModSettings(WotrGameFixture game)
        {
            var mod = game.Session.ModAssembly;
            settings = mod.GetType("TurnBasedPartyAI.PlayerSettings", true);
            rules = mod.GetType("TurnBasedPartyAI.TurnRules", true);
            main = mod.GetType("TurnBasedPartyAI.Main", true);
            mode = mod.GetType("TurnBasedPartyAI.TurnMode", true);
            kind = mod.GetType("TurnBasedPartyAI.UnitKind", true);
        }

        private object Call(string method, params object[] arguments)
        {
            try
            {
                return settings.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private object Mode(string turnMode) => turnMode == null ? null : Enum.Parse(mode, turnMode);

        public void SetKindMode(string unitKind, string turnMode) => Call("SetKindMode", Enum.Parse(kind, unitKind), Mode(turnMode));
        public string Apply(IReadOnlyList<UnitEntityData> units, string turnMode) => (string)Call("Apply", units, Mode(turnMode));
        public string Describe(IReadOnlyList<UnitEntityData> units) => (string)Call("Describe", units);
        public string Remove(string id, string name) => (string)Call("Remove", id, name);
        public string RemoveAll() => (string)Call("RemoveAll");

        public List<(string Id, string Mode, string Name)> OverrideList()
        {
            var list = (IEnumerable)rules.GetMethod("OverrideList", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            return list.Cast<object>().Select(entry =>
            {
                var type = entry.GetType();
                return ((string)type.GetField("Item1").GetValue(entry), type.GetField("Item2").GetValue(entry).ToString(), (string)type.GetField("Item3").GetValue(entry));
            }).ToList();
        }

        private FieldInfo RulesField(string name) => rules.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>Where the mod saves the single characters' modes and the kinds' modes.</summary>
        public (string Overrides, string KindModes) Files
        {
            get => ((string)RulesField("OverridesFile").GetValue(null), (string)RulesField("KindModesFile").GetValue(null));
            set
            {
                RulesField("OverridesFile").SetValue(null, value.Overrides);
                RulesField("KindModesFile").SetValue(null, value.KindModes);
            }
        }

        /// <summary>What the mod gave Unity Mod Manager to draw in its window (ModEntry.OnGUI).</summary>
        public object UmmPanel
        {
            get
            {
                var entry = main.GetField("Entry", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                return entry.GetType().GetField("OnGUI").GetValue(entry);
            }
        }
    }

    public sealed partial class PartyAiTurnTests
    {
        [Fact]
        public void KindModesAreSavedAndReadBack()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            var path = Path.Combine(Path.GetTempPath(), "TurnBasedPartyAI-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                rules.SetKindMode("Companion", "AlwaysAi");
                rules.SetKindMode("Summon", "NeverAi");
                rules.SaveKindModes(path);
                File.AppendAllText(path, "Companion\tSometimes\n");

                rules.Reset();
                Assert.Equal("FollowAiButton", rules.KindMode("Companion"));
                Assert.Equal(1, rules.LoadKindModes(path));
                Assert.Equal("AlwaysAi", rules.KindMode("Companion"));
                Assert.Equal("NeverAi", rules.KindMode("Summon"));
                Assert.Equal("FollowAiButton", rules.KindMode("MainCharacter"));

                // A kind the file does not name follows the AI button.
                File.WriteAllText(path, "Pet\tNeverAi\n");
                Assert.Equal(0, rules.LoadKindModes(path));
                Assert.Equal("FollowAiButton", rules.KindMode("Companion"));
                Assert.Equal("NeverAi", rules.KindMode("Pet"));
            }
            finally
            {
                rules.Reset();
                File.Delete(path);
            }
        }

        /// <summary>
        /// The mod's settings are in Unity Mod Manager's window, with or without Mod Menu. The window is drawn with Unity's
        /// IMGUI, which does not run offline: the test checks that the mod gives Unity Mod Manager its panel, and does what
        /// the panel's controls do (a kind's row, the buttons for the selected characters, a character's Remove button and
        /// Remove all). Each change is saved at once and takes effect in the next turn.
        /// </summary>
        [Fact]
        public void ThePanelsChangesTakeEffectAndAreSavedAtOnce()
        {
            game.RequireGame();
            var rules = new ModRules(game);
            var settings = new ModSettings(game);
            Assert.NotNull(settings.UmmPanel);

            var folder = Path.Combine(Path.GetTempPath(), "TurnBasedPartyAI-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string overridesFile = Path.Combine(folder, "CharacterModes.txt"), kindModesFile = Path.Combine(folder, "KindModes.txt");
            var modFolderFiles = settings.Files;
            settings.Files = (overridesFile, kindModesFile);
            try
            {
                settings.SetKindMode("Companion", "AlwaysAi");
                Assert.Equal("AlwaysAi", rules.KindMode("Companion"));
                Assert.Contains("Companion\tAlwaysAi", File.ReadAllLines(kindModesFile));

                using var battle = OfflineBattle.Start(Skirmish(), 1);
                UnitEntityData player = battle.Unit("p0"), companion = battle.Unit("p1");

                // With nothing selected the buttons change nothing and say so.
                Assert.NotEmpty(settings.Apply(new List<UnitEntityData>(), "AlwaysAi"));
                Assert.Empty(settings.OverrideList());

                var told = settings.Apply(new List<UnitEntityData> { player, companion }, "NeverAi");
                Assert.Contains(player.CharacterName, told);
                Assert.Contains(companion.CharacterName, told);
                Assert.Equal("NeverAi", rules.OverrideOf(companion));
                Assert.Contains(companion.CharacterName, settings.Describe(new List<UnitEntityData> { companion }));
                var listed = settings.OverrideList();
                Assert.Equal(2, listed.Count);
                Assert.Contains((companion.UniqueId, "NeverAi", companion.CharacterName), listed);
                Assert.Equal(2, File.ReadAllLines(overridesFile).Length);

                settings.Remove(companion.UniqueId, companion.CharacterName);
                Assert.Null(rules.OverrideOf(companion));
                Assert.Single(File.ReadAllLines(overridesFile));
                settings.RemoveAll();
                Assert.Empty(settings.OverrideList());
                Assert.Empty(File.ReadAllLines(overridesFile));

                // Companions: Always the AI; the main character's AI button is off.
                var log = RunUntilBothHadATurn(battle);
                Assert.True(AiPlayed(log, companion));
                Assert.False(AiPlayed(log, player));
            }
            finally
            {
                rules.Reset();
                settings.Files = modFolderFiles;
                Directory.Delete(folder, true);
            }
        }
    }
}
