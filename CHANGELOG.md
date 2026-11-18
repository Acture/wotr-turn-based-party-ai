# Changelog

## Unreleased

- The game's AI plays the turn-based turns of party members whose AI button is on, through the
  game's own `PreventDirectControl` flag for the length of the turn. Characters with the button off
  wait for the player as before.
- The AI button switches the AI for real time and turn-based mode together. Fights that the game
  forces into turn-based mode follow the button as it shows there.
- Optional Mod Menu page: for the main character, companions, pets and summons, "Follow the AI
  button", "Always the AI" or "Never the AI"; and a mode for single characters, applied to the
  characters selected in the game, which replaces their kind's. Single characters' modes are kept
  in `CharacterModes.txt` in the mod's folder, not in saves. If the page cannot be added, the mod
  logs it and keeps its defaults.
- The Limited AI difficulty option restricts these turns as it restricts the party AI in real time.
- Switching a character's AI off during its AI-played turn hands the rest of the turn to the player
  at once, and the button shows it as off. An action the AI had already started finishes first.
- `Info.json` declares game version 2.7.0, so Unity Mod Manager does not load the mod on older
  games, and loads it after Mod Menu.
- If one of the mod's patches cannot be applied, the mod removes all of them and Unity Mod Manager
  reports it as not loaded, instead of running with only some of them.
- Building needs Mod Menu installed (or `-p:ModMenuDll=...`); playing does not.
- Offline tests on the game's Unity Mono through the `external/wotr-testing` submodule, now pinned
  to its main branch, with Mod Menu loaded. They switch the AI as the player does
  (`BattleUnitSpec.WithAI`) and cover the cases above, combat ending during an AI-played turn, and
  a seeded battle with every button off that must run exactly as without the mod. On .NET
  Framework the mod is loaded without Mod Menu and the tests marked `Runtime=UnityMono` are left
  out.
