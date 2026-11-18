# Changelog

## Unreleased

- The game's AI plays the turn-based turns of party members whose AI button is on (its
  turn-based setting), through the game's own `PreventDirectControl` flag for the length of the
  turn. Characters with the button off wait for the player as before.
- The Limited AI difficulty option restricts these turns as it restricts the party AI in real time.
- Switching a character's AI off during its AI-played turn hands the rest of the turn to the player
  at once, and the button shows it as off. An action the AI had already started finishes first.
- In fights where the game forces turn-based mode while its turn-based setting is off, the mod
  leaves every turn to the player, as the unmodded game does. The AI button there is the real-time
  switch, which is on by default.
- `Info.json` declares game version 2.7.0; Unity Mod Manager does not load the mod on older games.
- If one of the mod's patches cannot be applied, the mod removes all of them and Unity Mod Manager
  reports it as not loaded, instead of running with only some of them.
- Offline tests on the game's Unity Mono through the `external/wotr-testing` submodule, now pinned
  to its main branch. They switch the AI as the player does (`BattleUnitSpec.WithAI`) and cover the
  cases above, combat ending during an AI-played turn, and a seeded battle with every button off
  that must run exactly as without the mod. On .NET Framework the tests marked `Runtime=UnityMono`
  are left out.
