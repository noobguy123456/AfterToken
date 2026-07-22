@echo off
setlocal enabledelayedexpansion

:: Switch to script directory
CD /d %~dp0

:: =====================================================
:: Path configuration (based on D:\U3D_project\AfterToken\Tools\Luban)
:: =====================================================
:: Script:    AfterToken\AfterToken\Configs\GameConfig
:: Code root: AfterToken\AfterToken
:: Repo root: AfterToken
:: Luban:     AfterToken\Tools\Luban\Luban.exe
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\..
set REPO_ROOT=%PROJECT_ROOT%..
set LUBAN_EXE=%SCRIPT_DIR%\..\..\..\Tools\Luban\Luban.exe

set CONF_ROOT=%SCRIPT_DIR%
set CODE_OUT=%PROJECT_ROOT%\Assets\GameScripts\HotFix\GameProto
set DATA_OUT=%PROJECT_ROOT%\Assets\AssetRaw\Configs\json

echo [Luban] Start generating configs...
echo [Luban] Luban: %LUBAN_EXE%
echo [Luban] Code output: %CODE_OUT%
echo [Luban] Data output: %DATA_OUT%

:: Check Luban.exe
if not exist "%LUBAN_EXE%" (
    echo [Luban] Error: %LUBAN_EXE% not found
    echo [Luban] Please extract Luban to D:\U3D_project\AfterToken\Tools\Luban
    if not defined AI_MODE pause
    exit /b 1
)

:: Create output directories
if not exist "%CODE_OUT%" mkdir "%CODE_OUT%"
if not exist "%DATA_OUT%" mkdir "%DATA_OUT%"

:: Copy bridge files (JSON loader)
echo [Luban] Copying bridge files...
copy /y "%CONF_ROOT%\CustomTemplate\ConfigSystem.cs" "%CODE_OUT%\ConfigSystem.cs" >nul
copy /y "%CONF_ROOT%\CustomTemplate\ExternalTypeUtil.cs" "%CODE_OUT%\ExternalTypeUtil.cs" >nul

:: Generate client code and JSON data
echo [Luban] Generating code and data...
"%LUBAN_EXE%" ^
    -t client ^
    -c cs-newtonsoft-json ^
    -d json ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir="%CODE_OUT%\GameConfig" ^
    -x outputDataDir="%DATA_OUT%" ^
    -x json.indent=true

if %ERRORLEVEL% neq 0 (
    echo [Luban] Generation failed, error code: %ERRORLEVEL%
    if not defined AI_MODE pause
    exit /b %ERRORLEVEL%
)

echo [Luban] Generation succeeded
echo [Luban] Code: %CODE_OUT%\GameConfig
echo [Luban] Data: %DATA_OUT%

if not defined AI_MODE pause
