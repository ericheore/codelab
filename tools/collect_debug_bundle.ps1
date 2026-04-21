param(
    [string]$Version = "1.1.0",
    [string]$ModId = "CampfireBugFix",
    [string]$GameLogPath = "",
    [string]$OutDir = "debug_bundle"
)

$ErrorActionPreference = "Stop"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$stage = Join-Path $OutDir "bundle_$timestamp"
$zip = Join-Path $OutDir "debug_bundle_$timestamp.zip"

if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
if (Test-Path $zip) { Remove-Item -Force $zip }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

# 1) 打包日志（如果你把终端输出重定向保存了）
if (Test-Path "build.log") {
    Copy-Item "build.log" (Join-Path $stage "build.log")
}

# 2) 打包产物
$distZip = Join-Path "dist" "$ModId-$Version.zip"
if (Test-Path $distZip) {
    Copy-Item $distZip (Join-Path $stage "$ModId-$Version.zip")
}

# 3) 配置文件
Get-ChildItem "mod/FocusPatch*.txt" -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $stage $_.Name)
}

# 4) manifest
if (Test-Path "mod/CampfireBugFix.json") {
    Copy-Item "mod/CampfireBugFix.json" (Join-Path $stage "CampfireBugFix.json")
}

# 5) 游戏日志（用户可选路径）
if (-not [string]::IsNullOrWhiteSpace($GameLogPath) -and (Test-Path $GameLogPath)) {
    Copy-Item $GameLogPath (Join-Path $stage "game.log")
}

# 6) 简易说明
@"
How to use this debug bundle:
1) share this zip with the developer
2) include a short repro sentence:
   "I opened <screen>, clicked <card>, pressed <key>, expected <x>, got <y>"
"@ | Out-File -FilePath (Join-Path $stage "README_debug_bundle.txt") -Encoding utf8

Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
Write-Host "完成: $zip"
