@echo off
REM Uninstall scan-agent:// protocol handler
REM This script must be run as Administrator

echo ========================================
echo Uninstalling scan-agent:// Protocol Handler
echo ========================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: This script must be run as Administrator
    echo.
    echo Right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

echo Removing protocol handler...
echo.

REM Delete registry entries
reg delete "HKEY_CLASSES_ROOT\scan-agent" /f

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Protocol handler uninstalled successfully!
    echo ========================================
    echo.
) else (
    echo.
    echo ERROR: Failed to uninstall protocol handler
    echo   (It may not have been installed)
    echo.
)

pause

