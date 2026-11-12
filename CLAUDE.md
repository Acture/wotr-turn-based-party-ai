# Project guidance

- Read README.md before changing behavior. This is a public repository; treat every tracked file and published artifact as public.
- Toolchain: .NET SDK on Windows with a WotR 2.7.0 installation and PowerShell 7. `GamePath.props` (ignored) or `-p:WrathInstallDir=...` points at the game. Commands:
  - `dotnet build src/TurnBasedPartyAI/TurnBasedPartyAI.csproj` builds the mod and packages `artifacts/packages/TurnBasedPartyAI-<version>.zip`; it never copies into the game unless `-p:DeployToGame=true`.
  - `pwsh -NoProfile -File scripts/Invoke-OfflineTests.ps1` runs real game code offline on the game's Unity Mono through the `external/wotr-testing` submodule (`git submodule update --init external/wotr-testing`; do not initialize its nested submodules). Pass `-Runtime netfx` for .NET Framework.
- Do not claim an unconfigured or unrun check passed. Record which runtime each offline result used. Offline results never replace in-game confirmation; behavior that only the game's interface shows (buttons, selection, camera) is unverified until checked in the game.
- Change game behavior only through the game's own mechanisms, and keep the scope to party members whose turn-based AI switch is on: with the switch off, a turn must behave exactly as in the unmodded game.
- Game files: never commit, package or publish game DLLs, bundles, localization, saves or snapshots of them, including in test reports, issues or PRs. Game assemblies are compile-time references only.
- Do not launch the game, modify campaign saves or use computer use without asking first.
- Public documentation describes installation, usage, supported behavior and known limitations. Internal status, research, design notes, task plans and verification records do not belong in this repository; keep raw run reports and machine-specific measurements in the ignored artifacts/ directory.
- CLAUDE.md is the single real instruction file. AGENTS.md points to CLAUDE.md and .github/copilot-instructions.md points to ../CLAUDE.md.
- Do not add AI co-author lines. Commit only when asked; obtain authorization for pushes, PRs, history rewrites, force pushes, branch deletion, releases and visibility changes.
- Document user-visible changes in CHANGELOG.md. Published release behavior must match the tested commit.
