@echo off
chcp 65001 > nul
title GitHub Upload - Whocares Mercenary Corps
powershell.exe -NoProfile -ExecutionPolicy Bypass -NoLogo -File "%~dp0setup_github.ps1"
echo.
echo ----------------------------------------------------------
echo  스크립트가 종료되었습니다. 이 창은 자동으로 닫히지 않습니다.
echo  문제가 있었다면 github_upload_log.txt 를 확인해 주세요.
echo ----------------------------------------------------------
pause
