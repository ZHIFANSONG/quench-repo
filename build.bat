@echo off
setlocal
cd /d "%~dp0"
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" -nologo -target:winexe -out:Quench.exe -platform:anycpu -optimize+ ^
 -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Core.dll -r:System.ServiceProcess.dll ^
 -win32icon:assets\quench.ico ^
 src\AssemblyInfo.cs src\Program.cs src\Native.cs src\Theme.cs src\Loc.cs ^
 src\DisplayTopology.cs src\GpuDevice.cs src\Nvml.cs src\Elevation.cs ^
 src\Headless.cs src\UiControls.cs src\MonitorList.cs src\PowerSave.cs ^
 src\MainForm.cs
if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)
echo BUILD OK
