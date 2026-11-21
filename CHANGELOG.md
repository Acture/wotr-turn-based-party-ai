# Changelog

## Unreleased

- The game's AI plays the turn-based turns of party members whose AI button is on, through the
  game's own `PreventDirectControl` flag for the length of the turn. Characters with the button off
  wait for the player as before.
- The AI button switches the AI for real time and turn-based mode together. Fights that the game
  forces into turn-based mode follow the button as it shows there.
- Settings in Unity Mod Manager's window, with or without Mod Menu, and on a Mod Menu page when Mod
  Menu is installed, both with the same values: for the main character, companions and pets,
  "Follow the AI button" (the default), "Always the AI" or "Never the AI"; and a mode for single
  characters, applied to the characters selected in the game, which replaces their kind's. Unity
  Mod Manager's window lists every single character's mode with a button to remove it. Summons get
  a mode only when ToyBox is installed: the game never lets the player control them unless
  ToyBox's "Make Controllable" option for summons is on. The settings are kept in the mod's folder
  (`KindModes.txt`, `CharacterModes.txt`), not in saves. If the Mod Menu page cannot be added, the
  mod logs it; the settings stay in Unity Mod Manager's window.
- The Limited AI difficulty option restricts these turns as it restricts the party AI in real time.
- Switching a character's AI off during its AI-played turn hands the rest of the turn to the player
  at once, and the button shows it as off. An action the AI had already started finishes first.
- `Info.json` declares game version 2.7.0, so Unity Mod Manager does not load the mod on older
  games, and loads it after Mod Menu.
- If one of the mod's patches cannot be applied, the mod removes all of them and Unity Mod Manager
  reports it as not loaded, instead of running with only some of them.
- Building needs Mod Menu installed (or `-p:ModMenuDll=...`); playing does not.
- Offline tests on the game's Unity Mono through the `WotR.Testing` NuGet package
  (0.1.0-preview.4), run with `dotnet test`, with Mod Menu loaded. They switch the AI as the player
  does (`BattleUnitSpec.WithAI`) and cover the cases above, combat ending during an AI-played
  turn, and a seeded battle with every button off that must run exactly as without the mod. With
  `-p:WotrRuntime=netfx` (.NET Framework) the mod is loaded without Mod Menu and the tests marked
  `Runtime=UnityMono` are left out.
