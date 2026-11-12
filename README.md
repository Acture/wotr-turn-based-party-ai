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
- Characters whose button is off wait for the player, as in the unmodded game.
- The flag exists only while the turn runs and is not written to saves.

## Use

Install Unity Mod Manager for WotR, then add `TurnBasedPartyAI-<version>.zip` in its Mods tab.
In turn-based mode, switch a character's AI on with the AI button; turn-based mode keeps this
setting separately from real time.

## Known limitations

- During a character's own AI-played turn the player cannot select or command that character.
  Switching its AI off takes effect from its next turn.
- What the character does depends on its AI brain, the same one the game uses for it in real time.
- Verified offline only so far (see Tests). How the game's interface shows the AI button,
  the selection and the camera during these turns has not been checked in the game.

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

Offline results do not replace a check in the game.

## License

[GNU Affero General Public License v3.0](LICENSE).
