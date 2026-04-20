param(
    [string]$ModId = "CampfireBugFix",
    [string]$Version = "1.1.0",
    [string]$OutDir = "dist",
    [string]$BuildConfig = "Release",
    [string]$DllPath = "bin/$BuildConfig/net9.0/CampfireBugFix.dll",
    [string]$PckPath = "CampfireBugFix.pck",
    [string]$ManifestPath = "mod/CampfireBugFix.json"
)

$ErrorActionPreference = "Stop"

function Ensure-Exists([string]$Path, [string]$Hint) {
    if (-not (Test-Path $Path)) {
        throw "缺少文件: $Path。$Hint"
    }
}

Write-Host "[1/6] dotnet build ($BuildConfig)..."
dotnet build -c $BuildConfig

Write-Host "[2/6] 验证产物..."
Ensure-Exists $DllPath "请确认 csproj 输出路径是否正确。"
Ensure-Exists $PckPath "请先运行 Godot 打包脚本生成 .pck。"
Ensure-Exists $ManifestPath "请确认 manifest 路径。"

$stage = Join-Path $OutDir "$ModId-$Version"
$zip = Join-Path $OutDir "$ModId-$Version.zip"

Write-Host "[3/6] 清理旧产物..."
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
if (Test-Path $zip) { Remove-Item -Force $zip }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Write-Host "[4/6] 拷贝文件到 staging..."
Copy-Item $DllPath (Join-Path $stage "$ModId.dll")
Copy-Item $PckPath (Join-Path $stage "$ModId.pck")
Copy-Item $ManifestPath (Join-Path $stage "$ModId.json")
Get-ChildItem "mod/FocusPatch*.txt" -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $stage $_.Name)
}

Write-Host "[5/6] 生成 zip..."
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip

Write-Host "[6/6] 完成: $zip"
