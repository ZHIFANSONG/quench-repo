# Quench 1.0.0

[English](#english) | [简体中文](#简体中文)

---

## English

First release. A single self-contained executable — no installer, no runtime to install,
no config files.

### What it does

Quench answers two questions about a hybrid-graphics laptop, then gives you the switch
to act on the answer:

1. **Which GPU is my screen actually wired to?** It enumerates the display adapters and
   parses the EDID of each attached monitor, so the diagram shows the physical wiring —
   internal panel on the integrated GPU, or routed through the discrete one.
2. **How much power is the discrete GPU drawing right now?** Real wattage straight from
   NVML, plus temperature, clocks, PCIe link width and VRAM.

Then one button **disables the discrete GPU**, which takes it from 10–30 W of idle draw
(bursting to 25–40 W whenever something pokes it) to zero: the device is powered down to
D3 and its driver is unloaded, so no app can wake it again. It is the same mechanism
Device Manager uses, so it is fully reversible, needs no reboot, and never touches the
BIOS.

### Highlights

- **Zero-wake telemetry.** Reading NVML wakes the GPU out of D3. Quench polls the passive
  device power state instead, and only samples NVML when the GPU is already awake — so
  the tool that measures idle power is not the reason the GPU never idles.
- **No blind trust in flags.** Hybrid vs. direct is decided from which adapter the panel
  hangs off, not from a preference the driver reports.
- **Chinese and English**, picked from the system locale and switchable in the window.
- **Headless mode** for scripting: `--status`, `--disable`, `--enable`.
- **No leftovers.** Nothing is installed, no service, no registry writes. The only state
  is the device's own enabled flag, which the button toggles back.

### Requirements

- Windows 10 or 11, 64-bit — .NET Framework 4.8 is already there on both.
- An NVIDIA discrete GPU for the live power readout. Routing and the disable button work
  for any vendor.

### Known limits

- Anything wired physically to the discrete GPU (external HDMI/DP on most gaming
  laptops) goes dark while it is disabled. The internal panel is unaffected because it
  hangs off the integrated GPU.
- Moving the *panel* between GPUs is a firmware (MUX) setting; Quench reports it but
  cannot change it. That still needs the BIOS or the vendor utility.
- Sleep/resume, a driver update or a BIOS update can re-enable the device. Press the
  button again.

### Verified on

A hybrid-graphics laptop (AMD integrated GPU + NVIDIA discrete GPU, Windows 10 64-bit,
.NET Framework 4.8). Enable → disable → enable round-trip confirmed through the UI, with
the device state re-read from the OS after each step.

`Quench.exe` in this release is built by GitHub Actions from the tagged source in this
repository.

---

## 简体中文

首个版本。一个自包含的单文件可执行程序——不用安装、不用装运行时、没有配置文件。

### 它做什么

Quench 回答关于双显卡笔记本的两个问题，然后给你一个开关去执行答案：

1. **我的屏幕到底接在哪块显卡上？** 它枚举显示适配器并解析每台已接入显示器的 EDID，所以图里画的是物理接线——内屏挂在核显上，还是被绕到了独显。
2. **独显此刻真实在耗多少电？** 直接从 NVML 取的真实瓦数，外加温度、频率、PCIe 链路宽度和显存占用。

然后一个按钮**禁用独立显卡**，把它从空载 10~30 W（被程序碰一下就是 25~40 W）变成零：设备被下电到 D3、驱动被卸载，任何程序都没法再把它唤醒。走的是设备管理器同一个机制，所以完全可逆、不用重启、从不碰 BIOS。

### 亮点

- **不吵醒它的遥测。** 读 NVML 会把独显从 D3 唤醒。Quench 改成被动轮询设备电源状态，只在独显本来就醒着时才采一次 NVML——这样一个用来测待机功耗的工具，就不会成为独显永远不睡觉的原因。
- **不迷信标志位。** 混合还是直连，判断依据是内屏挂在哪块适配器下面，而不是驱动报上来的某个偏好值。
- **中英双语**，按系统区域自动选择，窗口里也能随时切换。
- **命令行模式**，方便脚本调用：`--status`、`--disable`、`--enable`。
- **没有残留。** 不安装东西、不建服务、不写注册表。唯一的状态就是设备自己的启用标志，点按钮就能翻回去。

### 系统要求

- Windows 10 或 11，64 位——两者都自带 .NET Framework 4.8。
- 实时功耗读数需要 NVIDIA 独显；路由显示和禁用按钮对任何厂商都有效。

### 已知限制

- 物理上直连独显的接口（大多数游戏本的外接 HDMI / DP）在禁用期间会黑屏。内屏不受影响，因为它挂在核显上。
- 把**内屏**在两块显卡之间切换是固件（MUX）的设置；Quench 如实报告但改不了。那仍然需要进 BIOS 或用厂商工具。
- 睡眠唤醒、驱动更新或 BIOS 更新都可能把设备重新启用。再点一次按钮即可。

### 验证环境

一台混合显卡笔记本（AMD 核显 + NVIDIA 独显、Windows 10 64 位、.NET Framework 4.8）。
启用 → 禁用 → 启用 的往返流程通过界面完整验证，每一步之后都从操作系统重新读取设备状态核对。

本发布中的 `Quench.exe` 由 GitHub Actions 从本仓库对应的 tag 源码编译产出。
