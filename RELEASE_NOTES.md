# Quench 1.1.0

[English](#english) | [简体中文](#简体中文)

---

## English

### New: deep sleep — the one that actually gets the card to 0 W

Disabling the device is not the only way to stop the discrete GPU from drawing power, and
on a normal hybrid laptop it is often not the one you want. The GPU is not off just
because nothing is rendering on it: the vendor control centre polls it every couple of
seconds to paint its own readouts, and every poll pulls it straight back out of D3.

Measured on a hybrid machine at idle, with the vendor service running, the discrete GPU
spent only **20–30 %** of its time asleep. With that service stack stopped it spent
**100 %** — verified over 2, 4 and 10 minute windows, with no wake-up at all.

So Quench now has a second button. **Deep sleep** stops the vendor control centre's
background service, sets its start type to manual so it stays down across reboots, and
terminates its helper processes. Press **Back to normal** and everything comes back —
Quench remembers the original start type and writes it back.

While deep sleep is on:

- The discrete GPU stays in D3 and draws 0 W. Games, benchmarks and CUDA still work — the
  driver wakes the card on demand exactly as it always does.
- The vendor control centre shows "not connected", and GPU-mode switching, manual fan
  control, performance profiles and lighting are unavailable. Everything you already
  configured (fan curves, lighting, power limits) is kept and applies again afterwards.
- Only offered when a known vendor service stack is present. On other machines the button
  is hidden rather than guessed at.

### Also in this release

- Both features are available headless: `--quiet-on`, `--quiet-off`, `--quiet-status`, on
  top of the existing `--status`, `--disable`, `--enable`.
- The status pill row now shows deep sleep state alongside the routing and admin pills.
- Screenshots and both READMEs updated; the Chinese README covers both features.

### Which button do I want?

It depends on the machine. If the panel is wired to the discrete GPU (MUX in "discrete
only" mode) the card has to keep scanning it out, so it can never sleep, and disabling it
just leaves Windows with no driver for the panel. Put the MUX back to hybrid first, then
use deep sleep. If the panel already hangs off the integrated GPU, deep sleep is almost
always the better choice — it gets you the same 0 W without taking the card away.

### Requirements

- Windows 10 or 11, 64-bit — .NET Framework 4.8 is already there on both.
- An NVIDIA discrete GPU for the live power readout. Routing and the disable button work
  for any vendor.

### Verified on

A hybrid-graphics laptop (AMD integrated GPU + NVIDIA discrete GPU, Windows 10 64-bit,
.NET Framework 4.8). Deep sleep on → 120 s of continuous D3 with zero wake-ups → back to
normal → service running again. Enable → disable → enable round-trip confirmed through the
UI, with the device state re-read from the OS after each step.

`Quench.exe` in this release is built by GitHub Actions from the tagged source in this
repository.

---

## 简体中文

### 新增：深度省电 —— 真正让独显归零的那一个

想阻止独显吃电，「禁用设备」并不是唯一的办法，在普通的混合显卡笔记本上它往往也不是
最好的那个。独显并不是「没在画东西就等于关了」：厂商控制台每隔一两秒就轮询它一次去刷
新自己的读数，每一次轮询都把它从 D3 里直接拽出来。

在一台混合显卡机器上空载实测：厂商服务在跑的时候，独显只有 **20~30 %** 的时间在睡；
把那套服务停掉之后是 **100 %**——用 2 分钟、4 分钟和 10 分钟的窗口反复验证过，中间一次
唤醒都没有。

所以 Quench 现在多了第二个按钮。**深度省电**会停掉厂商控制台的后台服务、把它的启动类型
改成手动（这样重启之后它也不会自己爬起来），并终止它的辅助进程。点**恢复正常**就全部
回来——Quench 记得原来的启动类型，会把它写回去。

深度省电开着的时候：

- 独显稳定待在 D3，功耗 0 W。游戏、跑分、CUDA 完全不受影响——驱动照旧随叫随醒。
- 厂商控制台显示「未连接」，显卡模式切换、风扇手动调速、性能模式、灯效暂时不可用。
  你已经设置过的东西（风扇曲线、灯效、功耗墙）都会保留，关掉之后照旧生效。
- 只有检测到已知的厂商服务栈时才会出现这个按钮。别的机器上它直接隐藏，而不是乱猜。

### 这个版本还有

- 两个功能都有命令行入口：新增 `--quiet-on`、`--quiet-off`、`--quiet-status`，与原有的
  `--status`、`--disable`、`--enable` 并列。
- 底部状态胶囊新增深度省电状态，和路由、管理员两个胶囊并排。
- 截图与中英文 README 都已更新，中文 README 覆盖两个功能。

### 我该用哪个按钮？

取决于机器。如果内屏本身就接在独显上（MUX 处于独显直连），那独显必须一直扫描输出，永远
不可能睡，禁用也只会让 Windows 失去面板驱动。这种情况先把 MUX 切回混合模式，再用深度
省电。如果内屏已经挂在核显上，那深度省电几乎总是更好的选择——同样是 0 W，但不用把显卡
从系统里拿走。

### 系统要求

- Windows 10 或 11，64 位——两者都自带 .NET Framework 4.8。
- 实时功耗读数需要 NVIDIA 独显；路由显示和禁用按钮对任何厂商都有效。

### 验证环境

一台混合显卡笔记本（AMD 核显 + NVIDIA 独显、Windows 10 64 位、.NET Framework 4.8）。
深度省电开 → 连续 120 秒 D3、零唤醒 → 恢复正常 → 服务重新跑起来。启用 → 禁用 → 启用 的
往返流程通过界面完整验证，每一步之后都从操作系统重新读取设备状态核对。

本发布中的 `Quench.exe` 由 GitHub Actions 从本仓库对应的 tag 源码编译产出。
