using System.Collections.Generic;
using System.Linq;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using ModMenu.Settings;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    /// <summary>
    /// The mod's page in Mod Menu (Options, Mods): a mode for each kind of unit (summons only with ToyBox), and a mode for
    /// the characters selected in the game. Every reference to Mod Menu's types is in this class, which runs only when
    /// Mod Menu has loaded. The values are the mod's own (<see cref="PlayerSettings"/>), kept in its folder and shared
    /// with its Unity Mod Manager panel; the page shows the kinds' modes and passes changes on.
    /// </summary>
    internal static class ModMenuSettings
    {
        private const string Root = "turnbasedpartyai";
        private static bool registered;

        private static string KindKey(UnitKind kind) => Root + ".kind." + kind.ToString().ToLowerInvariant();

        internal static void Register(UnityModManager.ModEntry modEntry)
        {
            if (registered) return;

            var modes = new List<LocalizedString>
            {
                Strings.Create(Root + ".mode.follow", "Follow the AI button", "跟随 AI 按钮"),
                Strings.Create(Root + ".mode.always", "Always the AI", "总是 AI"),
                Strings.Create(Root + ".mode.never", "Never the AI", "从不 AI"),
            };
            var characterModes = new List<LocalizedString>
            {
                Strings.Create(Root + ".mode.kind", "As its kind", "跟随类别"),
                modes[1],
                modes[2],
            };

            var builder = SettingsBuilder.New(Root, Strings.Create(Root + ".title", "Turn-Based Party AI", "回合制队伍 AI"))
                .SetMod(modEntry, false)
                .SetModDescription(Strings.Create(Root + ".description",
                    "Lets the game's AI play party members' turns in turn-based combat. The AI button switches the AI for real time and turn-based mode together. The same settings are in Unity Mod Manager's window (Ctrl+F10).",
                    "在回合制战斗中由游戏自带的 AI 替队员走回合。AI 按钮同时切换即时制和回合制的 AI。Unity Mod Manager 窗口（Ctrl+F10）中有同样的设置。"))
                .AddSubHeader(Strings.Create(Root + ".kinds", "Who plays turn-based turns", "回合制中谁来走回合"), true);

            // Summons only with ToyBox, whose "Make Controllable" option for summons lets the player control them.
            var kinds = TurnRules.KindsInSettings(Main.ToyBoxLoaded);
            foreach (var kind in kinds) AddKind(builder, modes, kind);

            builder.AddSubHeader(Strings.Create(Root + ".characters", "Single characters", "单个角色"), true)
                .AddDropdownButton(DropdownButton.New(Root + ".selected", 0,
                        Strings.Create(Root + ".selected.desc", "Selected characters", "选中的角色"),
                        Strings.Create(Root + ".selected.apply", "Apply", "应用"),
                        ApplyToSelected, characterModes)
                    .WithLongDescription(Strings.Create(Root + ".selected.long",
                        "Select characters in the game, choose a mode and apply it. It replaces the mode of their kind for these characters, in every save of this playthrough, and takes effect from their next turn. \"As its kind\" removes it. Unity Mod Manager's window lists every single character's mode.",
                        "先在游戏中选中角色，再选择模式并应用。它替代这些角色所属类别的模式，对这一周目的所有存档有效，从角色的下一个回合起生效。“跟随类别”会取消单独设置。Unity Mod Manager 窗口中列出了所有单个角色的设置。")))
                .AddButton(Button.New(
                    Strings.Create(Root + ".show.desc", "Modes of the selected characters", "选中角色的模式"),
                    Strings.Create(Root + ".show", "Show", "查看"),
                    () => Tell(PlayerSettings.Describe(PlayerSettings.SelectedCharacters()))))
                .AddButton(Button.New(
                    Strings.Create(Root + ".clear.desc", "Remove every single character's mode", "清除所有单个角色的设置"),
                    Strings.Create(Root + ".clear", "Remove", "清除"),
                    () => Tell(PlayerSettings.RemoveAll())));

            // The page shows the modes in the mod's file, which the Unity Mod Manager panel may have changed since Mod
            // Menu last saw them. Without that file (Mod Menu kept the modes before the mod had one), the mod takes
            // the modes Mod Menu kept and saves them. Taken before Mod Menu sees the page, in case it reports its own
            // values as changes.
            var fromFile = Main.KindModesSaved;
            var fileModes = kinds.Select(kind => TurnRules.KindMode(kind)).ToArray();

            ModMenu.ModMenu.AddSettings(builder);
            registered = true;

            for (var i = 0; i < kinds.Length; i++)
            {
                var mode = fromFile ? fileModes[i] : TurnRules.ToMode(ModMenu.ModMenu.GetSettingValue<int>(KindKey(kinds[i])));
                TurnRules.SetKindMode(kinds[i], mode);
                if (fromFile) ModMenu.ModMenu.SetSetting(KindKey(kinds[i]), (int)mode);
            }
            if (!fromFile) Main.SaveKindModes();
        }

        /// <summary>Shows a kind's mode chosen in the Unity Mod Manager panel.</summary>
        internal static void Show(UnitKind kind, TurnMode mode)
        {
            if (registered) ModMenu.ModMenu.SetSetting(KindKey(kind), (int)mode);
        }

        private static void AddKind(SettingsBuilder builder, List<LocalizedString> modes, UnitKind kind)
        {
            var key = KindKey(kind);
            var (english, chinese) = PlayerSettings.KindTitle(kind);
            var (longEnglish, longChinese) = kind == UnitKind.Summon
                ? (PlayerSettings.SummonsHelpEnglish + " " + PlayerSettings.ModesHelpEnglish, PlayerSettings.SummonsHelpChinese + PlayerSettings.ModesHelpChinese)
                : (PlayerSettings.ModesHelpEnglish, PlayerSettings.ModesHelpChinese);
            builder.AddDropdownList(DropdownList.New(key, (int)TurnMode.FollowAiButton, Strings.Create(key + ".desc", english, chinese), modes)
                .OnValueChanged(value => PlayerSettings.SetKindModeFromModMenu(kind, TurnRules.ToMode(value)))
                .WithLongDescription(Strings.Create(key + ".long", longEnglish, longChinese)));
        }

        private static void Tell(string text) => EventBus.RaiseEvent((IWarningNotificationUIHandler h) => h.HandleWarning(text, false));

        private static void ApplyToSelected(int choice)
        {
            TurnMode? mode = choice == 1 ? TurnMode.AlwaysAi : choice == 2 ? TurnMode.NeverAi : (TurnMode?)null;
            Tell(PlayerSettings.Apply(PlayerSettings.SelectedCharacters(), mode));
        }
    }
}
