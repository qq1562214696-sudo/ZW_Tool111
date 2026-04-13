@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo    开始 SLN 打包 - 生成主程序 EXE
echo ========================================

set ROOT_DIR=%~dp0

:: 先调用 DLL 打包，确保模块 DLL 最新
echo [步骤 1/3] 更新模块 DLL...
call "%ROOT_DIR%DLL打包.bat"
if %errorlevel% neq 0 (
    echo [错误] DLL 打包失败，终止 SLN 打包
    exit /b %errorlevel%
)

set PUBLISH_OUTPUT=%ROOT_DIR%bin\Release\ZW_Tool_Publish
set MAIN_PROJECT=%ROOT_DIR%ZW_Tool.csproj
set ASSETS_MODULES_DIR=%ROOT_DIR%项目\Assets\Modules
set PLUGINS_OUTPUT=%PUBLISH_OUTPUT%\Plugins

:: 发布主项目（生成单文件 EXE）
echo [步骤 2/3] 发布主项目...
dotnet publish "%MAIN_PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PUBLISH_OUTPUT%" ^
    --nologo

if %errorlevel% neq 0 (
    echo [错误] 主项目发布失败
    exit /b %errorlevel%
)

:: 将模块 DLL 复制到发布目录的 Plugins 文件夹（供动态加载）
echo [步骤 3/3] 复制模块 DLL 到 Plugins 目录...
if exist "%ASSETS_MODULES_DIR%" (
    if not exist "%PLUGINS_OUTPUT%" mkdir "%PLUGINS_OUTPUT%"
    copy /Y "%ASSETS_MODULES_DIR%\*.dll" "%PLUGINS_OUTPUT%\" >nul
    echo   已复制模块 DLL 到 %PLUGINS_OUTPUT%
) else (
    echo   [警告] 未找到模块 DLL 目录，请确认 DLL 打包是否成功
)

echo ========================================
echo    SLN 打包完成！
echo    可执行文件位置: %PUBLISH_OUTPUT%\ZW_Tool.exe
echo ========================================
pause