@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ===========================================================================
rem  Xeonstrap - Windows build helper
rem
rem  By default this publishes the single file build: one Xeonstrap.exe in
rem  PublishedBuilds\Windows, the same options publish-all.ps1 uses for the
rem  Windows release. "solution" only compiles Xeonstrap.sln, which is quicker
rem  while working on code.
rem
rem  Usage:
rem    build.bat                      single file Xeonstrap.exe (Release)
rem    build.bat Debug                single file, Debug configuration
rem    build.bat solution             compile Xeonstrap.sln (Release)
rem    build.bat solution Debug       compile the solution in Debug
rem    build.bat publish              release flow: publish-all.ps1 -Only windows
rem    build.bat no-restore           build without restoring NuGet packages
rem    build.bat binlog               also write an MSBuild binary log
rem    build.bat "-p:Foo=Bar"         pass an extra MSBuild property (quote it)
rem    build.bat --help               this text
rem
rem  Needs the .NET 10 SDK, the external/wpfui submodule, and Rust for the
rem  native xeonimage and xeonpacer libraries. Without Rust the build still
rem  succeeds, the warning tells you what is missing.
rem
rem  Logs land in logs\. Run it from anywhere, paths resolve from this file.
rem ===========================================================================

set "XEONROOT=%~dp0"
if "%XEONROOT:~-1%"=="\" set "XEONROOT=%XEONROOT:~0,-1%"

rem Every variable below is named so that it cannot clash with an MSBuild
rem property. cmd puts its variables in the environment of the child process,
rem and MSBuild reads environment variables as properties without caring
rem about case, so a variable called OUTDIR would set OutDir for every project
rem in the build and all of them would write their output into the publish
rem folder instead of using their own.

set "MODE=single"
set "BUILDCONFIG=Release"
set "PUBLISHALL=0"
set "NORESTOREARG="
set "EXTRA="
set "BINLOG="
set "USAGE_RC=0"

:parse
if "%~1"=="" goto :args-done
set "ARG=%~1"
set "KNOWN=0"
if /i "%ARG%"=="single"     ( set "MODE=single"     & set "KNOWN=1" )
if /i "%ARG%"=="solution"   ( set "MODE=solution"   & set "KNOWN=1" )
if /i "%ARG%"=="publish"    ( set "PUBLISHALL=1"    & set "KNOWN=1" )
if /i "%ARG%"=="debug"      ( set "BUILDCONFIG=Debug"    & set "KNOWN=1" )
if /i "%ARG%"=="release"    ( set "BUILDCONFIG=Release"  & set "KNOWN=1" )
if /i "%ARG%"=="no-restore" ( set "NORESTOREARG= --no-restore" & set "KNOWN=1" )
if /i "%ARG%"=="restore"    ( set "NORESTOREARG="      & set "KNOWN=1" )
if /i "%ARG%"=="binlog"     ( set "BINLOG=1"        & set "KNOWN=1" )
if /i "!ARG:~0,3!"=="-p:"   ( set "EXTRA=!EXTRA! %ARG%" & set "KNOWN=1" )
if /i "!ARG:~0,3!"=="/p:"   ( set "EXTRA=!EXTRA! %ARG%" & set "KNOWN=1" )
if /i "%ARG%"=="--help"     goto :usage
if /i "%ARG%"=="-h"         goto :usage
if /i "%ARG%"=="help"       goto :usage
if /i "%ARG%"=="/?"         goto :usage
if "%KNOWN%"=="0" (
	echo Unknown option: %ARG%
	echo MSBuild properties contain "=", so wrap them in quotes: "-p:Name=Value"
	echo.
	set "USAGE_RC=2"
	goto :usage
)
shift
goto :parse
:args-done

pushd "%XEONROOT%" >nul 2>&1
if errorlevel 1 (
	echo [x] Cannot open "%XEONROOT%".
	endlocal
	exit /b 1
)

echo Xeonstrap build ^(%BUILDCONFIG%^)
echo   root: %XEONROOT%

rem --- toolchain -------------------------------------------------------------

where dotnet >nul 2>&1
if errorlevel 1 (
	echo [x] dotnet was not found on PATH.
	echo     Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
	goto :fail
)

set "SDKVERSION="
for /f "delims=" %%V in ('dotnet --version 2^>nul') do set "SDKVERSION=%%V"
if not defined SDKVERSION (
	echo [x] "dotnet --version" failed. Is the .NET SDK installed correctly?
	goto :fail
)
for /f "tokens=1 delims=." %%M in ("!SDKVERSION!") do set "SDKMAJOR=%%M"
echo   sdk:  !SDKVERSION!
if not "!SDKMAJOR!"=="10" (
	echo [x] The .NET 10 SDK is required ^(global.json wants 10.0.300 or newer^).
	echo     Found !SDKVERSION!. See https://dotnet.microsoft.com/download/dotnet/10.0
	goto :fail
)

rem --- required sources -------------------------------------------------------

if not exist "external\wpfui\src\Wpf.Ui\Wpf.Ui.csproj" (
	echo [x] external\wpfui is missing, so Xeonstrap cannot compile.
	echo     Run: git submodule update --init --recursive
	goto :fail
)

if not exist "src\Xeonstrap.App\Xeonstrap.csproj" (
	echo [x] src\Xeonstrap.App\Xeonstrap.csproj is missing.
	goto :fail
)

if not exist "external\LibreWPF\ProGPU.Wpf.dll" (
	echo [!] external\LibreWPF is missing. Windows builds work without it, Linux and macOS builds do not.
)

rem The native libraries come from cargo. Xeonstrap.csproj looks for cargo at
rem %USERPROFILE%\.cargo\bin\cargo.exe before falling back to PATH, and rustup
rem does not always put that folder on PATH, so check both here as well.
set "CARGO="
if exist "%USERPROFILE%\.cargo\bin\cargo.exe" set "CARGO=%USERPROFILE%\.cargo\bin\cargo.exe"
if not defined CARGO for /f "delims=" %%C in ('where cargo 2^>nul') do if not defined CARGO set "CARGO=%%C"

rem A Release build stops without xeonpacer.dll, so skip the frame pacer
rem instead of failing, loudly.
set "PACER="
if /i "%BUILDCONFIG%"=="Release" (
	if not defined CARGO (
		echo [!] Rust ^(cargo^) was not found, so xeonimage.dll and xeonpacer.dll cannot be built.
		echo     Building without frame generation and image helpers. Install Rust
		echo     ^(https://rustup.rs, then: rustup target add x86_64-pc-windows-msvc^) for a complete build.
		set "PACER= -p:XeonstrapAllowMissingPacer=true"
	)
)

rem --- log -------------------------------------------------------------------

if not exist logs md logs
set "BUILDSTAMP="
for /f "usebackq delims=" %%T in (`powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss" 2^>nul`) do set "BUILDSTAMP=%%T"
if not defined BUILDSTAMP set "BUILDSTAMP=latest"
set "BUILDLOG=logs\build-%BUILDSTAMP%.log"
if "%BINLOG%"=="1" set "BINLOGARG= -bl:logs\build-%BUILDSTAMP%.binlog"

set "PWSH=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%PWSH%" set "PWSH=powershell.exe"
echo   log:  !BUILDLOG!

rem MSBuild arguments shared by both modes: node reuse off, like the release
rem script, and MSBuild writes the log itself so the console stays live.
set "MSBARGS= -nr:false -fl -flp:logfile=%BUILDLOG%;verbosity=minimal"

if "%PUBLISHALL%"=="1" goto :publish-all
if /i "%MODE%"=="solution" goto :solution-build

rem --- single file ------------------------------------------------------------
rem Matches the Windows target in publish-all.ps1, so the result is the same
rem single file the release ships. TreatWarningsAsErrors, ContinuousIntegrationBuild
rem and -clp:ErrorsOnly are left to publish-all.ps1, they are for the release run.

:single-file
rem Absolute paths on purpose: a relative -o becomes a global property that
rem every referenced project resolves against its own folder, which scatters
rem PublishedBuilds\Windows copies all over the tree.
set "OUTFOLDER=%XEONROOT%\PublishedBuilds\Windows"
set "PUBARTIFACTS=%XEONROOT%\artifacts\publish\windows"
set "APPEXE=%OUTFOLDER%\Xeonstrap.exe"
echo   mode: single file Xeonstrap.exe
echo   out:  PublishedBuilds\Windows
echo.
echo Publishing ^(%BUILDCONFIG%, win-x64^), this takes a minute or two...
echo.

rem Wipe the folder first, like Reset-OutputDirectory in publish-all.ps1, so
rem no stale file from an earlier build survives.
if exist "%OUTFOLDER%" rd /s /q "%OUTFOLDER%" >nul 2>&1

dotnet publish "src\Xeonstrap.App\Xeonstrap.csproj" -c %BUILDCONFIG% -r win-x64 -o "%OUTFOLDER%" --artifacts-path "%PUBARTIFACTS%" --self-contained false -p:PublishProfile=FolderProfile -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=false -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false --nologo -v minimal%NORESTOREARG%%EXTRA%%PACER%%BINLOGARG%%MSBARGS%
set "DOTNETRC=!ERRORLEVEL!"
if not "!DOTNETRC!"=="0" (
	echo.
	echo [x] Publish failed ^(exit code !DOTNETRC!^). Log: !BUILDLOG!
	goto :fail
)

if not exist "%APPEXE%" (
	echo.
	echo [x] The build did not produce %APPEXE%. Log: !BUILDLOG!
	goto :fail
)
for %%A in ("%APPEXE%") do set /a "SIZEMB=%%~zA/1048576"
echo.
echo Built %APPEXE% ^(!SIZEMB! MB, %BUILDCONFIG%, win-x64^)
if defined PACER echo   note: without Rust, so no frame generation and no image helpers

rem The release ships one file, so mention it if anything else landed there.
set "FILEFOLDS=0"
for /f %%N in ('dir /b /a-d "%OUTFOLDER%" 2^>nul ^| find /c /v ""') do set "FILEFOLDS=%%N"
if not "!FILEFOLDS!"=="1" (
	echo   note: %OUTFOLDER% holds !FILEFOLDS! files. The build itself is
	echo   Xeonstrap.exe, the other files are only needed next to it.
) else (
	echo   PublishedBuilds\Windows holds Xeonstrap.exe and nothing else.
)
popd
call :pause-if-double-click
endlocal
exit /b 0

rem --- solution ---------------------------------------------------------------

:solution-build
echo   mode: Xeonstrap.sln
echo.
echo Compiling the solution ^(%BUILDCONFIG%^), this can take a few minutes...
echo.

dotnet build "Xeonstrap.sln" -c %BUILDCONFIG% --nologo -v minimal%NORESTOREARG%%EXTRA%%PACER%%BINLOGARG%%MSBARGS%
set "DOTNETRC=!ERRORLEVEL!"
if not "!DOTNETRC!"=="0" (
	echo.
	echo [x] Build failed ^(exit code !DOTNETRC!^). Log: !BUILDLOG!
	goto :fail
)

set "APPEXE=src\Xeonstrap.App\bin\%BUILDCONFIG%\net10.0-windows\win-x64\Xeonstrap.exe"
echo.
echo Build succeeded ^(%BUILDCONFIG%^).
if exist "%APPEXE%" (
	echo   app: %APPEXE%
) else (
	echo   app: not found at %APPEXE%, check the log
)
popd
call :pause-if-double-click
endlocal
exit /b 0

rem --- full release flow ------------------------------------------------------

:publish-all
if /i "%BUILDCONFIG%"=="Release" if defined PACER (
	echo [!] publish-all.ps1 builds the frame pacer in Release and stops when
	echo     Rust is missing. Install Rust first for this one.
)
echo   mode: publish-all.ps1 -Only windows
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%XEONROOT%\publish-all.ps1" -Only windows -Configuration %BUILDCONFIG% -NoPause
if errorlevel 1 (
	echo.
	echo [x] publish-all.ps1 failed.
	goto :fail
)
echo.
if exist "PublishedBuilds\Windows\Xeonstrap.exe" (
	echo Built PublishedBuilds\Windows\Xeonstrap.exe
) else (
	echo [x] PublishedBuilds\Windows\Xeonstrap.exe is missing.
	goto :fail
)
popd
call :pause-if-double-click
endlocal
exit /b 0

:fail
popd
echo.
call :pause-if-double-click
endlocal
exit /b 1

:usage
echo Xeonstrap build helper
echo.
echo   build.bat [options]
echo.
echo   No option publishes the single file build: one Xeonstrap.exe in
echo   PublishedBuilds\Windows, Release, win-x64.
echo.
echo   solution     compile Xeonstrap.sln instead of publishing
echo   publish      run publish-all.ps1 -Only windows ^(full release flow^)
echo   debug        use the Debug configuration
echo   release      use the Release configuration ^(default^)
echo   no-restore   pass --no-restore to dotnet
echo   binlog       also write logs\build-^<timestamp^>.binlog
echo   "-p:Name=Value"  pass an extra MSBuild property, quotes included
echo   --help, -h, /?  show this text
echo.
echo   Logs: logs\build-^<timestamp^>.log
echo.
call :pause-if-double-click
endlocal & exit /b %USAGE_RC%

:pause-if-double-click
rem Keep the window open when the file is double-clicked in Explorer.
if defined CI goto :eof
echo "%CMDCMDLINE%" | find /i "%~nx0" >nul
if not errorlevel 1 (
	echo.
	pause
)
goto :eof
