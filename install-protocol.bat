@echo off
REM Install scan-agent:// protocol handler
REM This script must be run as Administrator

echo ========================================
echo Installing scan-agent:// Protocol Handler
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

REM Get the current directory
set CURRENT_DIR=%~dp0
set EXE_PATH=%CURRENT_DIR%bin\Release\ScanAgent.exe

REM Check if executable exists
if not exist "%EXE_PATH%" (
    echo ERROR: ScanAgent.exe not found at:
    echo   %EXE_PATH%
    echo.
    echo Please build the application first:
    echo   build.bat
    echo.
    pause
    exit /b 1
)

echo Installing protocol handler for:
echo   %EXE_PATH%
echo.

REM Create registry entries
reg add "HKEY_CLASSES_ROOT\scan-agent" /ve /d "URL:Scan Agent Protocol" /f
reg add "HKEY_CLASSES_ROOT\scan-agent" /v "URL Protocol" /d "" /f
reg add "HKEY_CLASSES_ROOT\scan-agent\DefaultIcon" /ve /d "\"%EXE_PATH%\",1" /f
reg add "HKEY_CLASSES_ROOT\scan-agent\shell\open\command" /ve /d "\"\"%EXE_PATH%\"\" \"\"%%1\"\"" /f

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Protocol handler installed successfully!
    echo ========================================
    echo.
    echo You can now use URLs like:
    echo   scan-agent://scan?token=xxx^&dealId=yyy^&webHookUrl=https://example.com
    echo.
    echo To test:
    echo   1. Open a web browser
    echo   2. Enter: scan-agent://scan?token=test123
    echo   3. The application should launch and process the URL
    echo.
) else (
    echo.
    echo ERROR: Failed to install protocol handler
    echo.
)

pause

