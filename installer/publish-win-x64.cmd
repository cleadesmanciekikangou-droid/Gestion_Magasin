@echo off
setlocal
cd /d "%~dp0\.."
dotnet publish src\GestionMagasin.WinForms\GestionMagasin.WinForms.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts\publish\win-x64
if errorlevel 1 exit /b %errorlevel%
echo Publication x64 terminee: artifacts\publish\win-x64
