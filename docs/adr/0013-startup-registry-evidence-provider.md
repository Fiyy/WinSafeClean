# ADR 0013: 启动项注册表证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

应用常通过注册表 `Run` 和 `RunOnce` 项配置登录后自启动。删除被这些启动项引用的文件可能导致应用更新器、托盘程序或安全组件异常。

## 决策

`StartupEntryEvidenceProvider` 通过可注入的 `IWindowsStartupEntrySource` 获取启动项记录。默认实现 `RegistryWindowsStartupEntrySource` 只读读取：

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce`

读取注册表值时保留原始环境变量文本，由路径解析器统一展开。匹配成功时输出 `EvidenceType.StartupReference`，source 使用 hive、注册表位置和值名，confidence 为 `0.85`。

## 理由

注册表 `Run`/`RunOnce` 是最常见且结构明确的启动项来源，适合作为首批实现。Startup 文件夹中的 `.lnk` 目标解析涉及 Shell Link/COM 边界，先保留为后续独立任务，避免把证据读取面一次铺得过宽。

## 后果

优点：

- 可以发现目标文件被常见注册表启动项直接引用。
- 读取保持只读，不修改注册表。
- HKCU、HKLM 和 32-bit Wow6432Node 位置均被覆盖。

限制：

- 当前不解析 Startup 文件夹快捷方式。
- 当前不解析更少见的启动扩展点，例如 Winlogon、Services 或 Browser Helper Objects。
- 当前只匹配启动项命令中的可执行路径，不分析脚本参数中的间接引用。
