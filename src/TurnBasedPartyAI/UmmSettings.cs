using System;
using UnityEngine;
using UnityModManagerNet;

namespace TurnBasedPartyAI
{
    /// <summary>
    /// The mod's settings in Unity Mod Manager's window (Ctrl+F10, the mod's row), with or without Mod Menu: the kinds'
    /// modes, a mode for the characters selected in the game, and every single character's mode with a button to remove
    /// it. The values are the same as on the Mod Menu page.
    /// </summary>
    internal static class UmmSettings
    {
        /// <summary>What the last button did. Always drawn, so a click does not change the controls within a frame.</summary>
        private static string message = "";

        private static bool failed;
        private static GUIStyle wrapped;

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Draw();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                // OnGUI runs every frame while the window is open; log the first failure only.
                if (failed) return;
                failed = true;
                Main.Logger?.LogException(e);
            }
        }

        private static void Draw()
        {
            var bold = UnityModManager.UI.bold ?? GUI.skin.label;
            wrapped ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            var modes = new[]
            {
                PlayerSettings.ModeName(TurnMode.FollowAiButton),
                PlayerSettings.ModeName(TurnMode.AlwaysAi),
                PlayerSettings.ModeName(TurnMode.NeverAi),
            };

            GUILayout.Label(Strings.Text("Who plays turn-based turns", "回合制中谁来走回合"), bold);
            var toyBox = Main.ToyBoxLoaded;
            foreach (var kind in TurnRules.KindsInSettings(toyBox))
            {
                var (english, chinese) = PlayerSettings.KindTitle(kind);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Strings.Text(english, chinese), GUILayout.Width(340));
                var current = (int)TurnRules.KindMode(kind);
                var chosen = GUILayout.Toolbar(current, modes, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (chosen != current) PlayerSettings.SetKindMode(kind, TurnRules.ToMode(chosen));
            }
            GUILayout.Label(Strings.Text(PlayerSettings.ModesHelpEnglish, PlayerSettings.ModesHelpChinese), wrapped);
            if (toyBox) GUILayout.Label(Strings.Text(PlayerSettings.SummonsHelpEnglish, PlayerSettings.SummonsHelpChinese), wrapped);

            GUILayout.Space(12);
            GUILayout.Label(Strings.Text("Single characters", "单个角色"), bold);
            var selected = PlayerSettings.SelectedCharacters();
            if (selected.Count == 0)
            {
                GUILayout.Label(Strings.Text(
                    "Select characters in the game to give them their own mode. It replaces their kind's mode, in every save of this playthrough.",
                    "在游戏中选中角色后，可以给它们单独设置模式。它替代角色所属类别的模式，对这一周目的所有存档有效。"), wrapped);
            }
            else
            {
                GUILayout.Label(Strings.Text("Selected: ", "选中的角色：") + PlayerSettings.Describe(selected), wrapped);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Strings.Text("As its kind", "跟随类别"), GUILayout.ExpandWidth(false)))
                    message = PlayerSettings.Apply(selected, null);
                if (GUILayout.Button(modes[1], GUILayout.ExpandWidth(false)))
                    message = PlayerSettings.Apply(selected, TurnMode.AlwaysAi);
                if (GUILayout.Button(modes[2], GUILayout.ExpandWidth(false)))
                    message = PlayerSettings.Apply(selected, TurnMode.NeverAi);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            var own = TurnRules.OverrideList();
            if (own.Count == 0)
                GUILayout.Label(Strings.Text("No character has its own mode.", "没有单独设置模式的角色。"));
            foreach (var (id, mode, name) in own)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{name}: {PlayerSettings.ModeName(mode)}", GUILayout.Width(340));
                if (GUILayout.Button(Strings.Text("Remove", "清除"), GUILayout.ExpandWidth(false)))
                    message = PlayerSettings.Remove(id, name);
                GUILayout.EndHorizontal();
            }
            if (own.Count > 1 && GUILayout.Button(Strings.Text("Remove all", "全部清除"), GUILayout.ExpandWidth(false)))
                message = PlayerSettings.RemoveAll();

            GUILayout.Label(message, wrapped);
        }
    }
}
