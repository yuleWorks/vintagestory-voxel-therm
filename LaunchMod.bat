@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "VSBASE=%~dp0..\..\"
set "CURRENTDIR=%~dp0"
set "MODDIR="

for /d %%D in ("%CURRENTDIR%*") do (
  if exist "%%D\modinfo.json" (
    set "MODDIR=%%D"
    goto :found
  )
)

echo ERROR: No folders within "%CURRENTDIR%" contain modinfo.json
exit /b 1

:found
echo "%MODDIR%"
start "" "%VSBASE%Vintagestory\Vintagestory.exe" --dataPath="%VSBASE%VintagestoryData" --addModPath "%MODDIR%\bin\Debug\Mods"
exit