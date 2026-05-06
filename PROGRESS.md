# 项目进度

## 当前状态

阶段：2 - Windows evidence 适配器

日期：2026-05-06

## 已完成

- 创建项目目录结构
- 建立项目 README
- 建立项目原则
- 建立项目目标
- 建立 TDD 策略
- 建立 AI Agent 协作指南
- 建立调研摘要
- 建立风险模型
- 建立实现框架
- 建立路线图
- 建立首个 ADR
- 建立根目录 `AGENTS.md`
- 建立质量门禁
- 建立任务模板
- 补充子 Agent 分工优先原则
- 初始化 git 仓库
- 安装项目内本地 .NET 8 SDK 到 `.tools/dotnet`
- 新增 `global.json`
- 新增项目级 `NuGet.config`
- 新增 `scripts/bootstrap-dotnet.ps1`
- 新增 `scripts/test.ps1`
- 初始化 `WinSafeClean.sln`
- 创建 `WinSafeClean.Core`
- 创建 `WinSafeClean.Core.Tests`
- 按 TDD 建立禁止路径识别测试
- 实现最小路径风险分类核心
- 建立 JSON 报告模型测试
- 实现最小 JSON 报告序列化
- 根据子 Agent 审查补充路径规范化安全测试
- 支持 `..`、`\\?\`、本机 admin share、重复分隔符和非 C 盘 Windows 目录保护
- 创建公开 GitHub 仓库：https://github.com/Fiyy/WinSafeClean
- 添加 Markdown 报告输出
- 修复 `scripts/test.ps1`，确保测试失败时传播退出码
- 创建 `WinSafeClean.Cli`
- 创建 `WinSafeClean.Cli.Tests`
- 实现只读 CLI：`scan --path <PATH> [--format json|markdown] [--privacy full|redacted] [--output <FILE>]`
- CLI 明确拒绝 `delete`、`clean`、`quarantine`、`restore`、`plan` 等执行型命令
- 修复子 Agent 审查发现的 `--output` 覆盖风险
- `--output` 只允许创建不存在的新报告文件，并拒绝受保护 Windows 路径
- Markdown 报告会将控制字符渲染成可见转义
- 添加文件扫描抽象 `FileSystemScanner`
- 支持扫描单个文件、目录直接子项和缺失路径降级
- 支持 CLI `--max-items`
- 支持 CLI `--no-recursive`
- 缺失但受保护的 Windows 路径仍保留 `Blocked` 风险
- 修复子 Agent 审查发现的 `MaxItems` 枚举成本边界
- 非法路径语法降级为 `Unknown / ReportOnly` 报告项，CLI 不崩溃
- 新增 Core 层 `ScanReportGenerator`，CLI 复用该报告生成入口
- 新增 `IFileSystem` 探针和系统适配器，用于稳定测试文件系统异常降级
- 增加 scanner 权限、路径过长、IO、安全策略异常降级测试
- 增加报告生成流程测试
- 新增 ADR 0006，记录报告项 `itemKind` 与有限时间元数据方向
- 报告 schema 演进到 `1.1`
- `ScanReportItem` 新增 `itemKind` 和可空 `lastWriteTimeUtc`
- JSON 和 Markdown 报告输出文件/目录/未知类型以及可读取的修改时间
- 时间戳读取失败时只置空，不中断扫描
- 补充 CLI 参数边界测试：缺值、未知选项、缺失 output parent、已有目录 output
- 补充 serializer 空报告项测试
- 报告 schema 演进到 `1.2`
- 新增报告 `privacyMode`
- CLI 支持 `--privacy full|redacted`
- redacted 报告会替换路径 token，并抑制 `lastWriteTimeUtc`
- redacted 会处理 `reasons` 和 `blockers` 中的已知路径
- 新增 ADR 0007，记录报告隐私模式与兼容影响
- CLI 支持显式 `--recursive`
- 递归扫描使用全局 `MaxItems`
- 递归扫描默认不跟随 reparse point、junction 或 symlink
- 新增 ADR 0008，记录递归扫描策略
- 报告 schema 演进到 `1.3`
- 新增报告项 `evidence`
- 新增 `EvidenceRecord` 和 `EvidenceType`
- Markdown 报告新增 Evidence 区块
- redacted 会处理 evidence 中的路径文本
- 新增 ADR 0009，记录证据模型
- 新增 `WinSafeClean.Windows`
- 新增 `WinSafeClean.Windows.Tests`
- 新增 `IFileEvidenceProvider` 和 `CompositeFileEvidenceProvider`
- 新增 Windows evidence provider 骨架：服务、计划任务、启动项、卸载注册表、运行进程
- 证据 provider 失败会降级为 `CollectionFailure` evidence
- 新增 ADR 0010，记录 Windows 证据适配器骨架
- 实现 `ServiceEvidenceProvider` 读取 Windows 服务 `ImagePath`
- 新增 `IWindowsServiceSource` 和 `RegistryWindowsServiceSource`
- 新增 `ServiceImagePathParser`，覆盖 quoted/unquoted path、参数、环境变量、`\SystemRoot` 和 `\??\` 前缀
- 新增 ADR 0011，记录服务 `ImagePath` 证据策略
- 实现 `ScheduledTaskEvidenceProvider` 读取计划任务 Exec action
- 新增 `IWindowsScheduledTaskSource` 和 `FileSystemWindowsScheduledTaskSource`
- 计划任务数据源只读扫描 `%SystemRoot%\System32\Tasks` XML，并跳过 reparse point 与损坏任务文件
- 新增 ADR 0012，记录计划任务 action 证据策略
- 实现 `StartupEntryEvidenceProvider` 读取注册表 `Run` / `RunOnce`
- 新增 `IWindowsStartupEntrySource` 和 `RegistryWindowsStartupEntrySource`
- 启动项数据源覆盖 HKCU、HKLM 和 HKLM Wow6432Node 常见位置
- 新增 ADR 0013，记录启动项注册表证据策略
- 实现 `UninstallRegistryEvidenceProvider` 读取卸载注册表项
- 新增 `IWindowsUninstallEntrySource` 和 `RegistryWindowsUninstallEntrySource`
- 卸载注册表 provider 区分直接引用 evidence 和 `InstallLocation` 归属 evidence
- 新增 ADR 0014，记录卸载注册表证据策略
- 实现 `RunningProcessEvidenceProvider` 读取当前运行进程映像路径
- 新增 `IWindowsProcessSource` 和 `SystemWindowsProcessSource`
- 运行进程 provider 对单个进程路径读取失败做跳过降级，不读取进程命令行
- 新增 ADR 0015，记录运行进程证据策略
- `ScanReportGenerator` 支持注入 `IFileEvidenceProvider`
- `Program.cs` 默认组合 Windows evidence providers，`CommandLineApp.Run` 保持可注入测试边界
- 新增 ADR 0016，记录 CLI evidence provider 组合边界
- 完成 BleachBit CleanerML 兼容性调研
- 新增 ADR 0017，决定未来只读解析 CleanerML 安全子集，不直接执行或内置 GPL 规则
- 新增报告 schema `1.3` JSON 兼容 fixture
- 新增 schema fixture 测试，锁住 evidence、risk、privacyMode、itemKind 和时间字段输出结构
- `FileSystemScanOptions` 新增 `CancellationToken`
- Core scanner、report generator 和 CLI 支持扫描取消
- `Program.cs` 支持 Ctrl+C 取消，CLI 取消返回 exit code `130`
- 新增 ADR 0018，记录扫描取消机制
- 新增 Program 级端到端 CLI 测试，验证真实入口和默认 evidence provider 组合
- 新增 `WinSafeClean.CleanerRules`
- 新增 `WinSafeClean.CleanerRules.Tests`
- 实现 CleanerML 安全子集解析器：metadata、option、running blocker、file/glob/walk 候选
- CleanerML 解析器忽略 `winreg`、`process`、`truncate`、`shred`、`deep` 和非 Windows action
- 新增 `CleanerRuleEvidenceProvider`
- CleanerML file、glob、walk 候选可映射为 `KnownCleanupRule` evidence
- CleanerML running blocker 会进入 evidence message，但不会执行进程检查
- `IFileEvidenceProvider.CollectEvidence` 支持可选 `CancellationToken`
- Composite、report generator、Windows providers 和 CleanerRules provider 会传播取消
- 取消不会被错误转换为 `CollectionFailure` evidence
- 新增 ADR 0019，记录 evidence provider 内部取消策略
- 新增 Core `Planning` 模块
- 新增只读 `CleanupPlan`、`CleanupPlanItem`、`CleanupPlanAction`
- `CleanupPlanGenerator` 将报告项保守映射为 `Keep`、`ReportOnly` 或 `ReviewForQuarantine`
- 新增 ADR 0020，记录只读清理计划草案
- 新增 `CleanerMlRuleFileLoader`
- CleanerML 支持显式加载单个用户规则文件或目录顶层 `.xml` 文件
- CleanerML 规则加载支持取消 token，不递归、不自动下载
- 新增 `CleanupPlanJsonSerializer`
- 新增 `CleanupPlanMarkdownSerializer`
- CLI 新增只读 `plan` 命令，支持 JSON/Markdown 输出
- `plan` 命令只生成预览，不执行删除、隔离或修复
- 新增 `CleanupPlan` JSON schema fixture，覆盖 `Keep`、`ReportOnly`、`ReviewForQuarantine`
- 新增 CLI `plan` Program 级端到端测试
- 补充 CLI `plan` redacted 输出、输出文件保护、输入不修改和 Markdown 转义测试
- CLI `scan` / `plan` 支持 `--cleanerml <FILE_OR_DIR>`
- `--cleanerml` 只加载用户显式提供的规则文件或目录顶层 `.xml` 文件
- `plan --cleanerml` 可将命中的 CleanerML 候选体现在只读计划原因中，但不会执行清理
- 新增 CLI Program 级 `scan --cleanerml` 端到端测试
- 新增 `docs/USAGE.md`，补充 `scan`、`plan`、CleanerML 和 `--output` 的只读使用示例
- 验证命令：`pwsh -NoProfile -File scripts\test.ps1`
- 测试通过：168 passed

## 正在进行

- Phase 3 只读清理计划

## 下一步

1. 设计文件签名 evidence provider。

## 待决策

- UI 使用 WPF 还是 WinUI 3。
- 是否兼容 BleachBit CleanerML 作为规则输入。
- 是否在 MVP 使用 SQLite 保存扫描历史。
- 是否提供 PowerShell 模块入口。
