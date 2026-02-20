@echo off
chcp 65001 >nul
echo.
echo ╔════════════════════════════════════════════════╗
echo ║   Создание инсталятора Controls              ║
echo ╚════════════════════════════════════════════════╝
echo.
echo Запуск PowerShell скрипта...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$PSDefaultParameterValues['*:Encoding'] = 'utf8'; & '%~dp0СоздатьИнсталятор.ps1'"

echo.
pause
