param(
    [switch]$CloseUnity
)

$ErrorActionPreference = "Stop"

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"
$batchLog = Join-Path $projectPath "compile-verify.log"
$editorLog = Join-Path $projectPath "Logs\Editor.log"

if (-not (Test-Path $unityExe)) {
    Write-Error "Unity editor not found at $unityExe"
}

function Get-LastCompileSucceededFromEditorLog {
    if (-not (Test-Path $editorLog)) { return $false }

    $lines = Get-Content $editorLog -Tail 400
    $lastError = ($lines | Select-String -Pattern "error CS\d+" | Select-Object -Last 1)
    $lastSuccess = ($lines | Select-String -Pattern "Tundra build success|Mono: successfully reloaded assembly" | Select-Object -Last 1)

    if ($null -eq $lastSuccess) { return $false }
    if ($null -eq $lastError) { return $true }

    return $lastSuccess.LineNumber -gt $lastError.LineNumber
}

$unityProc = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProc -and -not $CloseUnity) {
    if (Get-LastCompileSucceededFromEditorLog) {
        Write-Host "Unity is open and the latest Editor.log compile finished successfully."
        exit 0
    }

    Write-Error @"
Unity is open but Editor.log does not show a successful compile after the latest errors.
Recompile in the editor, or rerun with -CloseUnity to verify in batch mode.
"@
}

if ($unityProc) {
    Write-Host "Closing open Unity instance(s) for batch compile verification..."
    $unityProc | Stop-Process -Force
    Start-Sleep -Seconds 8
}

Write-Host "Running Unity batch compile check..."
if (Test-Path $batchLog) { Remove-Item $batchLog -Force }

$argumentString = "-batchmode -nographics -quit -projectPath `"$projectPath`" -executeMethod ProjectZx.Editor.CompileCheck.Run -logFile `"$batchLog`""
$process = Start-Process `
    -FilePath $unityExe `
    -ArgumentList $argumentString `
    -WorkingDirectory $projectPath `
    -Wait `
    -PassThru `
    -NoNewWindow
$exitCode = $process.ExitCode

if (Test-Path $batchLog) {
    $errors = Select-String -Path $batchLog -Pattern "error CS\d+|Scripts have compiler errors|compilation failed" -SimpleMatch:$false
    if ($errors) {
        Write-Host "Compiler errors:"
        $errors | Select-Object -First 20 | ForEach-Object { Write-Host $_.Line }
    }
}

if ($exitCode -ne 0) {
    Write-Error "Unity compile verification failed with exit code $exitCode."
}

Write-Host "Unity compile verification passed."
exit 0