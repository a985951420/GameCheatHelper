@echo off
chcp 65001 >nul
echo ========================================
echo   GameCheatHelper 发布脚本
echo ========================================
echo.

:: 设置变量
set "PROJECT_DIR=%~dp0"
set "PUBLISH_DIR=%PROJECT_DIR%publish"
set "PROJECT_FILE=GameCheatHelper.csproj"

:: 打印当前目录
echo [1/4] 当前目录: %PROJECT_DIR%
echo.

:: 清理旧的发布文件
echo [2/4] 清理旧的发布文件...
if exist "%PUBLISH_DIR%\*.exe" (
    del /Q "%PUBLISH_DIR%\*.exe"
    echo       已删除旧的 EXE 文件
)
if exist "%PUBLISH_DIR%\*.pdb" (
    del /Q "%PUBLISH_DIR%\*.pdb"
    echo       已删除旧的 PDB 文件
)
echo.

:: 执行发布命令
echo [3/4] 开始发布编译...
echo       配置: Release
echo       平台: win-x64
echo       模式: 单文件自包含
echo.
dotnet publish "%PROJECT_FILE%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ 发布失败！错误代码: %ERRORLEVEL%
    echo    请检查编译错误信息
    pause
    exit /b %ERRORLEVEL%
)
echo.

:: 复制额外文件到发布目录
echo [4/4] 复制额外文件...

:: 复制配置文件
if exist "NLog.config" (
    copy /Y "NLog.config" "%PUBLISH_DIR%\" >nul
    echo       已复制 NLog.config
)

:: 复制数据目录
if exist "Data" (
    if not exist "%PUBLISH_DIR%\Data" mkdir "%PUBLISH_DIR%\Data"
    xcopy /Y /E /I "Data" "%PUBLISH_DIR%\Data" >nul
    echo       已复制 Data 目录
)

:: 生成快速开始说明文件
echo 正在生成 README.txt...
(
echo GameCheatHelper - 游戏秘籍助手
echo =================================
echo.
echo 快速开始：
echo 1. 双击 GameCheatHelper.exe 启动程序
echo 2. 启动游戏（魔兽争霸3 或 星际争霸1）
echo 3. 使用热键快速输入秘籍（默认 F1-F5）
echo.
echo 支持的游戏：
echo - 魔兽争霸3（Warcraft III）
echo - 星际争霸1（StarCraft）
echo.
echo 默认热键绑定：
echo F1 - 获得资源
echo F2 - 显示全图
echo F3 - 无敌模式
echo F4 - 无限能量/魔法
echo F5 - 快速建造
echo.
echo 技术支持：
echo - 项目主页: https://github.com/a985951420/GameCheatHelper
echo - 日志文件: logs\ 目录
echo - 配置文件: Data\ 目录
echo.
echo 版本: 1.0.0
echo 开发者: Time
echo Copyright © 2026
) > "%PUBLISH_DIR%\README.txt"
echo       已生成 README.txt

echo.
echo ========================================
echo ✅ 发布完成！
echo ========================================
echo.
echo 发布文件位置: %PUBLISH_DIR%
echo.
echo 主程序: GameCheatHelper.exe
dir "%PUBLISH_DIR%\GameCheatHelper.exe" | find "GameCheatHelper.exe"
echo.
pause
