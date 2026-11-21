# wotr-turn-based-party-ai

Lets the game's own AI play party members' turns in turn-based combat in Pathfinder: Wrath of the
Righteous. A Unity Mod Manager mod for game version 2.7.0. Its settings are in Unity Mod Manager's
window, and on a page in the game's options when [Mod Menu](https://github.com/WittleWolfie/ModMenu)
is installed.

## Why

In real time, the AI button lets the game's AI fight for a party member. Turn-based mode keeps
its own setting of that button, but in 2.7.0 the game never acts on it: every turn of a character
the player controls waits for the player's commands.

## What it does

- When a party member's turn starts in turn-based combat and the AI is to play it (by default:
  when that character's AI button is on; see Settings), the mod hands the turn to the game. It
  holds the game's own "not under the player's direct control" flag
  (`UnitEntityData.PreventDirectControl`) until the turn ends. The game uses the same flag in story
  scenes, for fleeing characters and during resurrection. The game's turn controller then lets its
  AI choose the character's actions with the character's own AI brain, as it does for enemies and
  allies, and ends the turn.
- The AI button (on the action bar or the party portrait) switches the AI for real time and
  turn-based mode together. In the unmodded game it switches only the mode of the game's
  turn-based setting.
- The game's Limited AI difficulty option restricts these turns as it restricts the party AI in
  real time.
- Switching a character's AI off during its AI-played turn hands the rest of that turn to the
  player at once.
- Characters whose turns the AI is not to play wait for the player, as in the unmodded game. With
  every button off and the default settings, a turn-based battle runs exactly as without the mod.
- The flag is released when the turn ends, when combat ends, when the player leaves turn-based
  mode and when the area is unloaded (loading a save, leaving to the main menu). It is not written
  to saves; the game does not save during combat.

## Use

Install Unity Mod Manager (0.27.11 or later) for WotR 2.7.0, then add
`TurnBasedPartyAI-<version>.zip` in its Mods tab. In turn-based combat, switch a character's AI on
with the AI button on the action bar or its party portrait.

By default nothing changes until you switch a character's AI on in turn-based mode. The game keeps
two AI switches for each character, one for real time (on by default) and one for turn-based mode
(off by default), and saves both; the AI button shows the one for the mode set in the game's
options. Two cases look as if the mod were on by default:

- A character whose turn-based switch is already on in your save, because you clicked its AI button
  in turn-based mode before installing the mod (the unmodded game ignores it), has its turns played
  by the AI right away. The button shows it as on.
- In a fight the game forces into turn-based mode while that setting is off, the button shows the
  real-time switch, which is on unless you switched it off: the AI then plays the turns of those
  characters until the button is clicked or their kind is set to "Never the AI".

## Settings

The mod's settings are in Unity Mod Manager's window (Ctrl+F10, the mod's row), with or without
[Mod Menu](https://github.com/WittleWolfie/ModMenu). With Mod Menu installed, the page
"Turn-Based Party AI" under Options, Mods has the same settings; both show the same values.

Who plays the turn-based turns of each kind of character:

| Kind | Who belongs to it |
|---|---|
| Main character | The player's character |
| Companions | Story companions, mercenaries and any other character the player controls that is not one of the others |
| Pets | Animal companions and other pets |
| Summons | Summoned creatures. Offered only when [ToyBox](https://github.com/cabarius/ToyBox) is installed (see below) |

Each kind is "Follow the AI button" (the default), "Always the AI" or "Never the AI".

Single characters: select characters in the game and give them their own mode, which replaces
their kind's ("As its kind" removes it). Unity Mod Manager's window lists every character with its
own mode, with a button to remove it, and "Remove all". On the Mod Menu page, choose a mode and
apply it to the selected characters; "Show" tells the modes of the selected characters, "Remove"
removes every single character's mode.

The game never lets the player control summons: its AI plays their turns, with or without this
mod, so a mode for them changes nothing in the unmodded game. ToyBox's "Make Controllable" option
for summons lets the player control them; while it is on, summons follow the mode for summons and
their own modes like any other character. The settings offer the mode for summons only when ToyBox
is installed.

The settings are kept in the mod's folder: the kinds' modes in `KindModes.txt`, single characters'
modes in `CharacterModes.txt`, for every save of the same playthrough; they are not written to
saves. Changes take effect from each character's next turn.

## Known limitations

- During a character's own AI-played turn the player cannot select or command that character.
- After switching the AI off during that turn, an action the AI had already started (for example a
  full attack) finishes first. If it used the character's whole round, the turn then ends.
- Switching the AI on, or changing a setting, during a turn that waits for the player takes effect
  from that character's next turn.
- The Mod Menu page cannot list single characters' modes, because Mod Menu pages are fixed when
  the game starts; Unity Mod Manager's window lists them. A summon's own mode lasts as long as that
  summon: a new summon is a new character.
- What the character does depends on its AI brain, the same one the game uses for it in real time.
- Verified offline only so far (see Tests). Not yet checked in the game: the settings in Unity Mod
  Manager's window and on the Mod Menu page (their texts, applying modes to selected characters,
  the messages they show, both showing the same values, keeping the settings over a restart); summons made controllable with ToyBox; how the AI button, the party portraits, the
  selection and the camera behave during AI-played turns and after the AI is switched off
  mid-turn; leaving turn-based mode, loading a save and leaving to the main menu during an
  AI-played turn; a fight that forces turn-based mode; pets.

## Build

Requires Windows, the .NET SDK, a WotR 2.7.0 installation and Mod Menu installed in its `Mods`
folder (or `-p:ModMenuDll=path/to/ModMenu.dll`); the mod is compiled against Mod Menu but does not
need it to run. Create an ignored `GamePath.props` at the repository root (or pass
`-p:WrathInstallDir=...`):

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
Unity Mono runtime without starting the game, through the
[WotR.Testing](https://github.com/Acture/wotr-testing) NuGet package. They load the mod as Unity
Mod Manager does, with Mod Menu loaded first, and fight turn-based battles with the game's units.
They need the same game installation and Mod Menu as the build.

```powershell
dotnet test tests/TurnBasedPartyAI.OfflineTests/TurnBasedPartyAI.OfflineTests.csproj
dotnet test tests/TurnBasedPartyAI.OfflineTests/TurnBasedPartyAI.OfflineTests.csproj -p:WotrRuntime=netfx   # .NET Framework instead of the game's Mono
```

The package checks every run (no tests, missing reports or a different runtime fail it) and writes
its environment reports under `artifacts/bin/TurnBasedPartyAI.OfflineTests/`.

They check that the AI plays the turns it is to play and only those; the AI button switching both
modes; each kind's mode, single characters' modes, how the mod sorts characters into kinds and that
the settings offer a mode for summons only with ToyBox; that the mod gives Unity Mod Manager its
settings panel, what the panel's controls do and that each change is saved at once; the Limited AI
option; switching the AI off during an AI-played turn and on during a player's turn; combat ending
during an AI-played turn, as when a cutscene ends it or a save is loaded; a fight that forces
turn-based mode; and that with every button off a seeded battle runs exactly as it does with the
mod's patches removed.

Mod Menu builds its pages from Unity objects, which do not exist offline, so the page itself is
not built in the tests: they check that the mod then logs it and keeps working, and set the modes
the page would set directly. Unity Mod Manager's window is drawn with Unity's IMGUI, which does not
run offline either; the tests do what its controls do. On .NET Framework, Mod Menu's own patches do not apply and a
turn-based attack by a character the player controls ends the test process, also without this
mod. With `-p:WotrRuntime=netfx` the test project therefore loads the mod without Mod Menu and
leaves out the tests marked `Runtime=UnityMono`; a `--filter` of your own replaces that filter.
Leaving turn-based mode during combat pauses the game, which the offline battles do not run, so
that case is not covered offline.

Offline results do not replace a check in the game.

## License

[GNU Affero General Public License v3.0](LICENSE).
