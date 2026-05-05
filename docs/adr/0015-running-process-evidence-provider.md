# ADR 0015: 运行进程证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

如果目标文件正是当前运行进程的映像路径，删除或替换该文件通常会导致应用异常、更新失败或需要重启。因此运行进程引用是强证据。

## 决策

`RunningProcessEvidenceProvider` 通过可注入的 `IWindowsProcessSource` 获取进程记录。默认实现 `SystemWindowsProcessSource` 使用 `Process.GetProcesses()`，首批只读取：

- PID
- ProcessName
- MainModule.FileName

单个进程的 `MainModule` 因权限、位数或进程退出导致读取失败时，只将该进程路径置空或跳过，不中断整个 provider。

匹配成功时输出 `EvidenceType.RunningProcessReference`，source 使用进程名和 PID，confidence 为 `1.0`。

## 理由

进程映像路径是直接且高置信度的关系证据。首批不读取命令行、模块列表或窗口标题，避免扩大隐私面和权限面。

## 后果

优点：

- 可以识别目标文件正在被运行进程使用。
- 单个进程权限失败不会让扫描失败。
- 不读取进程命令行，减少敏感参数泄露。

限制：

- 当前不识别加载目标 DLL 的进程。
- 当前不读取命令行参数中间接引用的文件。
- PID 会随系统运行变化，只作为当前扫描时刻证据。
