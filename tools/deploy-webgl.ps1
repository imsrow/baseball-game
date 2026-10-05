# WebGL 빌드 결과물(unity/BaseballProto/Builds/WebGL)을 gh-pages 브랜치에만 올린다. main에는 넣지 않는다.
# 별도 worktree에서 작업하므로 현재 작업 폴더의 브랜치·변경 사항에는 영향이 없다.
# 사용: powershell -ExecutionPolicy Bypass -File tools/deploy-webgl.ps1
# git은 진행 메시지를 stderr로 내므로 Stop 대신 종료 코드로 실패를 판단한다
$ErrorActionPreference = 'Continue'

$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $root 'unity/BaseballProto/Builds/WebGL'
$branch = 'gh-pages'
$worktree = Join-Path ([System.IO.Path]::GetTempPath()) 'baseball-gh-pages'

if (-not (Test-Path (Join-Path $build 'index.html'))) { throw "빌드 결과물이 없습니다: $build" }

function Git { git -C $root @args; if ($LASTEXITCODE -ne 0) { throw "git 실패: $args" } }
function GitWt { git -C $worktree @args; if ($LASTEXITCODE -ne 0) { throw "git 실패: $args" } }

$source = (git -C $root rev-parse --short HEAD).Trim()

if (Test-Path $worktree) { Remove-Item -Recurse -Force $worktree }
Git worktree prune

# 원격 gh-pages가 있으면 이어서, 없으면 기록 없는(orphan) 브랜치로 시작
$remote = git -C $root ls-remote --heads origin $branch
if ($remote) {
    Git fetch origin $branch
    Git worktree add -B $branch $worktree "origin/$branch"
} else {
    Git worktree add --orphan -b $branch $worktree
}

try {
    # 이전 배포 파일을 지우고 새 빌드로 교체
    Get-ChildItem -Force $worktree | Where-Object { $_.Name -ne '.git' } | Remove-Item -Recurse -Force
    Copy-Item -Recurse -Force (Join-Path $build '*') $worktree
    # Jekyll 처리 끄기 (밑줄로 시작하는 파일 등을 그대로 서비스)
    New-Item -ItemType File -Force (Join-Path $worktree '.nojekyll') | Out-Null

    GitWt add -A
    git -C $worktree diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "변경 사항 없음: 배포 생략"
    } else {
        GitWt commit -q -m "WebGL 빌드 배포 (main $source)"
        GitWt push -u origin $branch
        Write-Host "배포 완료: $branch (main $source)"
    }
} finally {
    git -C $root worktree remove --force $worktree
}
