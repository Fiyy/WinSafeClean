# 项目进度

## 当前状态

阶段：1 - 只读核心 MVP

日期：2026-05-05

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
- 验证命令：`pwsh -NoProfile -File scripts\test.ps1`
- 测试通过：97 passed

## 正在进行

- Phase 1 只读核心 MVP

## 下一步

1. 开始 Phase 2 Windows 证据收集设计：服务、计划任务、启动项、卸载注册表、进程引用。
2. 评估是否兼容 BleachBit CleanerML 作为规则输入。
3. 设计报告 schema 兼容测试夹具。
4. 为长时间扫描设计取消机制。

## 待决策

- UI 使用 WPF 还是 WinUI 3。
- 是否兼容 BleachBit CleanerML 作为规则输入。
- 是否在 MVP 使用 SQLite 保存扫描历史。
- 是否提供 PowerShell 模块入口。
