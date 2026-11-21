# Turn-Based Party AI

A Unity Mod Manager mod for Pathfinder: Wrath of the Righteous 2.7.0. In turn-based combat, the game's
own AI plays the turns of the party members whose AI button is on, as it already does in real time.

In the unmodded game, turn-based mode keeps its own setting of the AI button but never acts on it:
every turn of a character you control waits for your commands. With this mod, the button works in
turn-based mode too. Characters whose button is off wait for you as before.

## Features

- The AI button (on the action bar or a party portrait) works in turn-based combat. It switches the
  AI for real time and turn-based mode together.
- The game's own AI plays the turn, with the character's own AI brain, the same one it uses for that
  character in real time. The game's Limited AI difficulty option applies as in real time.
- Switch a character's AI off during its turn and the rest of that turn is yours at once.
- Settings for who plays the turns of the main character, companions and pets, and summons with
  ToyBox: follow the AI button, always the AI, or never the AI. You can also give single characters
  their own mode.
- Settings in Unity Mod Manager's window, and also on a page in the game's options with Mod Menu.
- The mod keeps none of its own data in your saves; the AI buttons are the game's own switches,
  which the game saves. With every AI button off and the default settings, turn-based combat runs
  exactly as without the mod.

## Install

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (0.27.11 or later) for
   Wrath of the Righteous 2.7.0.
2. Download `TurnBasedPartyAI-<version>.zip` from
   [Nexus Mods](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1278) or the
   [GitHub releases](https://github.com/Acture/wotr-turn-based-party-ai/releases), and add it in
   Unity Mod Manager's Mods tab.
3. Optional: [Mod Menu](https://github.com/WittleWolfie/ModMenu/releases) (tested with 2.0.14) puts
   the settings in the game's options too. [ToyBox](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/8)
   is needed only for summons (see Settings).

To uninstall, disable or remove the mod in Unity Mod Manager. Your saves do not depend on it, but
the AI buttons stay as you last set them: a character whose AI you switched off in turn-based mode
with the mod has it off in real time too, until you switch it on again.

## Use

In turn-based combat, switch a character's AI on with the AI button on the action bar or its party
portrait. From that character's next turn, the AI plays its turns until you switch it off again.

By default nothing changes until you switch a character's AI on in turn-based mode. The game keeps
two AI switches for each character, one for real time (on by default) and one for turn-based mode
(off by default), and saves both; the AI button shows the one for the mode set in the game's
options. Two cases look as if the mod were on by default:

- If you clicked a character's AI button in turn-based mode before installing the mod, its
  turn-based switch is on in your save (the unmodded game ignores it). The AI plays its turns right
  away; the button shows it as on.
- In a fight the game forces into turn-based mode while you play in real time, the button shows the
  real-time switch, which is on unless you switched it off. The AI then plays those characters'
  turns until you click the button or set their kind to "Never the AI".

## Settings

Open Unity Mod Manager's window (Ctrl+F10) and click the mod's row. With Mod Menu installed, the same
settings are also under Options, Mods, "Turn-Based Party AI"; both show the same values.

Who plays the turn-based turns of each kind of character:

| Kind | Who belongs to it |
|---|---|
| Main character | Your character |
| Companions | Story companions, mercenaries and any other character you control that is not one of the others |
| Pets | Animal companions and other pets |
| Summons | Summoned creatures; offered only when ToyBox is installed |

Each kind is "Follow the AI button" (the default), "Always the AI" or "Never the AI".

**Single characters:** select characters in the game, then give them their own mode, which replaces
their kind's ("As its kind" removes it). Unity Mod Manager's window lists every character with its
own mode, with a button to remove it. On the Mod Menu page, "Show" tells the modes of the selected
characters and "Remove" removes every single character's mode.

**Summons:** the game never lets you control summons. Its AI plays their turns with or without this
mod, so a mode for them changes nothing in the unmodded game. ToyBox's "Make Controllable" option for
summons lets you control them; while it is on, summons follow the mode for summons and their own
modes like any other character.

Changes take effect from each character's next turn. The settings are kept in the mod's folder
(`KindModes.txt`, `CharacterModes.txt`) and apply to every save; they are not written to saves.

## Known limitations

- During a character's AI-played turn you cannot select or command that character.
- After you switch the AI off during that turn, an action the AI had already started (a full attack,
  for example) finishes first. If it used the character's whole round, the turn then ends.
- Switching the AI on, or changing a setting, during a turn that waits for you takes effect from that
  character's next turn.
- The Mod Menu page cannot list single characters' modes, because Mod Menu pages are fixed when the
  game starts; Unity Mod Manager's window lists them. A summon's own mode lasts as long as that
  summon: a new summon is a new character.
- What a character does depends on its AI brain. The mod does not change how the game's AI chooses.
- Only lightly tested in the game so far, at the start of the first chapter, with a large mod list
  (Mod Menu, ToyBox and TabletopTweaks among others): companions' turns played by the AI and the
  settings in Unity Mod Manager's window worked without problems. Not yet checked in the game:
  switching the AI off during an AI-played turn, the Mod Menu page, summons made controllable with
  ToyBox, pets, a fight that forces turn-based mode, and leaving turn-based mode, loading a save or
  leaving to the main menu during an AI-played turn. Please report what you find.

## 中文说明

回合制队伍 AI：在回合制战斗中，由游戏自带的 AI 替打开了 AI 按钮的队员走回合，和即时制里一样。原版游戏在回合制下
虽然保留了 AI 按钮的设置，却从不使用它。本模组让 AI 按钮在回合制下也生效，并且同时切换即时制和回合制。

下载：[Nexus Mods](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1278) 或
[GitHub Releases](https://github.com/Acture/wotr-turn-based-party-ai/releases)，在 Unity Mod Manager 的 Mods 页添加 zip。

- 默认什么都不变：在回合制下打开某个角色的 AI 按钮后，从它的下一个回合起由 AI 走。在它的回合中关掉 AI，剩下的部分立刻交还给你。
- 设置在 Unity Mod Manager 窗口（Ctrl+F10）中；装了 Mod Menu 时，游戏选项里也有同样的设置。可以按主角、队友、宠物
  （装了 ToyBox 时还有召唤物）设为“跟随 AI 按钮”“总是 AI”或“从不 AI”，也可以给选中的角色单独设置。
- 模组自己的数据不写入存档；AI 按钮的开关是游戏自己的，由游戏保存，卸载后保持你最后的设置（装着模组时在回合制里关掉的 AI，
  即时制里也是关的，需要时手动打开）。所有 AI 按钮都关、设置为默认时，回合制战斗和没装模组时完全一样。
- 注意：装模组之前在回合制下点开过 AI 按钮的角色，存档里已经是开的，装上后会直接由 AI 走。
- 目前只在游戏里浅浅测了第一章开头：AI 替队友走回合、UMM 设置面板都正常。其余情况还没在游戏里测过，欢迎反馈。

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

### How it works

When a party member's turn starts and the AI is to play it, the mod holds the game's own "not under
the player's direct control" flag (`UnitEntityData.PreventDirectControl`) until the turn ends. The
game uses the same flag in story scenes, for fleeing characters and during resurrection. The game's
turn controller then lets its AI play the turn, as it does for enemies and allies. The flag is
released when the turn ends, when combat ends, when the player leaves turn-based mode, when the AI
is switched off and when the area is unloaded; it is not saved.

## Tests

The offline tests run the game's own code (turn-based combat, its AI and rules) on the game's Unity
Mono runtime without starting the game, through the [WotR.Testing](https://github.com/Acture/wotr-testing)
NuGet package. They load the mod as Unity Mod Manager does, with Mod Menu loaded first, and fight
turn-based battles with the game's units. They need the same game installation and Mod Menu as the
build.

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

Mod Menu builds its pages from Unity objects, which do not exist offline, so the page itself is not
built in the tests: they check that the mod then logs it and keeps working, and set the modes the
page would set directly. Unity Mod Manager's window is drawn with Unity's IMGUI, which does not run
offline either; the tests do what its controls do. On .NET Framework, Mod Menu's own patches do not
apply and a turn-based attack by a character the player controls ends the test process, also
without this mod. With `-p:WotrRuntime=netfx` the test project therefore loads the mod without Mod
Menu and leaves out the tests marked `Runtime=UnityMono`; a `--filter` of your own replaces that
filter. Leaving turn-based mode during combat pauses the game, which the offline battles do not
run, so that case is not covered offline.

Offline results do not replace a check in the game.

GitHub Actions runs only the repository checks, `python tests/check_repository.py` and its tests:
no game files or build output are tracked, Info.json, the project, the changelog and the Nexus Mods
description name the same version, and links work. Building the mod and the offline tests need the
game's assemblies, so they run on a machine with the game installed.

## License

[GNU Affero General Public License v3.0](LICENSE), with an
[additional permission](LICENSE-EXCEPTION.md) to link and convey it together with the proprietary
game and Unity engine assemblies. Game files are not included.
