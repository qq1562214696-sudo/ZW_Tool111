@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo     发布 ZW_Tool（单文件 EXE + Assets 真实文件夹）
echo ========================================

set ROOT_DIR=%~dp0
set PUBLISH_OUTPUT=%ROOT_DIR%发布文件夹
set MAIN_PROJECT=%ROOT_DIR%ZW_Tool.csproj

:: 清理旧目录
if exist "%PUBLISH_OUTPUT%" (
    echo 正在清理旧发布目录...
    rmdir /s /q "%PUBLISH_OUTPUT%"
)

echo 正在发布单文件 EXE...
dotnet publish "%MAIN_PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "%PUBLISH_OUTPUT%" ^
    --nologo

if %errorlevel% neq 0 (
    echo [错误] 发布失败！
    pause
    exit /b %errorlevel%
)

:: 关键：强制把 Assets 文件夹复制到真实磁盘路径（确保模块 DLL 加载正常）
echo 正在复制 Assets 文件夹（保证模块加载）...
if exist "%ROOT_DIR%Assets\" (
    rmdir /s /q "%PUBLISH_OUTPUT%\Assets" 2>nul
    xcopy /E /Y /I "%ROOT_DIR%Assets" "%PUBLISH_OUTPUT%\Assets" >nul
    echo Assets 已复制。
) else if exist "%ROOT_DIR%项目\Assets\" (
    rmdir /s /q "%PUBLISH_OUTPUT%\Assets" 2>nul
    xcopy /E /Y /I "%ROOT_DIR%项目\Assets" "%PUBLISH_OUTPUT%\Assets" >nul
    echo 项目\Assets 已复制。
) else (
    echo [警告] 未找到 Assets 或 项目\Assets 文件夹。
)

echo ========================================
echo 发布完成！
echo EXE 位置: %PUBLISH_OUTPUT%\ZW_Tool.exe
echo Assets 位置: %PUBLISH_OUTPUT%\Assets
echo 
echo 发布目录应该只有 EXE + Assets 文件夹。
echo 请测试模块加载功能（示例工具等）。
echo ========================================
pause
endlocal