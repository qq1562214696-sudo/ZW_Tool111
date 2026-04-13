@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo    开始 DLL 打包 - 编译模块并替换到 Assets
echo ========================================

set ROOT_DIR=%~dp0
set MAIN_PROJECT=%ROOT_DIR%ZW_Tool.csproj
set PACKAGES_DIR=%ROOT_DIR%项目\Packages
set ASSETS_MODULES_DIR=%ROOT_DIR%项目\Assets\Modules

:: 检查必要目录
if not exist "%PACKAGES_DIR%" (
    echo [错误] 找不到 Packages 目录: %PACKAGES_DIR%
    exit /b 1
)

if not exist "%ASSETS_MODULES_DIR%" mkdir "%ASSETS_MODULES_DIR%"

:: 还原整个解决方案（确保所有依赖就绪）
echo [1/3] 还原 NuGet 包...
dotnet restore "%ROOT_DIR%ZW_Tool.sln" --nologo
if %errorlevel% neq 0 (
    echo [错误] 还原失败
    exit /b %errorlevel%
)

:: 编译主项目（模块依赖主项目接口）
echo [2/3] 编译主项目...
dotnet build "%MAIN_PROJECT%" -c Release --no-restore -nologo -v q
if %errorlevel% neq 0 (
    echo [错误] 主项目编译失败
    exit /b %errorlevel%
)

:: 遍历 Packages 下每个子文件夹，编译模块并复制 DLL
echo [3/3] 编译并复制模块 DLL...
for /d %%d in ("%PACKAGES_DIR%\*") do (
    set "MODULE_DIR=%%d"
    set "MODULE_NAME=%%~nxd"
    set "PROJ_FILE=%%d\!MODULE_NAME!.csproj"

    if exist "!PROJ_FILE!" (
        echo   正在编译模块: !MODULE_NAME!
        dotnet build "!PROJ_FILE!" -c Release --no-restore -nologo -v q
        if !errorlevel! neq 0 (
            echo [错误] 模块 !MODULE_NAME! 编译失败
            exit /b !errorlevel!
        )

        set "OUTPUT_DLL=%%d\bin\Release\net10.0-windows\!MODULE_NAME!.dll"
        if exist "!OUTPUT_DLL!" (
            copy /Y "!OUTPUT_DLL!" "%ASSETS_MODULES_DIR%\" >nul
            echo   已复制 !MODULE_NAME!.dll 到 Assets\Modules
        ) else (
            echo   [警告] 未找到输出 DLL: !OUTPUT_DLL!
        )
    ) else (
        echo   跳过非模块文件夹: !MODULE_NAME!
    )
)

echo ========================================
echo    DLL 打包完成！
echo    模块 DLL 已存放于: %ASSETS_MODULES_DIR%
echo ========================================
endlocal
pause