# Quench

**See which GPU your display is wired to — and cut power to the discrete one.**

English | [简体中文](README.zh-CN.md)

Quench is a single-file Windows utility for laptops with switchable graphics. It tells
you, at a glance, whether the built-in panel is driven by the integrated GPU or wired
straight to the discrete GPU, how much power the discrete GPU is really drawing, and it
can disable that GPU entirely so it stops drawing power — without touching the BIOS.

![Quench](docs/screenshots/main.png)

## Why

On a hybrid-graphics laptop the discrete GPU is not off just because nothing is
rendering on it. It parks in a low-power state and keeps sipping power — typically
**10–20 W** with no display attached — and jumps to **25–40 W** the moment a background
app touches its context, even if that app is doing nothing with it.

Windows has no supported way to say "use the integrated GPU only" from the desktop.
The firmware setting is usually binary (hybrid ⇄ discrete), so the only way to reach a
true dGPU-zero state without a reboot into the BIOS is to disable the device — which is
exactly what vendor control centres do internally, and what Quench does with a click.

## What it shows

- **Display routing** — a diagram of every attached monitor and the GPU actually
  driving it, read from `EnumDisplayDevices` plus the EDID, so it reflects the real
  wiring rather than a driver-reported preference.
- **Live board power** — real wattage read through NVML, the same source `nvidia-smi`
  uses, but without needing an elevated prompt.
- **Device and power state** — enabled/disabled, and whether the GPU is sitting in D0
  or has dropped to D3.
- **Temperature, clocks, PCIe link, VRAM** — for context while gaming.

## What it does

One button disables the discrete GPU (or re-enables it). The device is disabled through
the Windows SetupAPI class installer, the same mechanism Device Manager uses, so it is
completely reversible and needs no reboot, no driver reinstall and no BIOS trip.

While disabled the GPU draws **zero** power. The internal panel keeps working because it
is wired to the integrated GPU. Anything physically wired to the discrete GPU — external
HDMI/DisplayPort outputs on most gaming laptops — goes dark until you re-enable it.

## Screenshots

| Enabled — dGPU idling in D3 | Confirmation | Disabled — 0 W |
| --- | --- | --- |
| ![](docs/screenshots/main.png) | ![](docs/screenshots/confirm.png) | ![](docs/screenshots/disabled.png) |

## Requirements

- Windows 10 or 11, 64-bit
- .NET Framework 4.8 (present on every supported Windows 10/11 install)
- NVIDIA discrete GPU for the live power telemetry; the routing diagram and the
  enable/disable button work for any vendor

## Download

Grab `Quench.exe` from the [latest release](releases/latest). It is a single
self-contained executable — no installer, no dependencies, nothing to unzip.

## Usage

Double-click `Quench.exe`. The window opens on the current routing and power state.

- **Disable discrete GPU** — asks for confirmation, then disables the device. Windows
  shows a UAC prompt because changing a device's state requires administrator rights;
  Quench relaunches itself elevated and does the work.
- **Enable discrete GPU** — puts it back, same one-click path.
- **EN / 中文** — switches the interface language.

The window refreshes by itself. It deliberately does not poll the GPU constantly: reading
NVML wakes the GPU out of D3, and a tool that measures idle power should not be the
reason the GPU never idles. When the GPU is asleep, Quench shows `0 W` and says so
instead of waking it up to take a reading.

### Command line

For scripting, the same executable has a headless mode:

```
Quench.exe --status     # print the current device state
Quench.exe --disable    # disable the discrete GPU
Quench.exe --enable     # enable it again
```

These run elevated and write their result to `quench-result.txt` next to the executable.

## How it works

| Need | Mechanism |
| --- | --- |
| Which GPU drives which display | `EnumDisplayDevices` + `EnumDisplaySettings`, EDID parsed from the registry for the monitor name |
| Live power, temperature, clocks | `nvml.dll` via P/Invoke, with an adaptive sampling policy so it does not pin the GPU awake |
| Device state | `CM_Get_DevNode_Status`, `CM_PROB_DISABLED` |
| D0/D3 power state | `DEVPKEY_Device_PowerData` (`SetupDiGetDeviceProperty`) |
| Disable / enable | `SetupDiSetClassInstallParams` + `SetupDiCallClassInstaller(DIF_PROPERTYCHANGE)` |

Nothing is installed, no service is created, no registry keys are written. The only
persistent change is the device's own enabled/disabled flag, which the button toggles
back.

## Building

The project is plain C# with no NuGet packages and no SDK-style project file, so it
builds with the C# compiler that ships with the .NET Framework:

```bat
build.bat
```

To build by hand:

```bat
csc.exe -nologo -target:winexe -out:Quench.exe -platform:anycpu -optimize+ ^
  -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Core.dll ^
  -win32icon:assets\quench.ico ^
  src\AssemblyInfo.cs src\Program.cs src\Native.cs src\Theme.cs src\Loc.cs ^
  src\DisplayTopology.cs src\GpuDevice.cs src\Nvml.cs src\Elevation.cs ^
  src\Headless.cs src\UiControls.cs src\MonitorList.cs src\MainForm.cs
```

Source files use UTF-8 with a BOM. Keep the BOM when editing — the compiler and
Windows tooling will otherwise read the Chinese strings as ANSI.

## Notes and limits

- Disabling the discrete GPU is a system-wide change. If you have an external monitor
  plugged into an output that belongs to the discrete GPU, it will turn off.
- Some laptops route an external HDMI/DisplayPort through the integrated GPU even in
  hybrid mode; those keep working.
- Switching the *panel* between GPUs is a firmware (MUX) job. Quench reports the current
  routing but cannot change it — that still needs the BIOS or the vendor utility.
- Resuming from sleep, driver updates or a BIOS update can re-enable the device. Just
  press the button again.
- Developed and verified on a hybrid-graphics laptop (AMD iGPU + NVIDIA dGPU, Windows 10
  64-bit). Other hybrid laptops should behave the same way, but the routing detection is
  the part most likely to need tweaks on unusual hardware — issues with your `--status`
  output are welcome.

## License

MIT — see [LICENSE](LICENSE).
