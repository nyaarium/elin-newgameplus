@echo off
setlocal

echo Building...

set DEPLOY_PATH=S:\Steam\steamapps\common\Elin

dotnet build -c Release -p:DeployPath=%DEPLOY_PATH% 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Error code: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
