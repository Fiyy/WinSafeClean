# ADR 0031: Phase 3 后产品范围和入口选择

日期：2026-05-08

## 状态

Accepted

## 背景

Phase 3 文件级清理计划、隔离和恢复闭环已经具备。`PROGRESS.md` 中仍保留若干待决策项：

- UI 使用 WPF 还是 WinUI 3。
- 是否兼容 BleachBit CleanerML 作为规则输入。
- 是否在 MVP 使用 SQLite 保存扫描历史。
- 是否提供 PowerShell 模块入口。

这些决策会影响后续开发范围和依赖边界。

## 决策

1. Phase 4 UI 技术路线选择 WPF。
2. CleanerML 决策已完成：只读取用户显式提供的 CleanerML 安全子集作为 `KnownCleanupRule` evidence，不内置 GPL 规则，不执行 CleanerML 动作。
3. MVP 不引入 SQLite 扫描历史，CLI 继续保持无数据库状态。
4. MVP 不提供 PowerShell 模块入口，继续以 CLI 作为唯一入口。

## 理由

WPF 与 .NET 8 桌面开发栈稳定、部署复杂度低，适合先做本地、安全优先的 Windows 工具。WinUI 3 和 Windows App SDK 可以提供更现代 UI，但会增加打包、运行时和部署复杂度，不适合作为当前安全核心刚稳定后的下一步。

SQLite 扫描历史会引入数据保留、隐私、迁移和清理策略问题。当前报告已经支持 JSON/Markdown 输出，足以满足 MVP 的可复现和可分享需求。

PowerShell 模块适合高级用户自动化，但会扩大命令表面积。当前真实写操作刚加入强确认，先保持 CLI 单入口更容易审计。

## 后果

优点：

- Phase 4 有明确 UI 技术路线。
- MVP 继续保持依赖少、入口少、状态少。
- CleanerML 许可和执行边界保持清晰。

限制：

- MVP 不提供扫描历史数据库。
- MVP 不提供 PowerShell-native cmdlet。
- UI 先服务 Windows 桌面，不追求 WinUI 3 的现代控件和打包模型。
