@echo off
title We-Recap Attendance - Online Server & Cloudflare Tunnel
color 0A

echo ========================================================
echo   WE-RECAP - MANAGE SERVICE SIG SITE REMBANG
echo   Menjalankan Server Lokal & Cloudflare Tunnel HTTPS
echo ========================================================
echo.

cd /d "%~dp0SiteAttendance"

echo [1/2] Menjalankan Server ASP.NET Core...
start "We-Recap Server (Port 5000)" cmd /k "dotnet run --no-launch-profile"

timeout /t 4 /nobreak >nul

echo [2/2] Membuka Cloudflare Tunnel HTTPS...
echo.
echo Link publik HTTPS Anda akan muncul di bawah ini:
echo (Salin link https://xxxx.trycloudflare.com untuk dibuka di HP)
echo ========================================================
echo.

cd /d "%~dp0tools"
cloudflared.exe tunnel --protocol http2 --url http://localhost:5000

pause
