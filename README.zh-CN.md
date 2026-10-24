# Quench

**看清显示器接在哪块显卡上，并把独立显卡彻底断电。**

[English](README.md) | 简体中文

Quench 是一个单文件 Windows 小工具，专为「双显卡切换」的笔记本设计。它一眼告诉你：笔记本内屏到底由核显驱动、还是直连独显；独显此刻真实在耗多少瓦；以及——用一次点击就能把独显彻底禁用，让它停止耗电，全程不碰 BIOS。

![Quench](docs/screenshots/main.png)

## 为什么需要它

混合显卡的笔记本上，「没在渲染」不等于「没在耗电」。独显只是待在最低性能档里继续吃着电——就算没接任何显示器，**一般也有 10~30 W**；只要有浏览器标签、游戏启动器或者厂商控制台碰它一下，立刻窜到 **25~40 W**，哪怕那个程序根本没用它干活。两次触碰之间独显确实会自己落回 D3，但任何轮询它的东西都会把它直接拽回来。

而 Windows 桌面本身没有「只用核显」这个开关。固件里的选项通常只有两档（混合 ⇄ 独显直连），所以想在不进 BIOS 的前提下把独显功耗真正压到 0，唯一的路就是把设备禁掉——这本来也是各家厂商控制中心内部偷偷在做的事，Quench 只是把它摆到台面上，一次点击搞定。

## 它显示什么

- **显示路由** —— 一张图，画出每个已接入的显示器和**真正在驱动它的那块显卡**。数据取自 `EnumDisplayDevices` 加 EDID，反映的是物理接线，而不是驱动报上来的「偏好设置」。
- **实时板卡功耗** —— 通过 NVML 读的真实瓦数，和 `nvidia-smi` 同一个数据源，但不需要管理员权限。
- **设备与电源状态** —— 启用/禁用，以及独显现在是挂在 D0 还是已经退到 D3。
- **温度、频率、PCIe 链路、显存** —— 打游戏时看个大概。

## 它做什么

一个按钮禁用独显（或者重新启用）。底层走的是 Windows SetupAPI 的类安装器，和设备管理器自己用的机制完全一样，所以完全可逆，不用重启、不用重装驱动、不用进 BIOS。

禁用之后独显功耗是**真正的 0**。内屏照常显示，因为它本来就接在核显上。任何物理上直连独显的接口——大多数游戏本的外接 HDMI / DisplayPort——会黑掉，直到你重新启用。

## 截图

| 启用状态 —— 独显在 D3 待机 | 确认框 | 禁用后 —— 0 W |
| --- | --- | --- |
| ![](docs/screenshots/main.png) | ![](docs/screenshots/confirm.png) | ![](docs/screenshots/disabled.png) |

## 系统要求

- Windows 10 或 11，64 位
- .NET Framework 4.8（受支持的 Win10/11 都自带）
- 实时功耗读数需要 NVIDIA 独显；路由图和启用/禁用按钮对任何厂商的显卡都有效

## 下载

到 [最新发布](releases/latest) 下 `Quench.exe`。单文件、自包含——不用安装、没有依赖、不用解压。

## 使用

双击 `Quench.exe`。窗口一开就显示当前路由和功耗状态。

- **禁用独立显卡** —— 先弹确认框，确认后禁用设备。Windows 会弹 UAC，因为改设备状态需要管理员权限；Quench 会自己重启提权后完成操作。
- **启用独立显卡** —— 原路恢复，同样一次点击。
- **EN / 中文** —— 切换界面语言。

窗口会自己刷新。它**故意不做高频轮询**：读 NVML 会把独显从 D3 唤醒，一个用来测待机功耗的工具，不该成为独显永远不睡觉的原因。所以独显睡着的时候，Quench 显示 `0 W` 并明说「为保持休眠暂不采样」，而不是为了取一个数字把它叫醒。

### 命令行

同一个 exe 还有无界面模式，方便写脚本：

```
Quench.exe --status     # 打印当前设备状态
Quench.exe --disable    # 禁用独显
Quench.exe --enable     # 重新启用
```

这几个操作会自动提权，结果写到 exe 同目录的 `quench-result.txt`。

## 实现原理

| 需求 | 用到的机制 |
| --- | --- |
| 哪块显卡驱动哪个显示器 | `EnumDisplayDevices` + `EnumDisplaySettings`，显示器名字从注册表里的 EDID 解析 |
| 实时功耗、温度、频率 | P/Invoke 调 `nvml.dll`，配合自适应采样策略，避免把 GPU 一直钉醒 |
| 设备状态 | `CM_Get_DevNode_Status`、`CM_PROB_DISABLED` |
| D0/D3 电源状态 | `DEVPKEY_Device_PowerData`（`SetupDiGetDeviceProperty`） |
| 禁用 / 启用 | `SetupDiSetClassInstallParams` + `SetupDiCallClassInstaller(DIF_PROPERTYCHANGE)` |

不安装任何东西，不创建服务，不写注册表。唯一的持久改动是设备自身的启用/禁用标志，点按钮就能翻回去。

## 编译

项目是纯 C#，没有任何 NuGet 包，也不用 SDK 风格的工程文件，所以用 .NET Framework 自带的那个 C# 编译器就能编：

```bat
build.bat
```

手工编译：

```bat
csc.exe -nologo -target:winexe -out:Quench.exe -platform:anycpu -optimize+ ^
  -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Core.dll ^
  -win32icon:assets\quench.ico ^
  src\AssemblyInfo.cs src\Program.cs src\Native.cs src\Theme.cs src\Loc.cs ^
  src\DisplayTopology.cs src\GpuDevice.cs src\Nvml.cs src\Elevation.cs ^
  src\Headless.cs src\UiControls.cs src\MonitorList.cs src\MainForm.cs
```

源文件是 **UTF-8 带 BOM**。编辑时请保留 BOM——否则编译器和 Windows 的各种工具会按 ANSI 去读，中文字符串就成乱码了。

## 注意事项与限制

- 禁用独显是系统级改动。如果外接显示器插在属于独显的输出口上，禁用期间它会黑掉。
- 有些笔记本即使在混合模式下也把外接 HDMI / DisplayPort 走在核显上，那些不受影响。
- 把**内屏**在两块显卡之间切换属于固件（MUX）的活。Quench 只负责如实报告当前路由，不能改——那还是得进 BIOS 或用厂商工具。
- 睡眠唤醒、驱动更新、BIOS 更新都可能把设备重新启用。再点一次按钮即可。
- 禁用只是把设备从驱动栈里摘掉，并不会把它从 PCI 总线上移除：设备管理器里仍然看得到它，只是显示为停用（带一个错误码）。这是正常现象——卡本身已经在 D3，驱动也已卸载。
- 整机功耗的大头是 CPU、屏幕（高刷新率 + 满亮度很费电）以及其它正在跑的东西。Quench 只负责独显自己那一份——想看总数，请用墙插功率表，别指望软件。
- 已在一台混合显卡笔记本（AMD 核显 + NVIDIA 独显、Windows 10 64 位）上验证。其它混合显卡的机器行为应该一致，但路由检测是最可能需要在特殊硬件上做微调的部分——欢迎带着你的 `--status` 输出提 issue。

## 许可证

MIT —— 见 [LICENSE](LICENSE)。
