@echo off
REM ======================================
REM Unity Build Automation Script for Windows
REM ======================================

REM -- Set the path to your Unity Editor executable.
REM    Make sure to update the version and path as necessary.
set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\2022.3.49f1\Editor\Unity.exe"

REM -- Set the path to your Unity project.
set PROJECT_PATH="C:\Unity Projects\Oddinary-Farm"

REM -- Specify the fully qualified name of the static method that performs the build.
set BUILD_METHOD=BuildScript.PerformBuildWin64

REM -- Specify the target build platform.
set BUILD_TARGET=StandaloneWindows64

REM -- Specify the output path for the build (this can also be defined within your build script).
REM    In this example, the build script itself defines the output path.
REM    You can pass additional parameters if needed.

REM -- Set the log file to capture Unity's output.
set LOG_FILE="C:\Unity Projects\Oddinary-Farm\Logs\build.log"

echo Starting Unity build...
%UNITY_EXE% -batchmode -nographics -quit -projectPath %PROJECT_PATH% -executeMethod %BUILD_METHOD% -buildTarget %BUILD_TARGET% -logFile %LOG_FILE%

REM Check for errors during the build process.
if errorlevel 1 (
    echo Build failed. Check the log file at %LOG_FILE% for details.
    pause
    exit /b 1
) else (
    echo Build succeeded!
    pause
)
