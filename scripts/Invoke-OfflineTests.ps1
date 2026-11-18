# Requires PowerShell 7 on Windows. Runs the offline tests on the game's Unity Mono (or .NET Framework) through the
# generic runner of the wotr-testing submodule, which fails on zero tests, missing reports, timeouts and any result
# that did not pass.
[CmdletBinding()]
param(
    # Game root: a local installation or a snapshot. Defaults to GamePath.props.
    [string]$WotrInputRoot,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [ValidateSet('mono', 'netfx')][string]$Runtime = 'mono',
    # Optional test filter, e.g. "FullyQualifiedName~LimitedAi".
    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runner = Join-Path $root 'external/wotr-testing/scripts/Invoke-WotrOfflineTests.ps1'
if (-not (Test-Path -LiteralPath $runner)) {
    throw '[ENV_MISSING] external/wotr-testing is missing. Run: git submodule update --init external/wotr-testing'
}
$arguments = @{
    Project          = Join-Path $root 'tests/TurnBasedPartyAI.OfflineTests/TurnBasedPartyAI.OfflineTests.csproj'
    Runtime          = $Runtime
    Configuration    = $Configuration
    RepositoryRoot   = $root
    ResultsDirectory = Join-Path $root 'artifacts/test-results/offline'
}
if ($WotrInputRoot) { $arguments.WotrInputRoot = $WotrInputRoot }
if ($Runtime -eq 'netfx') {
    # On .NET Framework a turn-based attack by a unit the player controls ends the test process, also without this mod
    # (wotr-testing), and Mod Menu's own patches do not apply. Tests that need either carry the trait Runtime=UnityMono
    # and run on the game's Unity Mono only; the netfx run loads the mod without Mod Menu.
    $unityMonoOnly = 'Runtime!=UnityMono'
    $Filter = if ($Filter) { "($Filter)&$unityMonoOnly" } else { $unityMonoOnly }
    $env:TurnBasedPartyAIWithoutModMenu = 'true'
    Write-Host "netfx: without Mod Menu, leaving out tests with the trait Runtime=UnityMono (filter: $Filter)."
}
else {
    $env:TurnBasedPartyAIWithoutModMenu = 'false'
}
if ($Filter) { $arguments.Filter = $Filter }
& $runner @arguments
exit $LASTEXITCODE
