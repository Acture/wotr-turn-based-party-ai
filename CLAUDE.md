# Project guidance

- Read README.md before changing behavior. This is a public repository; treat every tracked file and published artifact as public.
- Toolchain: .NET SDK on Windows with a WotR 2.7.0 installation and Mod Menu installed in its `Mods` folder (or `-p:ModMenuDll=...`). `GamePath.props` (ignored) or `-p:WrathInstallDir=...` points at the game. Commands:
  - `dotnet build src/TurnBasedPartyAI/TurnBasedPartyAI.csproj` builds the mod and packages `artifacts/packages/TurnBasedPartyAI-<version>.zip`; it never copies into the game unless `-p:DeployToGame=true`.
  - `dotnet test tests/TurnBasedPartyAI.OfflineTests/TurnBasedPartyAI.OfflineTests.csproj` runs real game code offline on the game's Unity Mono through the `WotR.Testing` NuGet package. Add `-p:WotrRuntime=netfx` for .NET Framework (without Mod Menu and the tests with the trait `Runtime=UnityMono`).
- Do not claim an unconfigured or unrun check passed. Record which runtime each offline result used. Offline results never replace in-game confirmation; behavior that only the game's interface shows (buttons, selection, camera) is unverified until checked in the game.
- Change game behavior only through the game's own mechanisms. The AI plays a party member's turn-based turn only when the player asked for it: the character's AI button is on (with its kind on "Follow the AI button"), or the player set its kind or the character itself to "Always the AI" in the mod's settings. Every other turn must behave exactly as in the unmodded game; with the default settings and every AI button off, a turn-based battle must run exactly as without the mod. The AI button sets the real-time and turn-based AI switches together; that is the mod's only change to real-time play.
- Game files: never commit, package or publish game DLLs, bundles, localization, saves or snapshots of them, including in test reports, issues or PRs. Game assemblies are compile-time references only.
- Do not launch the game, modify campaign saves or use computer use without asking first.
- Public documentation describes installation, usage, supported behavior and known limitations. Internal status, research, design notes, task plans and verification records do not belong in this repository; keep raw run reports and machine-specific measurements in the ignored artifacts/ directory.
- CLAUDE.md is the single real instruction file. AGENTS.md points to CLAUDE.md and .github/copilot-instructions.md points to ../CLAUDE.md.
- Do not add AI co-author lines. Commit only when asked; obtain authorization for pushes, PRs, history rewrites, force pushes, branch deletion, releases and visibility changes.
- Document user-visible changes in CHANGELOG.md. Published release behavior must match the tested commit.
