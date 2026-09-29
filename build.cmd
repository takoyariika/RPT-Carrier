@echo off
setlocal
cd /d "%~dp0"

set "CSC64=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "CSC32=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if exist "%CSC64%" (
  set "CSC=%CSC64%"
) else if exist "%CSC32%" (
  set "CSC=%CSC32%"
) else (
  echo [ERROR] .NET Framework 4.x C# compiler was not found.
  exit /b 1
)

echo Building RPTCarrier.exe...
"%CSC%" /nologo /optimize+ /target:winexe /platform:anycpu ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /out:"RPTCarrier.exe" "RPTCarrier.cs"
if errorlevel 1 exit /b %errorlevel%

echo Building RPTCarrier.x86.exe...
"%CSC%" /nologo /optimize+ /target:winexe /platform:x86 ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /out:"RPTCarrier.x86.exe" "RPTCarrier.cs"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Built:
echo   %CD%\RPTCarrier.exe
echo   %CD%\RPTCarrier.x86.exe
echo.
echo Tip: RPTCarrier.ini is optional. Launch with no arguments to choose an EXE.
