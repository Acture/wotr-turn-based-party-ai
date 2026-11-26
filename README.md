# Turn-Based Party AI

Let Pathfinder: Wrath of the Righteous's own AI play turns for the party members and pets you choose
in turn-based combat—so you do not have to micro-manage every routine fight, while keeping instant
control over any character whenever you want.

In the unmodded game (2.7.0), the AI button works in real time, and turn-based mode saves its own
state for that button but never acts on it: every turn of a character you control still waits for
your commands. With this mod, the AI button works in turn-based mode too. Characters whose button is
off wait for you as before.

- **Download:** [Nexus Mods](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1278) · [GitHub Releases](https://github.com/Acture/wotr-turn-based-party-ai/releases) · [Changelog](CHANGELOG.md)
- **Requirements:** Wrath of the Righteous `2.7.0`, [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) `0.27.11` or later (no other mod required)
- **Language:** [English](#features) · [中文说明](#中文说明) · [Developers](#developer--contributing)

## Features

- **Per-character AI in turn-based combat:** Flip the AI button on the action bar or a party
  portrait to hand a character's turns to the game's AI, and flip it off at any time. The button
  switches real-time and turn-based AI together.
- **Instant takeover mid-turn:** Switch a character's AI off during its AI-played turn and the rest
  of that turn is handed back to you at once.
- **Uses the game's own AI:** Each character uses its own built-in AI brain—the same one it uses in
  real time—and the game's **Limited AI** difficulty option applies just as it does in real time.
- **Flexible per-kind and per-character rules:** Configure **Main character**, **Companions**,
  **Pets** (and **Summons** when ToyBox is installed) to `"Follow the AI button"`, `"Always the AI"`,
  or `"Never the AI"`, plus override individual selected characters with `"Always the AI"`,
  `"Never the AI"`, or `"As its kind"`.
- **UMM & optional Mod Menu settings (EN / 简体中文):** Configure in Unity Mod Manager's window
  (`Ctrl+F10`), or in the game's **Options → Mods** with Mod Menu installed. Settings automatically
  display in English or Simplified Chinese to match the game's language.
- **Clean saves and default-off behavior:** The mod keeps none of its own data in your saves; the AI
  buttons are the game's own switches, which the game saves. With every AI button off and the
  default settings, turn-based combat runs exactly as without the mod.

## Install & Quick Check

1. **Install Unity Mod Manager:** Install
   [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (`0.27.11` or later) for Wrath of the
   Righteous `2.7.0`. No other mod is required; the mod uses Unity Mod Manager's bundled Harmony and
   declares game version `2.7.0` (Unity Mod Manager will not load it on older versions).
2. **Add the mod zip:** Download `TurnBasedPartyAI-<version>.zip` from
   [Nexus Mods](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1278) or
   [GitHub Releases](https://github.com/Acture/wotr-turn-based-party-ai/releases), and install it in
   Unity Mod Manager's **Mods** tab.
   - *Optional:* [Mod Menu](https://github.com/WittleWolfie/ModMenu/releases) (tested with `2.0.14`)
     puts the same settings in **Options → Mods → Turn-Based Party AI**.
   - *Optional:* [ToyBox](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/8) is needed
     only if you want to control summons (see [Settings](#settings)).
3. **Verify in-game:** Launch the game, press **`Ctrl+F10`** to open Unity Mod Manager, and click
   **Turn-Based Party AI**. You should see the **Who plays turn-based turns** controls
   (`"Follow the AI button"`, `"Always the AI"`, `"Never the AI"`) and **Single characters** controls
   (`"As its kind"`, `"Always the AI"`, `"Never the AI"`). In turn-based combat, switch a
   companion's AI button on—starting from their next turn, the game's AI plays their turns until you
   switch it off.

## How to Use

In turn-based combat, switch a character's AI on with the AI button on the action bar or its party
portrait. From that character's next turn, the AI plays its turns until you switch it off again.
Switching the AI off during its AI-played turn hands the rest of that turn back to you immediately.

By default, nothing changes until you switch a character's AI on in turn-based mode. The game keeps
two AI switches for each character—one for real time (on by default) and one for turn-based mode
(off by default)—and saves both; the AI button shows the one for the mode set in the game's options.
Two cases can make the AI act right away even if you have not clicked anything since installing:

- **Previously toggled in turn-based mode:** If you clicked a character's AI button in turn-based
  mode before installing the mod, its turn-based switch is already saved as on (the unmodded game
  ignores it). The AI plays its turns right away, and the button shows it as on.
- **Forced turn-based fights while playing in real time:** In a fight the game forces into
  turn-based mode while your option is real time, the button shows the real-time switch, which is on
  unless you switched it off. The AI then plays those characters' turns until you click the button
  off or set them to `"Never the AI"`.

## Settings

Open Unity Mod Manager's window (**`Ctrl+F10`**) and click **Turn-Based Party AI**. With Mod Menu
installed, the same settings are also under **Options → Mods → Turn-Based Party AI**; both show the
same values. All settings text appears in English or Simplified Chinese to match the game's
language.

### By character kind

Each kind can be set to **`Follow the AI button`** (default), **`Always the AI`**, or
**`Never the AI`**:

| Kind | Who belongs to it |
|---|---|
| **Main character** | Your main character |
| **Companions** | Story companions, mercenaries, and any other character you control that is not one of the others |
| **Pets** | Animal companions and other pets |
| **Summons** | Summoned creatures; offered only when ToyBox is installed |

### Single characters

Select one or more characters in the game, then set them to **`Always the AI`** or **`Never the AI`**
to override their kind's mode (**`As its kind`** removes the override):

- **Unity Mod Manager window (`Ctrl+F10`):** Shows the currently selected characters' active modes,
  lists every character with its own mode, and provides a **`Remove`** button for each (plus
  **`Remove all`** when more than one is listed).
- **Mod Menu page (Options → Mods):** Choose a mode in **Selected characters** and click
  **`Apply`**. Use **`Show`** to view the selected characters' current modes and **`Remove`** to
  clear every single character's mode.

### Summons (with ToyBox)

The unmodded game never lets you control summons—its AI plays their turns with or without this mod,
so a summons rule changes nothing on its own. When ToyBox's **"Make Controllable"** option for
summons is on, summons follow this mod's **Summons** setting and single-character overrides like any
other character.

Changes take effect from each character's next turn.

## Save Safety, Uninstall & Compatibility

- **Your saves stay clean:** The mod keeps none of its own data in your saves; the AI buttons are
  the game's own switches, which the game saves. Mod settings live in plain text files in the mod's
  own folder—`KindModes.txt` for kind modes (applies across every save) and `CharacterModes.txt` for
  single-character overrides (keyed to each character, so it applies across every save of that
  playthrough).
- **Safe to install or uninstall mid-playthrough:** To uninstall, disable or remove the mod in Unity
  Mod Manager. Your saves do not depend on it, but the AI buttons stay as you last set them: a
  character whose AI you switched off in turn-based mode with the mod has it off in real time too,
  until you switch it on again.
- **Patch rollback safety:** If any Harmony patch fails to apply when loading (for example after an
  incompatible game update), the mod removes all of its patches and Unity Mod Manager reports it as
  not loaded instead of running half-patched.
- **Compatibility:** Built for Wrath of the Righteous `2.7.0` and Unity Mod Manager `0.27.11` or
  later, using Unity Mod Manager's bundled Harmony. No other mod is required; Mod Menu (`2.0.14`
  tested) and ToyBox are optional.

## Known Limitations

- During a character's AI-played turn you cannot select or command that character.
- After you switch the AI off during an AI-played turn, an action the AI had already started (a full
  attack, for example) finishes first. If it used the character's whole round, the turn then ends.
- Switching the AI on, or changing a setting, during a turn that waits for you takes effect from
  that character's next turn.
- The Mod Menu page cannot list single characters' modes because Mod Menu pages are fixed when the
  game starts; use Unity Mod Manager's window (`Ctrl+F10`) to view and remove individual entries. A
  summon's own mode lasts as long as that summon: a new summon is a new character.
- What a character does depends on its own AI brain. The mod does not change how the game's AI
  chooses actions or targets.
- **Early preview testing status:** Only lightly tested in the game so far, at the start of the
  first chapter, with a large mod list (Mod Menu, ToyBox and TabletopTweaks among others):
  companions' turns played by the AI and the settings in Unity Mod Manager's window worked without
  problems. Not yet checked in the game: switching the AI off during an AI-played turn, the Mod Menu
  page, summons made controllable with ToyBox, pets, a fight that forces turn-based mode, and
  leaving turn-based mode, loading a save or leaving to the main menu during an AI-played turn.
  Please report what you find.

## 中文说明

**回合制队伍 AI**：在回合制战斗中，由游戏自带的 AI 替你选定的队员和宠物走回合——省去每场杂兵战逐个操作的繁琐，同时随时保留每个角色的手动控制权。

原版游戏（2.7.0）在回合制下虽然保存了 AI 按钮的开关状态，却从不使用它。本模组让 AI 按钮在回合制下也生效，并且同时切换即时制和回合制；关闭 AI 按钮的角色仍像原版一样等你操作。

- **下载与安装**：从 [Nexus Mods](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1278) 或
  [GitHub Releases](https://github.com/Acture/wotr-turn-based-party-ai/releases) 下载
  `TurnBasedPartyAI-<version>.zip`，在 Unity Mod Manager（`0.27.11` 或更高版本，需要游戏 `2.7.0`）的
  Mods 页添加即可。无需其他前置模组；装了 Mod Menu 时会在游戏“选项 → Mods”里同步显示设置，装了 ToyBox 才需要召唤物设置。
- **验证与使用**：进游戏按 **`Ctrl+F10`** 打开 UMM 窗口点开本模组即可看到设置（界面随游戏语言自动显示简体中文或英文）。
  默认什么都不变：在回合制下打开某个角色的 AI 按钮后，从它的下一个回合起由 AI 走；在它的回合中关掉 AI，剩下的部分立刻交还给你。
- **设置规则**：可以按主角、队友、宠物（装了 ToyBox 时还有召唤物）设为“跟随 AI 按钮”“总是 AI”或“从不 AI”，也可以给游戏中选中的角色单独设为“总是 AI”或“从不 AI”（“跟随类别”可取消）。
  类别设置保存在模组目录的 `KindModes.txt`（对所有存档生效），单角色设置保存在 `CharacterModes.txt`（对该角色所在周目的所有存档生效）。
- **存档安全与中途卸载**：模组自己的数据不写入存档；AI 按钮的开关是游戏自己的，由游戏保存。可随时在 UMM 中禁用或卸载，存档不依赖本模组，但卸载后 AI 按钮保持你最后的设置（装着模组时在回合制里关掉的 AI，即时制里也是关的，需要时手动打开）。所有 AI 按钮都关、设置为默认时，回合制战斗和没装模组时完全一样。若加载时任一 Harmony 补丁失败，模组会自动撤回全部补丁并提示未加载。
- **注意**：装模组之前在回合制下点开过 AI 按钮的角色，存档里已经是开的，装上后会直接由 AI 走。目前只在游戏里浅浅测了第一章开头（AI 替队友走回合、UMM 设置面板正常），其余情况还没在游戏里测过，欢迎反馈。

---

## Developer & Contributing

### Build

Requires Windows, the .NET SDK, a WotR `2.7.0` installation and Mod Menu installed in its `Mods`
folder (or `-p:ModMenuDll=path/to/ModMenu.dll`); the mod is compiled against Mod Menu but does not
need it to run. Create an ignored `GamePath.props` at the repository root (or pass
`-p:WrathInstallDir=...`, or set the `WrathPath` / `WRATH_PATH` environment variable):

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
turn controller then lets its AI play the turn, as it does for enemies and allies, while a small
patch on `BlueprintAiAction.IsActorValid` keeps the Limited AI option restricting actions as it does
in real time. The flag is released when the turn ends, when combat ends, when the player leaves
turn-based mode, when the AI is switched off and when the area is unloaded; it is not saved. If any
Harmony patch fails to apply when loading, the mod removes all of its patches and Unity Mod Manager
reports it as not loaded instead of running half-patched.

### Offline tests

The offline tests run the game's own code (turn-based combat, its AI and rules) on the game's Unity
Mono runtime without starting the game, through the
[WotR.Testing](https://github.com/Acture/wotr-testing) NuGet package. They load the mod as Unity Mod
Manager does, with Mod Menu loaded first, and fight turn-based battles with the game's units. They
need the same game installation and Mod Menu as the build.

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

### Repository checks (CI)

GitHub Actions runs only the repository checks (plus `ruff` and `ty` on `tests/`), which need only
Python and Git:

```powershell
python tests/check_repository.py
python -m unittest discover -s tests -p 'test_*.py'
```

They check that no game files or build output are tracked, that `Info.json`, the project,
`CHANGELOG.md`, `README.md` and `docs/nexus-description.bbcode` agree on metadata and version, that
the entry method exists, and that relative Markdown links resolve to tracked files. Building the mod
and the offline tests need the game's assemblies, so they run on a machine with the game installed.

## License

[GNU Affero General Public License v3.0](LICENSE), with an
[additional permission](LICENSE-EXCEPTION.md) to link and convey it together with the proprietary
game and Unity engine assemblies. Game files are not included.
