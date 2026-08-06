<#
.SYNOPSIS
    Waits for the Unity Editor to close, then runs a headless compile of this project
    and summarises the result.

.DESCRIPTION
    Unity holds an exclusive lock on a project folder, so a batchmode run cannot start
    while the Editor has it open. This waits for the Editor to exit, compiles headlessly,
    and prints a verdict so the full log only needs opening when something broke.

    The Editor is located from ProjectSettings/ProjectVersion.txt, so both devs get the
    version the project is pinned to rather than whatever they happen to have installed.

.PARAMETER Editor
    Full path to Unity.exe. Overrides auto-discovery. Also readable from the
    SHIFT_UNITY_EDITOR environment variable.

.PARAMETER LogPath
    Where to write the Unity log. Defaults to the system temp directory, deliberately
    outside the repo.

.PARAMETER WaitMinutes
    How long to wait for the Editor to close before giving up. Default 6.

.EXAMPLE
    ./Tools/wait-and-compile.ps1
    ./Tools/wait-and-compile.ps1 -Editor "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe"
#>
param(
    [string]$Editor,
    [string]$LogPath,
    [int]$WaitMinutes = 6
)

$ErrorActionPreference = 'Continue'

$projectRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'

if (-not (Test-Path $versionFile)) {
    Write-Output "RESULT: ERROR - ProjectVersion.txt not found at $versionFile"
    exit 1
}

$version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
Write-Output "Project pins Unity $version"

# --- Locate the Editor -------------------------------------------------------------
if (-not $Editor -and $env:SHIFT_UNITY_EDITOR) { $Editor = $env:SHIFT_UNITY_EDITOR }

if (-not $Editor) {
    $candidates = @("C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe")

    # Unity Hub can install outside Program Files; that location lives in its config.
    $secondaryCfg = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $secondaryCfg) {
        $secondary = (Get-Content $secondaryCfg -Raw).Trim().Trim('"').Replace('\\', '\')
        if ($secondary) {
            $candidates += (Join-Path $secondary "$version\Editor\Unity.exe")
        }
    }

    $Editor = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $Editor) {
        Write-Output 'RESULT: ERROR - could not find Unity.exe. Tried:'
        $candidates | ForEach-Object { Write-Output "  $_" }
        Write-Output 'Pass -Editor <path> or set SHIFT_UNITY_EDITOR.'
        exit 1
    }
}

if (-not $LogPath) { $LogPath = Join-Path $env:TEMP 'shift-unity-compile.log' }

Write-Output "Editor:  $Editor"
Write-Output "Project: $projectRoot"
Write-Output "Log:     $LogPath"

# --- Phase 1: wait for the Editor to release the project lock ----------------------
$deadline = (Get-Date).AddMinutes($WaitMinutes)
while ((Get-Process -Name Unity -ErrorAction SilentlyContinue) -and ((Get-Date) -lt $deadline)) {
    Start-Sleep -Seconds 5
}

if (Get-Process -Name Unity -ErrorAction SilentlyContinue) {
    Write-Output "RESULT: TIMEOUT_EDITOR_STILL_OPEN (waited $WaitMinutes min)"
    exit 2
}

Write-Output 'Editor closed. Starting batchmode package resolve + compile...'
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

# --- Phase 2: headless import + script compile -------------------------------------
# Unity.exe is a GUI-subsystem binary, so the call operator (&) returns immediately
# without waiting and never sets $LASTEXITCODE. Start-Process -Wait -PassThru is what
# actually blocks until Unity exits and exposes a real exit code.
$proc = Start-Process -FilePath $Editor `
                      -ArgumentList @('-batchmode', '-quit', '-nographics',
                                      '-projectPath', "`"$projectRoot`"",
                                      '-logFile',     "`"$LogPath`"") `
                      -Wait -PassThru -NoNewWindow
$code = $proc.ExitCode

Write-Output "RESULT: UNITY_EXIT_CODE=$code"

if (-not (Test-Path $LogPath)) {
    Write-Output 'LOG_MISSING'
    exit $code
}

Write-Output "LOG_BYTES=$((Get-Item $LogPath).Length)"

# --- Phase 3: summarise, so a 40 KB log only gets opened when it must ---------------
$errors = Select-String -Path $LogPath -Pattern 'error CS\d+' -AllMatches |
          ForEach-Object { $_.Line.Trim() } |
          Select-Object -Unique

if ($errors) {
    Write-Output "COMPILE_ERRORS=$($errors.Count)"
    $errors | Select-Object -First 40 | ForEach-Object { Write-Output "  $_" }
} else {
    Write-Output 'COMPILE_ERRORS=0'
}

if (Select-String -Path $LogPath -Pattern 'Exiting batchmode successfully' -Quiet) {
    Write-Output 'BATCHMODE: exited successfully'
} else {
    Write-Output 'BATCHMODE: did NOT report success - inspect the log'
}

exit $code
