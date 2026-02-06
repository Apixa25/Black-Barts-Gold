# Build-Android.ps1 - Black Bart's Gold
# Builds Android APK using Unity with -noUpm to fix Package Manager startup error.
# Usage: .\Build-Android.ps1

$ErrorActionPreference = "Stop"

$UnityVersion = "6000.3.4f1"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$ProjectPath = "$PSScriptRoot\BlackBartsGold"
$LogFile = "$PSScriptRoot\build_log.txt"

if (-not (Test-Path $UnityExe)) {
    Write-Error "Unity not found at: $UnityExe"
    Write-Host "Update `$UnityVersion in this script to match your installed version."
    exit 1
}

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project not found at: $ProjectPath"
    exit 1
}

Write-Host "Building Black Bart's Gold for Android..." -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Log: $LogFile"
Write-Host ""

# -noUpm fixes: "UnityPackageManager.exe: The system cannot find the path specified"
# Unity 6 changed Package Manager structure; -noUpm disables it for batch builds.
$args = @(
    "-quit",
    "-batchmode",
    "-noUpm",
    "-projectPath", $ProjectPath,
    "-buildTarget", "Android",
    "-executeMethod", "BuildScript.BuildAndroid",
    "-logFile", $LogFile
)

& $UnityExe $args
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host "APK location: $ProjectPath\Builds\Android\BlackBartsGold.apk"
} else {
    Write-Host ""
    Write-Host "Build failed with exit code $exitCode. Check build_log.txt for details." -ForegroundColor Red
}

exit $exitCode
