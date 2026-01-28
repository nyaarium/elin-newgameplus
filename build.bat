@echo off
setlocal

echo Building...

REM Build the project
dotnet build -c Release 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Error code: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
