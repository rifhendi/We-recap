@echo off
title We-Recap Site Rembang - Launcher
color 0B

echo ==========================================================
echo    MEMBUKA APLIKASI WE-RECAP (SITE REMBANG)
echo ==========================================================
echo.

cd /d "%~dp0SiteAttendance"

powershell -Command "(Test-NetConnection -Port 5000 -ComputerName 127.0.0.1 -WarningAction SilentlyContinue).TcpTestSucceeded" > "%temp%\recap_port.txt" 2>nul
set /p IS_RUNNING=<"%temp%\recap_port.txt"

if "%IS_RUNNING%"=="True" (
    echo [OK] Server lokal sudah aktif.
) else (
    echo [1/2] Menyalakan background service aplikasi...
    start /min "WeRecapBackend" cmd /c "dotnet run --no-launch-profile"
    timeout /t 4 /nobreak >nul
)

echo [2/2] Membuka jendela aplikasi desktop...

set CHROME_PATH="C:\Program Files\Google\Chrome\Application\chrome.exe"
set EDGE_PATH="C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

if exist %CHROME_PATH% (
    start "" %CHROME_PATH% --app=http://localhost:5000 --window-size=1280,820
) else if exist %EDGE_PATH% (
    start "" %EDGE_PATH% --app=http://localhost:5000 --window-size=1280,820
) else (
    start http://localhost:5000
)

exit