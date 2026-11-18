using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using ModMenu.Settings;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    /// <summary>
    /// The mod's page in Mod Menu (Options, Mods): a mode for each kind of unit, and a mode for the characters selected in
    /// the game. Every reference to Mod Menu's types is in this class, which runs only when Mod Menu has loaded.
    /// Mod Menu keeps the kinds' modes with the game's settings; the characters' modes are in the mod's own file.
    /// </summary>
    internal static class ModMenuSettings
    {
        private const string Root = "turnbasedpartyai";
        private static bool registered;

        private static string KindKey(UnitKind kind) => Root + ".kind." + kind.ToString().ToLowerInvariant();

        private static string ModeName(TurnMode mode) => mode switch
        {
            TurnMode.AlwaysAi => Strings.Text("Always the AI", "总是 AI"),
            TurnMode.NeverAi => Strings.Text("Never the AI", "从不 AI"),
            _ => Strings.Text("Follow the AI button", "跟随 AI 按钮"),
        };

        private static string KindName(UnitKind kind) => kind switch
        {
            UnitKind.MainCharacter => Strings.Text("main character", "主角"),
            UnitKind.Pet => Strings.Text("pets", "宠物"),
            UnitKind.Summon => Strings.Text("summons", "召唤物"),
            _ => Strings.Text("companions", "队友"),
        };

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
                    "Lets the game's AI play party members' turns in turn-based combat. The AI button switches the AI for real time and turn-based mode together.",
                    "在回合制战斗中由游戏自带的 AI 替队员走回合。AI 按钮同时切换即时制和回合制的 AI。"))
                .AddSubHeader(Strings.Create(Root + ".kinds", "Who plays turn-based turns", "回合制中谁来走回合"), true);

            AddKind(builder, modes, UnitKind.MainCharacter, "Main character", "主角");
            AddKind(builder, modes, UnitKind.Companion, "Companions (story companions and mercenaries)", "队友（故事队友和佣兵）");
            AddKind(builder, modes, UnitKind.Pet, "Pets (animal companions and the like)", "宠物（动物伙伴等）");
            AddKind(builder, modes, UnitKind.Summon, "Summons", "召唤物");

            builder.AddSubHeader(Strings.Create(Root + ".characters", "Single characters", "单个角色"), true)
                .AddDropdownButton(DropdownButton.New(Root + ".selected", 0,
                        Strings.Create(Root + ".selected.desc", "Selected characters", "选中的角色"),
                        Strings.Create(Root + ".selected.apply", "Apply", "应用"),
                        ApplyToSelected, characterModes)
                    .WithLongDescription(Strings.Create(Root + ".selected.long",
                        "Select characters in the game, choose a mode and apply it. It replaces the mode of their kind for these characters, in every save of this playthrough, and takes effect from their next turn. \"As its kind\" removes it.",
                        "先在游戏中选中角色，再选择模式并应用。它替代这些角色所属类别的模式，对这一周目的所有存档有效，从角色的下一个回合起生效。“跟随类别”会取消单独设置。")))
                .AddButton(Button.New(
                    Strings.Create(Root + ".show.desc", "Modes of the selected characters", "选中角色的模式"),
                    Strings.Create(Root + ".show", "Show", "查看"),
                    ShowSelected))
                .AddButton(Button.New(
                    Strings.Create(Root + ".clear.desc", "Remove every single character's mode", "清除所有单个角色的设置"),
                    Strings.Create(Root + ".clear", "Remove", "清除"),
                    ClearAll));

            ModMenu.ModMenu.AddSettings(builder);
            // The values Mod Menu kept from earlier sessions; later changes arrive through OnValueChanged.
            foreach (UnitKind kind in Enum.GetValues(typeof(UnitKind)))
                TurnRules.SetKindMode(kind, TurnRules.ToMode(ModMenu.ModMenu.GetSettingValue<int>(KindKey(kind))));
            registered = true;
        }

        private static void AddKind(SettingsBuilder builder, List<LocalizedString> modes, UnitKind kind, string english, string chinese)
        {
            var key = KindKey(kind);
            builder.AddDropdownList(DropdownList.New(key, (int)TurnMode.FollowAiButton, Strings.Create(key + ".desc", english, chinese), modes)
                .OnValueChanged(value => TurnRules.SetKindMode(kind, TurnRules.ToMode(value)))
                .WithLongDescription(Strings.Create(key + ".long",
                    "Follow the AI button: the AI plays a turn when the character's AI button is on. Always the AI: whatever the button shows. Never the AI: the player plays it, as without the mod. A single character's mode replaces this. Changes take effect from the next turn; switching a character's AI button off during its AI-played turn always gives the rest of that turn to the player.",
                    "跟随 AI 按钮：角色的 AI 按钮打开时由 AI 走回合。总是 AI：不管按钮显示什么。从不 AI：由玩家操作，和没装模组时一样。单个角色的设置优先于这里。修改从下一个回合起生效；在角色由 AI 走的回合中关掉它的 AI 按钮，总能立刻把这个回合剩下的部分交还给玩家。")));
        }

        private static List<UnitEntityData> SelectedCharacters()
        {
            try
            {
                return Game.Instance?.SelectionCharacter?.SelectedUnits?
                    .Where(u => u != null && !u.IsDisposed && (u.IsDirectlyControllable || PartyAiTurns.IsHeld(u)))
                    .ToList() ?? new List<UnitEntityData>();
            }
            catch (Exception e)
            {
                Main.Logger?.LogException(e);
                return new List<UnitEntityData>();
            }
        }

        private static void Tell(string text) => EventBus.RaiseEvent((IWarningNotificationUIHandler h) => h.HandleWarning(text, false));

        private static void ApplyToSelected(int choice)
        {
            var characters = SelectedCharacters();
            if (characters.Count == 0)
            {
                Tell(Strings.Text("Select the characters in the game first.", "请先在游戏中选中角色。"));
                return;
            }
            TurnMode? mode = choice == 1 ? TurnMode.AlwaysAi : choice == 2 ? TurnMode.NeverAi : (TurnMode?)null;
            foreach (var unit in characters) TurnRules.SetOverride(unit, mode);
            Main.SaveOverrides();
            var names = string.Join(", ", characters.Select(u => u.CharacterName));
            Tell(mode is { } value
                ? Strings.Text($"{names}: {ModeName(value)}, from their next turn.", $"{names}：{ModeName(value)}，从下一个回合起生效。")
                : Strings.Text($"{names}: as their kind, from their next turn.", $"{names}：跟随类别，从下一个回合起生效。"));
        }

        private static void ShowSelected()
        {
            var characters = SelectedCharacters();
            if (characters.Count == 0)
            {
                Tell(Strings.Text("Select the characters in the game first.", "请先在游戏中选中角色。"));
                return;
            }
            Tell(string.Join("; ", characters.Select(u =>
            {
                var own = TurnRules.OverrideOf(u);
                var kind = TurnRules.Classify(u);
                return own is { } mode
                    ? Strings.Text($"{u.CharacterName}: {ModeName(mode)} (own)", $"{u.CharacterName}：{ModeName(mode)}（单独设置）")
                    : Strings.Text($"{u.CharacterName}: {ModeName(TurnRules.KindMode(kind))} ({KindName(kind)})", $"{u.CharacterName}：{ModeName(TurnRules.KindMode(kind))}（{KindName(kind)}）");
            })));
        }

        private static void ClearAll()
        {
            var count = TurnRules.OverrideCount;
            TurnRules.ClearOverrides();
            Main.SaveOverrides();
            Tell(Strings.Text($"Removed {count} single character modes.", $"已清除 {count} 个单个角色的设置。"));
        }
    }
}
