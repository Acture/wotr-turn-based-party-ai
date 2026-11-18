# wotr-turn-based-party-ai

Lets the game's own AI play party members' turns in turn-based combat in Pathfinder: Wrath of the
Righteous. A Unity Mod Manager mod for game version 2.7.0; it does not depend on other mods.

## Why

In real time, the AI button lets the game's AI fight for a party member. Turn-based mode keeps
its own setting of that button, but in 2.7.0 the game never acts on it: every turn of a character
the player controls waits for the player's commands.

## What it does

- When a party member's turn starts in turn-based combat and that character's AI button is on
  (its turn-based setting), the mod hands the turn to the game. It holds the game's own "not
  under the player's direct control" flag (`UnitEntityData.PreventDirectControl`) until the turn
  ends. The game uses the same flag in story scenes, for fleeing characters and during
  resurrection. The game's turn controller then lets its AI choose the character's actions with
  the character's own AI brain, as it does for enemies and allies, and ends the turn.
- The game's Limited AI difficulty option restricts these turns as it restricts the party AI in
  real time.
- Switching a character's AI off during its AI-played turn (on the action bar or the party
  portrait) hands the rest of that turn to the player at once.
- Characters whose button is off wait for the player, as in the unmodded game. With every button
  off, a turn-based battle runs exactly as without the mod.
- The flag is released when the turn ends, when combat ends, when the player leaves turn-based
  mode and when the area is unloaded (loading a save, leaving to the main menu). It is not written
  to saves; the game does not save during combat.

## Use

Install Unity Mod Manager (0.27.11 or later) for WotR 2.7.0, then add
`TurnBasedPartyAI-<version>.zip` in its Mods tab. In turn-based mode, switch a character's AI on
with the AI button on the action bar or its party portrait; turn-based mode keeps this setting
separately from real time, and it is off by default.

## Known limitations

- During a character's own AI-played turn the player cannot select or command that character.
- After switching the AI off during that turn, an action the AI had already started (for example a
  full attack) finishes first. If it used the character's whole round, the turn then ends.
- Switching the AI on during a turn that waits for the player takes effect from that character's
  next turn.
- Some fights force turn-based mode while the game's turn-based setting is off. There the AI
  button shows the real-time switch, and the mod leaves every turn to the player, as the unmodded
  game does.
- What the character does depends on its AI brain, the same one the game uses for it in real time.
- Verified offline only so far (see Tests). Not yet checked in the game: how the AI button, the
  party portraits, the selection and the camera behave during AI-played turns and after the AI is
  switched off mid-turn; leaving turn-based mode, loading a save and leaving to the main menu
  during an AI-played turn; a fight that forces turn-based mode.

## Build

Requires Windows, the .NET SDK and a WotR 2.7.0 installation. Create an ignored `GamePath.props`
at the repository root (or pass `-p:WrathInstallDir=...`):

```xml
<Project>
  <PropertyGroup>
    <WrathInstallDir>C:/Path/To/Pathfinder Second Adventure</WrathInstallDir>
  </PropertyGroup>
</Project>
```

```powershell
dotnet build src/TurnBasedPartyAI/TurnBasedPartyAI.csproj                       # builds artifacts/packages/TurnBasedPartyAI-<version>.zip
dotnet build src/TurnBasedPartyAI/TurnBasedPartyAI.csproj -p:DeployToGame=true  # also copies into <game>/Mods/TurnBasedPartyAI
```

## Tests

The offline tests run the game's own code (turn-based combat, its AI and rules) on the game's
Unity Mono runtime without starting the game, through
[wotr-testing](https://github.com/Acture/wotr-testing) in the `external/wotr-testing` submodule.
They load the mod as Unity Mod Manager does and fight turn-based battles with the game's units.

```powershell
git submodule update --init external/wotr-testing   # not --recursive
pwsh -NoProfile -File scripts/Invoke-OfflineTests.ps1
pwsh -NoProfile -File scripts/Invoke-OfflineTests.ps1 -Runtime netfx   # .NET Framework instead of the game's Mono
```

They check that the AI plays the turns of characters whose button is on and only theirs; the
Limited AI option; switching the AI off during an AI-played turn and on during a player's turn;
combat ending during an AI-played turn, as when a cutscene ends it or a save is loaded; a fight
that forces turn-based mode; and that with every button off a seeded battle runs exactly as it
does with the mod's patches removed.

On .NET Framework the script leaves out the tests marked `Runtime=UnityMono`. A turn-based attack
by a character the player controls ends the .NET Framework test process, also without this mod.
Leaving turn-based mode during combat pauses the game, which the offline battles do not run, so
that case is not covered offline.

Offline results do not replace a check in the game.

## License

[GNU Affero General Public License v3.0](LICENSE).
