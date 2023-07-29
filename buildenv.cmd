@echo off
REM Configures the environment variables required to build Neon.Operator projects.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

echo ==========================================
echo * Neon.Operator Build Environment Configurator *
echo ==========================================

REM Default NF_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set NO_ROOT=%~dp0 
set NO_ROOT=%NO_ROOT:~0,-2%

if not [%1]==[] set NO_ROOT=%1

if exist %NO_ROOT%\Neon.Operator.sln goto goodPath
echo The [%NO_ROOT%\Neon.Operator.sln] file does not exist.  Please pass the path
echo to the Neon.Operator solution folder.
goto done

:goodPath 

REM Set NF_REPOS to the parent directory holding the NEONFORGE repositories.

pushd "%NF_ROOT%\.."
set NF_REPOS=%cd%
popd 

REM We need to capture the user's GitHub username and email address:

echo.
set /p GITHUB_USERNAME="Enter your GitHub username: "

echo.
set /p GITHUB_EMAIL="Enter the email to be included in GitHub commits: "

REM Ask the developer if they're a maintainer and set NF_MAINTAINER if they say yes.

:maintainerPrompt

echo.
set /P "IS_MAINTAINER=Are you a NEONFORGE maintainer? (y/n): "

if "%IS_MAINTAINER%"=="y" (
    set NF_MAINTAINER=1
) else if "%IS_MAINTAINER%"=="Y" (
    set NF_MAINTAINER=1
) else if "%IS_MAINTAINER%"=="n" (
    set NF_MAINTAINER=
) else if "%IS_MAINTAINER%"=="N" (
    set NF_MAINTAINER=
) else (
    echo.
    echo "*** ERROR: You must answer with: Y or N."
    echo.
    goto maintainerPrompt
)

REM Ask maintainers for their NEONFORGE Office 365 username.

if "%NF_MAINTAINER%"=="1" (
    echo.
    set /p NC_USER="Enter your NEONFORGE Office 365 username: "
    setx NC_USER "%NC_USER%" /M > nul
)

REM Ask the developer if they're using preview Visual Studio.

:previewVSPrompt

echo.
set /P "IS_VS_PREVIEW=Are you a using a PREVIEW version of Visual Studio? (y/n): "

if "%IS_VS_PREVIEW%"=="y" (
    set IS_VS_PREVIEW=1
) else if "%IS_VS_PREVIEW%"=="Y" (
    set IS_VS_PREVIEW=1
) else if "%IS_VS_PREVIEW%"=="n" (
    set IS_VS_PREVIEW=0
) else if "%IS_VS_PREVIEW%"=="N" (
    set IS_VS_PREVIEW=0
) else (
    echo.
    echo "*** ERROR: You must answer with: Y or N."
    echo.
    goto previewVSPrompt
)

if "%IS_VS_PREVIEW%"=="1" (
    set VS_EDITION=Preview
) else (
    set VS_EDITION=Community
)

REM Get on with configuration.

echo.
echo Configuring...
echo.

REM Configure the environment variables.

set NO_BUILD=%NO_ROOT%\Build
set NO_TEST=%NO_ROOT%\Test
set NO_TEMP=C:\Temp
set DOTNETPATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set MSBUILDPATH=C:\Program Files\Microsoft Visual Studio\2022\%VS_EDITION%\Msbuild\Current\Bin\MSBuild.exe

REM Persist the environment variables.

setx GITHUB_USERNAME "%GITHUB_USERNAME%" /M       > nul
setx GITHUB_EMAIL "%GITHUB_EMAIL%" /M             > nul
setx NF_MAINTAINER "%NF_MAINTAINER%" /M           > nul              
setx NF_REPOS "%NF_REPOS%" /M                     > nul
setx NO_ROOT "%NO_ROOT%" /M                       > nul
setx NO_BUILD "%NO_BUILD%" /M                     > nul
setx NF_TEST "%NF_TEST%" /M                       > nul

if "%NF_MAINTAINER%"=="1" (
    setx NC_USER "%NC_USER%" /M > nul
)

setx DOTNETPATH "%DOTNETPATH%" /M                             > nul
setx MSBUILDPATH "%MSBUILDPATH%" /M                           > nul
setx DOTNET_CLI_TELEMETRY_OPTOUT 1 /M                         > nul
setx DEV_WORKSTATION 1 /M                                     > nul
setx OPENSSL_CONF "%NF_ROOT%\External\OpenSSL\openssl.cnf" /M > nul

REM Make sure required folders exist.

if not exist "%NO_TEMP%" mkdir "%NF_TEMP%"

REM Perform additional implementation via Powershell.

pwsh -File "%NO_ROOT%\buildenv.ps1"

:done
echo.
echo ============================================================================================
echo * Be sure to close and reopen Visual Studio and any command windows to pick up the changes *
echo ============================================================================================
pause
