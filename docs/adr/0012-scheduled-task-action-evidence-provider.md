# ADR 0012: 计划任务 Action 证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

Windows 计划任务经常作为应用自启动、维护任务、更新器和脚本入口。安全清理工具在建议删除文件前，需要知道目标文件是否被计划任务 action 直接引用。

## 决策

`ScheduledTaskEvidenceProvider` 通过可注入的 `IWindowsScheduledTaskSource` 获取任务记录。默认实现 `FileSystemWindowsScheduledTaskSource` 只读扫描：

- `%SystemRoot%\System32\Tasks`
- task XML 中的 `RegistrationInfo/URI`
- `Actions/Exec/Command`
- `Actions/Exec/Arguments`
- `Actions/Exec/WorkingDirectory`

数据源递归读取任务文件，但跳过 reparse point 目录。单个任务文件读取失败、权限不足或 XML 损坏时跳过该文件，不中断整个 provider。

匹配成功时输出 `EvidenceType.ScheduledTaskReference`，source 优先使用 task `URI`，confidence 为 `0.9`。

## 理由

直接读取任务 XML 可以避免 COM Task Scheduler 依赖和注册表写入风险，同时保留 action 原始 command/arguments。source 可注入，provider 测试不依赖真实系统任务。

## 后果

优点：

- 可以发现目标文件被计划任务 Exec action 直接引用。
- XML 读取保持只读，适合当前 evidence 阶段。
- 单个损坏任务不会让扫描失败。

限制：

- 当前只解析 `Exec` action，不解析 COM handler 等其他 action 类型。
- 当前只匹配 action command 中的可执行路径，不分析脚本参数中被间接引用的文件。
- 当前跳过不可读任务文件，不为每个文件生成独立 collection failure evidence。
