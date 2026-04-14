@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

set "PROJECT_DIR=%~dp0"
set "PACKAGES_DIR=%PROJECT_DIR%Packages"
set "ASSETS_DIR=%PROJECT_DIR%Assets"

if not exist "%ASSETS_DIR%" mkdir "%ASSETS_DIR%"

echo 正在扫描 Packages 目录下的所有 csproj 文件...

for /R "%PACKAGES_DIR%" %%F in (*.csproj) do (
    echo -------------------------------
    echo 构建项目: %%F
    
    dotnet build "%%F" -c Release
    
    if errorlevel 1 (
        echo 构建失败: %%F ，跳过 DLL 复制
    ) else (
        set "PROJECT_PATH=%%~dpF"
        set "PROJECT_NAME=%%~nF"
        set "DLL_FOUND="
        
        for /f "delims=" %%D in ('dir /s /b "!PROJECT_PATH!bin\Release\net10.0-windows\!PROJECT_NAME!.dll" 2^>nul') do (
            echo 找到 DLL: %%D
            copy /Y "%%D" "%ASSETS_DIR%\" >nul
            set "DLL_FOUND=1"
        )
        
        if not defined DLL_FOUND (
            echo DLL 未找到: !PROJECT_NAME!.dll
        )
    )
)

echo -------------------------------
echo 清理 Packages 下的 bin 和 obj 目录...
for /R "%PACKAGES_DIR%" %%D in (bin obj) do (
    if exist "%%D" (
        echo 删除目录: %%D
        rmdir /S /Q "%%D" 2>nul
    )
)

echo 完成！
pause