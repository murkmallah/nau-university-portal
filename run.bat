@echo off
title National Aviation University CMS - Live Server
cd /d "%~dp0"
echo ======================================================
echo    Starting National Aviation University Website
echo    Hot Reload (Auto-Refresh) is Enabled!
echo ======================================================
echo.
echo URL: http://localhost:5151
echo.
echo Press Ctrl + C to stop the server anytime.
echo.
taskkill /f /im NAU_Web.exe >nul 2>&1
dotnet run --project NAU_Web.csproj --launch-profile http
