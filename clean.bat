@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ===========================================================================
rem  Xeonstrap - remove build output, logs, caches and leftovers
rem
rem  Usage:
rem    clean.bat             build output, logs and caches
rem    clean.bat all         also Visual Studio state and personal settings
rem    clean.bat --dry-run   list what would be removed, delete nothing
rem    clean.bat --yes       do not ask before "all"
rem    clean.bat --help      this text
rem
rem  Removed:
rem    bin, obj, bin-*, obj-*                  .NET build output and caches
rem    logs, *.log, *.binlog, *.trx            logs from builds and tests
rem    PublishedBuilds, artifacts, TestResults publish and test output
rem    android\build, android\.gradle, ...     Android build output
rem    native\*\target, android\rust\target    cargo output
rem    result, result-*                        Nix build links
rem    *.tmp, *~, *.orig, *.rej                stray files
rem  Removed by "all":
rem    .vs, *.user, *.suo, *.userosscache, *.sln.docstates
rem
rem  Sources, submodules, git data and Android signing keys are never touched.
rem  Close Visual Studio and any running build first, or files stay locked.
rem ===========================================================================

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "MODE=standard"
set "DRY=0"
set "ASSUME=0"
set "USAGE_RC=0"

:parse
if "%~1"=="" goto :args-done
set "ARG=%~1"
set "KNOWN=0"
if /i "%ARG%"=="all"       ( set "MODE=all"      & set "KNOWN=1" )
if /i "%ARG%"=="standard"  ( set "MODE=standard" & set "KNOWN=1" )
if /i "%ARG%"=="--dry-run" ( set "DRY=1"         & set "KNOWN=1" )
if /i "%ARG%"=="-n"        ( set "DRY=1"         & set "KNOWN=1" )
if /i "%ARG%"=="--yes"     ( set "ASSUME=1"      & set "KNOWN=1" )
if /i "%ARG%"=="-y"        ( set "ASSUME=1"      & set "KNOWN=1" )
if /i "%ARG%"=="--help"    goto :usage
if /i "%ARG%"=="-h"        goto :usage
if /i "%ARG%"=="help"      goto :usage
if /i "%ARG%"=="/?"        goto :usage
if "%KNOWN%"=="0" (
	echo Unknown option: %ARG%
	echo.
	set "USAGE_RC=2"
	goto :usage
)
shift
goto :parse
:args-done

pushd "%ROOT%" >nul 2>&1
if errorlevel 1 (
	echo [x] Cannot open "%ROOT%".
	endlocal
	exit /b 1
)

set /a DIRS=0
set /a FILES=0

if /i "%MODE%"=="all" if "%DRY%"=="0" if "%ASSUME%"=="0" (
	echo "clean all" also removes Visual Studio state ^(.vs^) and personal settings.
	set "ANS="
	set /p "ANS=Continue? [y/N] "
	if /i not "!ANS!"=="y" (
		echo Cancelled, nothing was removed.
		popd
		endlocal
		exit /b 1
	)
)

echo Cleaning %ROOT%
echo.

echo   .NET build output and caches
call :nuke_name bin
call :nuke_name obj
call :nuke_prefix bin-
call :nuke_prefix obj-

echo   logs and publish output
call :nuke_dir logs
call :nuke_dir PublishedBuilds
call :nuke_dir artifacts
call :nuke_dir TestResults
call :nuke_dir result
for /f "delims=" %%D in ('dir /b /ad "result-*" 2^>nul') do call :nuke_dir "%%~fD"
call :nuke_files *.log
call :nuke_files *.binlog
call :nuke_files *.trx
call :nuke_files *.coverage

echo   Android build output
call :nuke_dir android\build
call :nuke_dir android\app\build
call :nuke_dir android\.gradle
call :nuke_dir android\.cxx
call :nuke_dir android\.kotlin
call :nuke_dir android\.idea
call :nuke_file android\local.properties

echo   Rust build output
call :nuke_name target native
call :nuke_name target android\rust
call :nuke_name target src\VirtualDisplay
call :nuke_name target external

echo   stray files
call :nuke_files *.tmp
call :nuke_files *~
call :nuke_files *.orig
call :nuke_files *.rej

if /i "%MODE%"=="all" (
	echo   Visual Studio state and personal settings
	call :nuke_dir .vs
	call :nuke_files *.user
	call :nuke_files *.suo
	call :nuke_files *.userosscache
	call :nuke_files *.sln.docstates
	call :nuke_files *.rsuser
	call :nuke_files *.psess
	call :nuke_files *.vsp
	call :nuke_files UpgradeLog*.XML
	call :nuke_files UpgradeLog*.htm

)

echo.
if "%DRY%"=="1" (
	echo Dry run, nothing was deleted. %DIRS% directories and %FILES% files would be removed.
) else (
	echo Removed %DIRS% directories and %FILES% files.
)
if not "%MODE%"=="all" echo Run "clean.bat all" to also remove .vs and personal settings.

popd
call :pause-if-double-click
endlocal
exit /b 0

:usage
echo Xeonstrap clean
echo.
echo   clean.bat [options]
echo.
echo   all          also remove .vs and personal settings ^(*.user, *.suo^)
echo   --dry-run    list what would be removed, delete nothing ^(also -n^)
echo   --yes        do not ask before "all" ^(also -y^)
echo   --help, -h, /?  show this text
echo.
echo   Build output, logs, caches and publish output are always removed.
echo.
call :pause-if-double-click
endlocal & exit /b %USAGE_RC%

rem --- helpers ----------------------------------------------------------------
rem  :nuke_dir   path        remove one directory
rem  :nuke_file  path        remove one file
rem  :nuke_name  name [base]      remove every directory below base (default: the
rem                                whole repository) whose name is exactly name
rem  :nuke_prefix prefix [base]   same, but any name starting with prefix
rem  :nuke_files pattern     remove every file matching this pattern, anywhere
rem  .git is never touched.

:nuke_dir
if not exist "%~1\" goto :eof
set "DFULL=%~f1"
set "DREL=!DFULL:%ROOT%\=!"
if /i not "!DFULL:\.git\=!"=="!DFULL!" (
	echo     [skip] !DREL!\ is inside .git
	goto :eof
)
if "%DRY%"=="1" (
	echo     [dry] !DREL!\
	set /a DIRS+=1
	goto :eof
)
rd /s /q "%~1" 2>nul
if exist "%~1\" (
	echo     [!] !DREL!\ is locked, close Visual Studio and any running build
) else (
	echo     - !DREL!\
	set /a DIRS+=1
)
goto :eof

:nuke_file
if not exist "%~f1" goto :eof
set "FFULL=%~f1"
set "FREL=!FFULL:%ROOT%\=!"
if "%DRY%"=="1" (
	echo     [dry] !FREL!
	set /a FILES+=1
	goto :eof
)
del /f /q "%~1" >nul 2>&1
if exist "%~f1" (
	echo     [!] !FREL! is locked
) else (
	echo     - !FREL!
	set /a FILES+=1
)
goto :eof

:nuke_name
call :nuke_scan "%~1" "" "%~2"
goto :eof

:nuke_prefix
call :nuke_scan "%~1" "*" "%~2"
goto :eof

:nuke_scan
rem  %1 = directory name, %2 = "*" to match every name starting with %1,
rem  %3 = subtree to search, default is the repository root
set "PAT=%~1"
set "WILD=%~2"
set "BASE=%~3"
if "%BASE%"=="" set "BASE=."
if not exist "%BASE%\" goto :eof
rem for /d /r walks every directory and ignores the name given to it, so the
rem name is compared here. PLEN is the length of PAT, needed for the prefix.
set "PLEN=0"
set "CHARS=%PAT%"
for /l %%I in (1,1,64) do if defined CHARS ( set "CHARS=!CHARS:~1!" & set /a PLEN+=1 )
for /d /r "%BASE%" %%D in (*) do (
	set "DPATH=%%~fD"
	set "DNAME=%%~nxD"
	if "%WILD%"=="*" (
		if /i "!DNAME:~0,%PLEN%!"=="%PAT%" call :nuke_dir "!DPATH!"
	) else (
		if /i "!DNAME!"=="%PAT%" call :nuke_dir "!DPATH!"
	)
)
goto :eof

:nuke_files
for /f "delims=" %%F in ('dir /b /s /a-d "%~1" 2^>nul') do call :nuke_file "%%~fF"
goto :eof

:pause-if-double-click
rem Keep the window open when the file is double-clicked in Explorer.
if defined CI goto :eof
echo "%CMDCMDLINE%" | find /i "%~nx0" >nul
if not errorlevel 1 (
	echo.
	pause
)
goto :eof
