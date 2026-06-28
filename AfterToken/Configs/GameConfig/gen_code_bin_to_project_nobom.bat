ho off
setlocal enabledelayedexpansion

:: 切换到脚本所在目录
CD /d %~dp0

:: =====================================================
:: 路径配置（基于 D:\U3D_project\AfterToken\Tools\Luban）
:: =====================================================
:: 脚本所在: AfterToken\AfterToken\Configs\GameConfig
:: 项目代码根目录: AfterToken\AfterToken
:: 仓库根目录: AfterToken
:: Luban 工具: AfterToken\Tools\Luban\Luban.exe
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\..
set REPO_ROOT=%PROJECT_ROOT%..
set LUBAN_EXE=%REPO_ROOT%\Tools\Luban\Luban.exe

set CONF_ROOT=%SCRIPT_DIR%
set CODE_OUT=%PROJECT_ROOT%\Assets\GameScripts\HotFix\GameProto
set DATA_OUT=%PROJECT_ROOT%\Assets\AssetRaw\Configs\json

echo [Luban] 开始生成配置表...
echo [Luban] Luban: %LUBAN_EXE%
echo [Luban] 代码输出: %CODE_OUT%
echo [Luban] 数据输出: %DATA_OUT%

:: 检查 Luban.exe
if not exist "%LUBAN_EXE%" (
    echo [Luban] 错误: 找不到 %LUBAN_EXE%
    echo [Luban] 请确认 Luban 已解压到 D:\U3D_project\AfterToken\Tools\Luban
    if not defined AI_MODE pause
    exit /b 1
)

:: 创建输出目录
if not exist "%CODE_OUT%" mkdir "%CODE_OUT%"
if not exist "%DATA_OUT%" mkdir "%DATA_OUT%"

:: 复制桥接文件（JSON 加载器）
echo [Luban] 复制桥接文件...
copy /y "%CONF_ROOT%\CustomTemplate\ConfigSystem.cs" "%CODE_OUT%\ConfigSystem.cs" >nul
copy /y "%CONF_ROOT%\CustomTemplate\ExternalTypeUtil.cs" "%CODE_OUT%\ExternalTypeUtil.cs" >nul

:: 生成客户端代码与 JSON 数据
echo [Luban] 生成代码与数据...
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
    echo [Luban] 生成失败，错误码: %ERRORLEVEL%
    if not defined AI_MODE pause
    exit /b %ERRORLEVEL%
)

echo [Luban] 生成成功
echo [Luban] 代码: %CODE_OUT%\GameConfig
echo [Luban] 数据: %DATA_OUT%

if not defined AI_MODE pause
