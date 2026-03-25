@echo off
REM Build script for ScanAgent Windows application

echo ========================================
echo Building ScanAgent for Windows
echo ========================================
echo.

REM Check if MSBuild is available
where msbuild >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: MSBuild not found in PATH
    echo.
    echo Please install Visual Studio or .NET Framework SDK
    echo Or add MSBuild to your PATH:
    echo   C:\Windows\Microsoft.NET\Framework\v3.5
    echo   C:\Program Files (x86)\MSBuild\14.0\Bin
    echo.
    pause
    exit /b 1
)

REM Clean previous build
echo Cleaning previous build...
if exist bin\Debug rmdir /s /q bin\Debug
if exist bin\Release rmdir /s /q bin\Release
if exist obj rmdir /s /q obj
echo.

REM Build Debug configuration
echo Building Debug configuration...
msbuild ScanAgent.csproj /p:Configuration=Debug /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Debug build failed
    pause
    exit /b 1
)
echo.

REM Build Release configuration
echo Building Release configuration...
msbuild ScanAgent.csproj /p:Configuration=Release /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Release build failed
    pause
    exit /b 1
)
echo.

echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Debug build:   bin\Debug\ScanAgent.exe
echo Release build: bin\Release\ScanAgent.exe
echo.
echo To run the application:
echo   bin\Debug\ScanAgent.exe
echo   or
echo   bin\Release\ScanAgent.exe
echo.
pause

