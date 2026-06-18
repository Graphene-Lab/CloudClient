@echo off
:: Get the directory of the current script
set "SCRIPT_DIR=%~dp0"

:: Check if the script is running with administrative privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

setlocal

:: Define the target installation folder
set "DEST=%ProgramFiles%\Cloud"

:: Create the destination folder if it doesn't exist
if not exist "%DEST%" (
    mkdir "%DEST%"
)

:: Copy all files from the current directory to the destination
xcopy "%SCRIPT_DIR%*" "%DEST%\" /E /I /H /Y

:: Check if dotnet CLI is available
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    :: dotnet is available, check runtimes
    dotnet --list-runtimes | findstr /i "Microsoft.WindowsDesktop.App 10." >nul
    if %errorlevel% equ 0 (
        set "desktop_installed=1"
    )
    dotnet --list-runtimes | findstr /i "Microsoft.AspNetCore.App 10." >nul
    if %errorlevel% equ 0 (
        set "aspnet_installed=1"
    )
) else (
    echo dotnet CLI not found. Assuming no runtimes are installed.
)

:: Install Desktop Runtime if not installed
if not defined desktop_installed (
    echo Installing Windows Desktop Runtime 10...
    if not exist "%SCRIPT_DIR%desktop-runtime.exe" (
        echo Downloading Windows Desktop Runtime 10 installer...
        curl -L -o "%SCRIPT_DIR%desktop-runtime.exe" https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe
    )
    start /wait "" "%SCRIPT_DIR%desktop-runtime.exe" /install /quiet /norestart
)

:: Install ASP.NET Core Runtime if not installed
if not defined aspnet_installed (
    echo Installing ASP.NET Core Runtime 10...
    if not exist "%SCRIPT_DIR%aspnet-runtime.exe" (
        echo Downloading ASP.NET Core Runtime 10 installer...
        curl -L -o "%SCRIPT_DIR%aspnet-runtime.exe" https://aka.ms/dotnet/10.0/aspnetcore-runtime-win-x64.exe
    )
    start /wait "" "%SCRIPT_DIR%aspnet-runtime.exe" /install /quiet /norestart
)

:: Launch the main application
pushd "%DEST%"
start "" "Cloud.exe"
popd
