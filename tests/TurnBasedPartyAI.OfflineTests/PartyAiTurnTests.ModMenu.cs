using System.Linq;
using Kingmaker.Settings;
using WotR.Testing.Offline;
using Xunit;

namespace TurnBasedPartyAI.OfflineTests
{
    public sealed partial class PartyAiTurnTests
    {
        private const string CompanionsKey = "turnbasedpartyai.kind.companion";

        /// <summary>
        /// Mod Menu loads before the mod, and the mod adds its page when the game's blueprints are loaded. Mod Menu builds
        /// a page's entries as Unity ScriptableObjects, which need the Unity engine: offline that fails, and the mod must
        /// then log it and keep working with its defaults (the other tests run with Mod Menu loaded). Where the page can be
        /// built, the test sets "Companions: Always the AI" through Mod Menu, as the page does, and fights. Mod Menu's own
        /// patches do not apply on .NET Framework, so this runs on the game's Unity Mono only.
        /// </summary>
        [Fact]
        [Trait("Runtime", "UnityMono")]
        public void TheModMenuPageSetsWhoPlaysTheTurnsOrItsFailureLeavesTheModWorking()
        {
            game.RequireGame();
            Assert.Contains(game.Session.Mods, m => m.Id == "ModMenu");

            var failure = game.Session.ModLog.FirstOrDefault(l => l.Contains("Mod Menu settings could not be added"));
            if (failure != null)
            {
                Assert.Contains(game.Session.ModLog, l => l.Contains(UnityNative.Marker) && l.Contains("ScriptableObject"));
                using var battle = OfflineBattle.Start(Skirmish(false, "p1"), 1);
                var log = RunUntilBothHadATurn(battle);
                Assert.True(AiPlayed(log, battle.Unit("p1")));
                Assert.False(AiPlayed(log, battle.Unit("p0")));
                return;
            }

            // Summons only with ToyBox, which the tests do not load.
            foreach (var kind in new[] { "maincharacter", "companion", "pet" })
                Assert.Equal(0, ModMenu.ModMenu.GetSettingValue<int>("turnbasedpartyai.kind." + kind));
            Assert.NotNull(ModMenu.ModMenu.GetSetting<SettingsEntityInt, int>("turnbasedpartyai.selected"));
            Assert.True(ModMenu.ModMenu.SetSetting(CompanionsKey, 1));
            try
            {
                using var battle = OfflineBattle.Start(Skirmish(), 1);
                var log = RunUntilBothHadATurn(battle);
                Assert.True(AiPlayed(log, battle.Unit("p1")));
                Assert.False(AiPlayed(log, battle.Unit("p0")));
            }
            finally
            {
                ModMenu.ModMenu.SetSetting(CompanionsKey, 0);
            }
        }
    }
}
