using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Entities;

namespace TurnBasedPartyAI
{
    /// <summary>Who plays a turn-based turn of a unit the player controls.</summary>
    internal enum TurnMode
    {
        /// <summary>The game's AI plays it when the unit's AI button is on.</summary>
        FollowAiButton = 0,
        /// <summary>The game's AI plays it whatever the AI button shows.</summary>
        AlwaysAi = 1,
        /// <summary>The player plays it, as in the unmodded game.</summary>
        NeverAi = 2,
    }

    /// <summary>The kinds of units the player controls, each with its own <see cref="TurnMode"/>.</summary>
    internal enum UnitKind
    {
        MainCharacter = 0,
        /// <summary>Story companions, mercenaries and any other unit the player controls that is none of the others.</summary>
        Companion = 1,
        /// <summary>Animal companions and other pets (<c>UnitEntityData.IsPet</c>).</summary>
        Pet = 2,
        /// <summary>
        /// Summoned creatures (<c>UnitEntityData.IsSummoned</c>, the game's summoned-unit buff). The unmodded game never
        /// lets the player control them, so the game's AI plays their turns whatever their mode; only a mod that makes
        /// them controllable, such as ToyBox, brings them under these rules.
        /// </summary>
        Summon = 3,
    }

    /// <summary>
    /// Decides who plays a turn: a mode set for the unit itself, otherwise its kind's mode. The player sets both in the
    /// mod's Unity Mod Manager panel or its Mod Menu page (<see cref="PlayerSettings"/>); both are kept in the mod's
    /// folder. Changes take effect from the next turn that starts.
    /// </summary>
    internal static class TurnRules
    {
        private static readonly Dictionary<UnitKind, TurnMode> KindModes = new Dictionary<UnitKind, TurnMode>();

        /// <summary>Modes set for single units, by <c>UnitEntityData.UniqueId</c>, with the name the unit had then.</summary>
        private static readonly Dictionary<string, (TurnMode Mode, string Name)> Overrides = new Dictionary<string, (TurnMode, string)>(StringComparer.Ordinal);

        internal static TurnMode KindMode(UnitKind kind) => KindModes.TryGetValue(kind, out var mode) ? mode : TurnMode.FollowAiButton;

        /// <summary>
        /// The kinds the settings offer a mode for. Summons only when ToyBox has loaded: its "Make Controllable" option
        /// for summons is what lets the player control them. Without it a mode for summons would change nothing, so the
        /// settings leave it out.
        /// </summary>
        internal static UnitKind[] KindsInSettings(bool toyBoxLoaded) => toyBoxLoaded
            ? new[] { UnitKind.MainCharacter, UnitKind.Companion, UnitKind.Pet, UnitKind.Summon }
            : new[] { UnitKind.MainCharacter, UnitKind.Companion, UnitKind.Pet };

        /// <summary>A stored setting as a mode; anything unknown follows the AI button.</summary>
        internal static TurnMode ToMode(int value) => Enum.IsDefined(typeof(TurnMode), value) ? (TurnMode)value : TurnMode.FollowAiButton;

        /// <summary>Where overrides are saved; null keeps them in memory only.</summary>
        internal static string OverridesFile;

        /// <summary>Where the kinds' modes are saved; null keeps them in memory only.</summary>
        internal static string KindModesFile;

        internal static UnitKind Classify(UnitEntityData unit)
        {
            if (unit.IsSummoned()) return UnitKind.Summon;
            if (unit.IsPet) return UnitKind.Pet;
            if (unit.IsMainCharacter) return UnitKind.MainCharacter;
            return UnitKind.Companion;
        }

        internal static TurnMode? OverrideOf(UnitEntityData unit)
            => unit != null && Overrides.TryGetValue(unit.UniqueId, out var entry) ? entry.Mode : (TurnMode?)null;

        internal static TurnMode ModeOf(UnitEntityData unit) => OverrideOf(unit) ?? KindMode(Classify(unit));

        /// <summary>Whether the game's AI plays the unit's turn that starts now. The unit is one the player controls.</summary>
        internal static bool AiPlays(UnitEntityData unit)
        {
            switch (ModeOf(unit))
            {
                case TurnMode.AlwaysAi: return true;
                case TurnMode.NeverAi: return false;
                // What the AI button shows: the switch of the game's turn-based setting, and the game's CompanionsAI.
                default: return unit.IsAIEnabled;
            }
        }

        internal static void SetKindMode(UnitKind kind, TurnMode mode) => KindModes[kind] = mode;

        /// <summary>Sets the unit's own mode; null removes it, so the unit follows its kind again.</summary>
        internal static void SetOverride(UnitEntityData unit, TurnMode? mode)
        {
            if (mode is { } value) Overrides[unit.UniqueId] = (value, unit.CharacterName);
            else Overrides.Remove(unit.UniqueId);
        }

        internal static int OverrideCount => Overrides.Count;

        /// <summary>Every unit's own mode, with the name the unit had when it was set, in the order of the names.</summary>
        internal static List<(string Id, TurnMode Mode, string Name)> OverrideList()
            => Overrides.Select(e => (Id: e.Key, e.Value.Mode, e.Value.Name))
                .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();

        internal static void RemoveOverride(string id) => Overrides.Remove(id);

        internal static void ClearOverrides() => Overrides.Clear();

        /// <summary>Back to the defaults: every kind follows the AI button, no overrides, nothing saved.</summary>
        internal static void Reset()
        {
            KindModes.Clear();
            Overrides.Clear();
        }

        /// <summary>Saves the overrides, one per line: unique id, mode, name. Units of every save share the file.</summary>
        internal static void SaveOverrides(string path)
            => WriteLines(path, Overrides.OrderBy(e => e.Key, StringComparer.Ordinal)
                .Select(e => $"{e.Key}\t{e.Value.Mode}\t{e.Value.Name?.Replace('\t', ' ').Replace('\n', ' ')}"));

        /// <summary>Saves every kind's mode, one per line: kind, mode.</summary>
        internal static void SaveKindModes(string path)
            => WriteLines(path, Enum.GetValues(typeof(UnitKind)).Cast<UnitKind>().Select(kind => $"{kind}\t{KindMode(kind)}"));

        /// <summary>
        /// Replaces the kinds' modes with the file's; a kind it does not name follows the AI button. Lines that cannot be
        /// read are skipped and counted.
        /// </summary>
        internal static int LoadKindModes(string path)
        {
            KindModes.Clear();
            if (!File.Exists(path)) return 0;
            var skipped = 0;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('\t');
                if (parts.Length == 2 && Enum.TryParse(parts[0], out UnitKind kind) && Enum.IsDefined(typeof(UnitKind), kind)
                    && Enum.TryParse(parts[1], out TurnMode mode) && Enum.IsDefined(typeof(TurnMode), mode))
                    KindModes[kind] = mode;
                else
                    skipped++;
            }
            return skipped;
        }

        /// <summary>Writes a temporary file first, so that a failure leaves the old file whole.</summary>
        private static void WriteLines(string path, IEnumerable<string> lines)
        {
            var temporary = path + ".tmp";
            File.WriteAllLines(temporary, lines);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        /// <summary>Replaces the overrides with the file's. Lines that cannot be read are skipped and counted.</summary>
        internal static int LoadOverrides(string path)
        {
            Overrides.Clear();
            if (!File.Exists(path)) return 0;
            var skipped = 0;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('\t');
                if (parts.Length >= 2 && parts[0].Length > 0 && Enum.TryParse(parts[1], out TurnMode mode) && Enum.IsDefined(typeof(TurnMode), mode))
                    Overrides[parts[0]] = (mode, parts.Length > 2 ? parts[2] : "");
                else
                    skipped++;
            }
            return skipped;
        }
    }
}
