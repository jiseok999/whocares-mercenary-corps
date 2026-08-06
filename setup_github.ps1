#Requires -Version 5.1
<#
    알빠임? 용병단 - GitHub 업로드 스크립트 (v2)

    UPLOAD_TO_GITHUB.bat 을 더블클릭하면 이 스크립트가 실행됩니다.
    실행 내용은 프로젝트 폴더의 github_upload_log.txt 에도 기록됩니다.
#>

# 네이티브 명령(git)의 stderr 출력 때문에 스크립트가 죽는 것을 막기 위해 Continue 사용.
# 중요한 지점은 $LASTEXITCODE 로 직접 확인합니다.
$ErrorActionPreference = 'Continue'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

$RepoUrl  = 'https://github.com/jiseok999/whocares-mercenary-corps.git'
$RepoWeb  = 'https://github.com/jiseok999/whocares-mercenary-corps'
$GitUser  = 'jiseok999'
$GitEmail = 'jiseok999@users.noreply.github.com'
$Branch   = 'main'

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Warn2($msg){ Write-Host "  [!]  $msg" -ForegroundColor Yellow }
function Write-Err2($msg) { Write-Host "  [X]  $msg" -ForegroundColor Red }

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath $ProjectRoot

# 무슨 일이 있어도 로그가 남도록 기록 시작
$LogPath = Join-Path $ProjectRoot 'github_upload_log.txt'
$transcriptOn = $false
try { Start-Transcript -Path $LogPath -Force | Out-Null; $transcriptOn = $true } catch {}

try {
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor White
    Write-Host "  알빠임? 용병단 -시너지 디펜스-  GitHub 업로드" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor White
    Write-Host "  프로젝트 : $ProjectRoot"
    Write-Host "  저장소   : $RepoWeb"
    Write-Host "  PowerShell : $($PSVersionTable.PSVersion)"

    # ------------------------------------------------------ 1. git 확인
    Write-Step "1/7  git 설치 확인"
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        Write-Err2 "git이 설치되어 있지 않습니다."
        Write-Host ""
        Write-Host "  아래 중 편한 방법으로 설치한 뒤 다시 실행해 주세요."
        Write-Host "    - https://git-scm.com/download/win  (설치 옵션은 전부 기본값이면 됩니다)"
        Write-Host "    - 또는 명령 프롬프트에서:  winget install --id Git.Git -e"
        return
    }
    Write-Ok (git --version)

    # ------------------------------------------------------ 2. 구조 확인
    Write-Step "2/7  프로젝트 구조 확인"
    foreach ($required in @('Assets', 'ProjectSettings', 'Packages')) {
        if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $required))) {
            Write-Err2 "$required 폴더가 없습니다. 이 스크립트가 Unity 프로젝트 루트에 있는지 확인해 주세요."
            return
        }
    }
    Write-Ok "Assets / ProjectSettings / Packages 확인"

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.gitignore'))) {
        Write-Err2 ".gitignore 파일이 없습니다. 이대로 올리면 Library 폴더까지 올라갑니다."
        return
    }
    Write-Ok ".gitignore 확인"

    # ------------------------------------------------------ 3. 저장소 초기화
    Write-Step "3/7  git 저장소 초기화"
    if (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git')) {
        Write-Ok "이미 git 저장소입니다 (초기화 건너뜀)"
    } else {
        git init -q
        if ($LASTEXITCODE -ne 0) { Write-Err2 "git init 실패"; return }
        Write-Ok "git init 완료"
    }

    # Unity 프로젝트에 필요한 설정
    git config core.longpaths true          # 260자 넘는 경로 지원
    git config core.autocrlf false          # 줄바꿈 변환 안 함
    git config core.quotepath false         # 한글 파일명을 그대로 출력
    Write-Ok "longpaths / autocrlf / quotepath 설정"

    if (-not (git config user.name))  { git config user.name  $GitUser }
    if (-not (git config user.email)) { git config user.email $GitEmail }
    Write-Ok ("user.name = " + (git config user.name) + " / user.email = " + (git config user.email))

    # ------------------------------------------------------ 4. 스테이징
    Write-Step "4/7  업로드할 파일 수집"
    Write-Host "  파일이 많아 1~3분 정도 걸릴 수 있습니다. 창이 멈춘 것처럼 보여도 기다려 주세요."
    git add -A
    if ($LASTEXITCODE -ne 0) { Write-Err2 "git add 실패"; return }

    $staged = @(git diff --cached --name-only)
    $hasCommit = $null
    git rev-parse --verify HEAD 2>$null | Out-Null
    $hasCommit = ($LASTEXITCODE -eq 0)

    if ($staged.Count -eq 0) {
        if (-not $hasCommit) {
            Write-Err2 "커밋할 파일이 하나도 없습니다. .gitignore 설정을 확인해 주세요."
            return
        }
        Write-Warn2 "새로 올릴 변경 사항이 없습니다."
    } else {
        Write-Ok "$($staged.Count) 개 파일"
    }

    # ------------------------------------------------------ 5. 용량 점검
    Write-Step "5/7  용량 점검 (GitHub는 파일 1개가 100MB를 넘으면 거부합니다)"
    $totalBytes = 0L
    $tooBig     = New-Object System.Collections.Generic.List[string]
    $unreadable = 0
    $idx        = 0

    foreach ($rel in $staged) {
        $idx++
        if (($idx % 3000) -eq 0) {
            Write-Host ("    ... {0} / {1} 확인" -f $idx, $staged.Count) -ForegroundColor DarkGray
        }
        try {
            $full = [System.IO.Path]::Combine($ProjectRoot, $rel.Replace('/', '\'))
            # 260자가 넘는 경로는 .NET 롱패스 접두사를 붙여야 읽을 수 있습니다.
            if ($full.Length -ge 240 -and -not $full.StartsWith('\\?\')) { $full = '\\?\' + $full }
            $fi = New-Object System.IO.FileInfo($full)
            if ($fi.Exists) {
                $len = $fi.Length
                $totalBytes += $len
                if ($len -gt 100MB) {
                    $tooBig.Add(("{0}  ({1:N1} MB)" -f $rel, ($len / 1MB)))
                }
            } else {
                $unreadable++
            }
        } catch {
            # 크기를 못 재는 파일이 있어도 스크립트를 멈추지 않습니다.
            $unreadable++
        }
    }

    Write-Ok ("총 용량 약 {0:N1} MB ({1:N2} GB)" -f ($totalBytes / 1MB), ($totalBytes / 1GB))
    if ($unreadable -gt 0) {
        Write-Warn2 "$unreadable 개 파일은 크기를 확인하지 못해 합계에서 제외했습니다 (업로드에는 지장 없습니다)."
    }

    if ($tooBig.Count -gt 0) {
        Write-Err2 "100MB를 넘는 파일이 있어 이대로는 업로드할 수 없습니다:"
        $tooBig | ForEach-Object { Write-Host "        $_" -ForegroundColor Red }
        Write-Host ""
        Write-Host "  위 목록을 알려주시면 .gitignore에 추가하거나 Git LFS를 설정해 드리겠습니다."
        return
    }
    Write-Ok "100MB를 넘는 파일 없음"

    if ($totalBytes -gt 2GB) {
        Write-Warn2 "총 용량이 2GB를 넘습니다. 업로드가 오래 걸리거나 실패할 수 있습니다."
    }

    # ------------------------------------------------------ 6. 커밋
    Write-Step "6/7  커밋"
    Write-Host ""
    $answer = Read-Host "  위 내용으로 GitHub에 업로드할까요? (y/n)"
    if ($answer -ne 'y' -and $answer -ne 'Y') {
        Write-Warn2 "취소했습니다. 파일은 스테이징된 상태로 남아 있습니다."
        return
    }

    if ($staged.Count -gt 0) {
        if ($hasCommit) {
            git commit -q -m "Update project files"
        } else {
            git commit -q -m "Initial commit: Whocares Mercenary Corps - Synergy Defense (Unity 6000.2.6f2)"
        }
        if ($LASTEXITCODE -ne 0) { Write-Err2 "커밋 실패"; return }
        Write-Ok "커밋 완료"
    }

    git branch -M $Branch
    Write-Ok "브랜치: $Branch"

    # ------------------------------------------------------ 7. 푸시
    Write-Step "7/7  GitHub로 업로드"
    git remote get-url origin 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        git remote set-url origin $RepoUrl
        Write-Ok "origin 주소 설정"
    } else {
        git remote add origin $RepoUrl
        Write-Ok "origin 추가"
    }

    Write-Host ""
    Write-Host "  로그인 창이 뜨면 'Sign in with your browser'를 눌러 로그인해 주세요." -ForegroundColor Yellow
    Write-Host "  용량이 커서 업로드에 몇 분 걸릴 수 있습니다." -ForegroundColor Yellow
    Write-Host ""

    git push -u origin $Branch

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "==========================================================" -ForegroundColor Green
        Write-Host "  업로드 완료!" -ForegroundColor Green
        Write-Host "  $RepoWeb" -ForegroundColor Green
        Write-Host "==========================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "  다음 단계: APK를 Releases에 올리면 제출 준비가 끝납니다."
    } else {
        Write-Host ""
        Write-Err2 "업로드에 실패했습니다. 위 오류 메시지를 알려주시면 이어서 봐드릴게요."
    }
}
catch {
    Write-Host ""
    Write-Err2 "예상치 못한 오류가 발생했습니다:"
    Write-Host ("  " + $_.Exception.Message) -ForegroundColor Red
    Write-Host ("  " + $_.ScriptStackTrace) -ForegroundColor DarkGray
}
finally {
    if ($transcriptOn) { try { Stop-Transcript | Out-Null } catch {} }
    Write-Host ""
    Write-Host "  (실행 기록이 github_upload_log.txt 에 저장되었습니다)" -ForegroundColor DarkGray
    Write-Host ""
    try { Read-Host "엔터를 누르면 창이 닫힙니다" } catch {}
}
