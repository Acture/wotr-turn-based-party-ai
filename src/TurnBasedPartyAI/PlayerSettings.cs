using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace TurnBasedPartyAI
{
    /// <summary>
    /// What the player changes in the mod's settings, from the Unity Mod Manager panel (<see cref="UmmSettings"/>) or the
    /// Mod Menu page (<see cref="ModMenuSettings"/>). Every change is saved in the mod's folder at once; a kind's mode
    /// chosen in the panel is shown on the Mod Menu page too.
    /// </summary>
    internal static class PlayerSettings
    {
        internal const string ModesHelpEnglish = "Follow the AI button: the AI plays a turn when the character's AI button is on. Always the AI: whatever the button shows. Never the AI: the player plays it, as without the mod. A single character's mode replaces this. Changes take effect from the next turn; switching a character's AI button off during its AI-played turn always gives the rest of that turn to the player.";
        internal const string ModesHelpChinese = "跟随 AI 按钮：角色的 AI 按钮打开时由 AI 走回合。总是 AI：不管按钮显示什么。从不 AI：由玩家操作，和没装模组时一样。单个角色的设置优先于这里。修改从下一个回合起生效；在角色由 AI 走的回合中关掉它的 AI 按钮，总能立刻把这个回合剩下的部分交还给玩家。";
        internal const string SummonsHelpEnglish = "The game does not let the player control summons, so its AI always plays their turns. The mode for summons applies only while ToyBox's \"Make Controllable\" option for summons is on.";
        internal const string SummonsHelpChinese = "游戏本身不让玩家操控召唤物，它们的回合总是由游戏 AI 走。只有打开 ToyBox 中召唤物的“设为可控”选项时，召唤物的设置才起作用。";

        internal static string ModeName(TurnMode mode) => mode switch
        {
            TurnMode.AlwaysAi => Strings.Text("Always the AI", "总是 AI"),
            TurnMode.NeverAi => Strings.Text("Never the AI", "从不 AI"),
            _ => Strings.Text("Follow the AI button", "跟随 AI 按钮"),
        };

        /// <summary>A kind's name in the settings.</summary>
        internal static (string English, string Chinese) KindTitle(UnitKind kind) => kind switch
        {
            UnitKind.MainCharacter => ("Main character", "主角"),
            UnitKind.Pet => ("Pets (animal companions and the like)", "宠物（动物伙伴等）"),
            UnitKind.Summon => ("Summons (with ToyBox)", "召唤物（配合 ToyBox）"),
            _ => ("Companions (story companions and mercenaries)", "队友（故事队友和佣兵）"),
        };

        /// <summary>A kind's name inside a sentence.</summary>
        private static string KindName(UnitKind kind) => kind switch
        {
            UnitKind.MainCharacter => Strings.Text("main character", "主角"),
            UnitKind.Pet => Strings.Text("pets", "宠物"),
            UnitKind.Summon => Strings.Text("summons", "召唤物"),
            _ => Strings.Text("companions", "队友"),
        };

        /// <summary>A kind's mode chosen in the Unity Mod Manager panel; the Mod Menu page shows it too.</summary>
        internal static void SetKindMode(UnitKind kind, TurnMode mode)
        {
            if (TurnRules.KindMode(kind) == mode) return;
            TurnRules.SetKindMode(kind, mode);
            Main.SaveKindModes();
            Main.ShowOnModMenuPage(kind, mode);
        }

        /// <summary>A kind's mode chosen on the Mod Menu page, which shows it already.</summary>
        internal static void SetKindModeFromModMenu(UnitKind kind, TurnMode mode)
        {
            if (TurnRules.KindMode(kind) == mode) return;
            TurnRules.SetKindMode(kind, mode);
            Main.SaveKindModes();
        }

        /// <summary>The characters selected in the game that the player controls, or whose turn the AI is playing.</summary>
        internal static List<UnitEntityData> SelectedCharacters()
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

        private static string SelectFirst => Strings.Text("Select the characters in the game first.", "请先在游戏中选中角色。");

        /// <summary>Gives the characters their own mode (null: as their kind) and saves it; returns what to tell the player.</summary>
        internal static string Apply(IReadOnlyList<UnitEntityData> characters, TurnMode? mode)
        {
            if (characters.Count == 0) return SelectFirst;
            foreach (var unit in characters) TurnRules.SetOverride(unit, mode);
            Main.SaveOverrides();
            var names = string.Join(", ", characters.Select(u => u.CharacterName));
            return mode is { } value
                ? Strings.Text($"{names}: {ModeName(value)}, from their next turn.", $"{names}：{ModeName(value)}，从下一个回合起生效。")
                : Strings.Text($"{names}: as their kind, from their next turn.", $"{names}：跟随类别，从下一个回合起生效。");
        }

        /// <summary>Each character's mode and where it comes from.</summary>
        internal static string Describe(IReadOnlyList<UnitEntityData> characters)
        {
            if (characters.Count == 0) return SelectFirst;
            return string.Join("; ", characters.Select(u =>
            {
                var kind = TurnRules.Classify(u);
                return TurnRules.OverrideOf(u) is { } own
                    ? Strings.Text($"{u.CharacterName}: {ModeName(own)} (own)", $"{u.CharacterName}：{ModeName(own)}（单独设置）")
                    : Strings.Text($"{u.CharacterName}: {ModeName(TurnRules.KindMode(kind))} ({KindName(kind)})", $"{u.CharacterName}：{ModeName(TurnRules.KindMode(kind))}（{KindName(kind)}）");
            }));
        }

        /// <summary>Removes one character's own mode, by the id <see cref="TurnRules.OverrideList"/> gives.</summary>
        internal static string Remove(string id, string name)
        {
            TurnRules.RemoveOverride(id);
            Main.SaveOverrides();
            return Strings.Text($"{name}: as their kind, from their next turn.", $"{name}：跟随类别，从下一个回合起生效。");
        }

        internal static string RemoveAll()
        {
            var count = TurnRules.OverrideCount;
            TurnRules.ClearOverrides();
            Main.SaveOverrides();
            return Strings.Text($"Removed {count} single character modes.", $"已清除 {count} 个单个角色的设置。");
        }
    }
}
