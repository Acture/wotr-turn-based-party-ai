# Changelog

## Unreleased

- The game's AI plays the turn-based turns of party members whose AI button is on (its
  turn-based setting), through the game's own `PreventDirectControl` flag for the length of the
  turn. Characters with the button off wait for the player as before.
- The Limited AI difficulty option restricts these turns as it restricts the party AI in real time.
- Offline tests on the game's Unity Mono through the `external/wotr-testing` submodule.
