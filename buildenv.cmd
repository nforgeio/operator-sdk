@echo off
REM Configures the environment variables required to build Neon.Operator projects.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

echo ====================================================
echo * Neon Operator SDK Build Environment Configurator *
echo ====================================================

REM Default NF_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set NO_ROOT=%~dp0 
set NO_ROOT=%NO_ROOT:~0,-2%

if not [%1]==[] set NO_ROOT=%1

if exist %NO_ROOT%\operator-sdk.sln goto goodPath
echo The [%NO_ROOT%\operator-sdk.sln] file does not exist.  Please pass the path
echo to the OperatorSDK solution folder.
goto done

:goodPath 

REM Get on with configuration.

echo.
echo Configuring...
echo.

REM Configure the environment variables.

set NO_BUILD=%NO_ROOT%\Build
set NO_TEST=%NO_ROOT%\Test

REM Temporarily add [%NF_ROOT%\neonSDK\ToolBin] to the PATH so
REM we'll be able to use things like [pathtool].
REM
REM NOTE: This assumes that NeonSDK is configured first.

set PATH=%PATH%;%NF_ROOT%\neonSDK\ToolBin

REM Persist the environment variables.

setx NO_ROOT "%NO_ROOT%" /M     > nul
setx NO_BUILD "%NO_BUILD%" /M   > nul
setx NF_TEST "%NF_TEST%" /M     > nul

:done
echo.
echo ============================================================================================
echo * Be sure to close and reopen Visual Studio and any command windows to pick up the changes *
echo ============================================================================================
