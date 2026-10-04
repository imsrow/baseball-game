# 엔진(netstandard2.1)을 Release로 빌드해 Unity 프로젝트 Plugins 폴더에 복사한다.
# 사용: powershell -ExecutionPolicy Bypass -File tools/sync-engine.ps1
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$engineProject = Join-Path $root 'src/BaseballSim.Engine/BaseballSim.Engine.csproj'
$output = Join-Path $root 'src/BaseballSim.Engine/bin/Release/netstandard2.1'
$target = Join-Path $root 'unity/BaseballProto/Assets/Plugins/BaseballSim'

dotnet build $engineProject -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "엔진 빌드 실패" }

New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item (Join-Path $output 'BaseballSim.Engine.dll') $target -Force
Copy-Item (Join-Path $output 'BaseballSim.Engine.pdb') $target -Force
Write-Host "엔진 DLL 복사 완료: $target"
