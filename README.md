# WinSafeClean

WinSafeClean 是一个面向 Windows 日常使用场景的安全磁盘清理与文件解释工具。

GitHub: https://github.com/Fiyy/WinSafeClean

项目目标不是做一个“见到垃圾就删除”的清理器，而是先回答三个问题：

1. 这个文件或目录是干什么的？
2. 它和哪些程序、系统组件、服务、计划任务、启动项或注册表记录有关？
3. 删除、隔离或交给系统工具清理是否会影响 Windows 或应用程序正常运行？

当前项目处于 Phase 3：清理计划、隔离和恢复。默认不做真实删除；真实文件移动只通过带强确认的 `quarantine` 和 `restore` 命令开放。

## 核心方向

- 先解释，再评估，最后才允许清理。
- 默认只读扫描，任何删除能力都必须经过风险模型和测试约束。
- 高置信度清理项可以自动建议；中低置信度项目只能报告或隔离。
- 系统敏感目录必须进入禁止或强警告列表。
- 所有核心判断逻辑先写测试，再写实现。

## 建议技术路线

- Runtime: .NET 8
- MVP 入口: CLI
- 后续 UI: WPF
- 测试框架: xUnit
- 核心模块: 文件发现、归属识别、关系证据、风险评分、清理计划、隔离恢复

当前仓库使用项目内本地 SDK：`.tools/dotnet/dotnet.exe`。该目录已加入 `.gitignore`，避免把工具链提交进仓库。

当前项目包含：

- `WinSafeClean.Core`：只读扫描、风险、报告和 evidence 基础模型
- `WinSafeClean.Cli`：默认只读 CLI，包含带强确认的文件隔离和恢复命令
- `WinSafeClean.Windows`：Windows evidence provider，已支持服务 `ImagePath`、计划任务 Exec action、注册表启动项、卸载注册表、文件 Authenticode 签名和运行进程映像路径关系证据
- `WinSafeClean.CleanerRules`：CleanerML 安全子集解析器、用户规则文件加载器和 `KnownCleanupRule` evidence provider，只读取规则候选，不执行清理动作
- `WinSafeClean.Core.Quarantine`：恢复元数据、内容 hash、操作日志、执行前校验、最小隔离执行器和最小恢复执行器

常用命令：

```powershell
pwsh -File .\scripts\bootstrap-dotnet.ps1
pwsh -File .\scripts\test.ps1 -Restore
```

## CLI

CLI 默认是只读报告器；`scan`、`plan` 和 `preflight` 不执行清理、删除、隔离或修复。`quarantine` 和 `restore` 是唯二会移动文件的命令，必须同时提供 `--manual-confirmation` 和 `--i-understand-this-moves-files`。

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Windows\Installer --format markdown
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --format json --output .\scan-report.json
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path . --max-items 50 --no-recursive
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --privacy redacted
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --recursive --max-items 200
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- plan --path C:\Temp --format markdown
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- plan --path C:\Temp --cleanerml .\rules\example.xml
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- preflight --plan .\plan.json --metadata .\abcd.restore.json --manual-confirmation
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- quarantine --plan .\plan.json --metadata .\abcd.restore.json --manual-confirmation --i-understand-this-moves-files
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- restore --metadata .\abcd.restore.json --manual-confirmation --i-understand-this-moves-files
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- restore --metadata .\legacy.restore.json --manual-confirmation --i-understand-this-moves-files --allow-legacy-metadata-without-hash
```

当前支持：

- `scan --path <PATH>`
- `plan --path <PATH>`，输出只读清理计划预览
- `preflight --plan <FILE> --metadata <FILE>`，输出只读执行前校验清单
- `quarantine --plan <FILE> --metadata <FILE> --manual-confirmation --i-understand-this-moves-files`，通过 preflight 后移动文件到隔离路径
- `restore --metadata <FILE> --manual-confirmation --i-understand-this-moves-files`，通过恢复元数据把隔离文件移回原路径
- `--format json|markdown`
- `--privacy full|redacted`，默认 `full`
- `--output <FILE>`，只允许写入不存在的新报告或结果文件
- `--operation-log <FILE>`，为 `quarantine` 和 `restore` 追加 JSONL 操作日志
- `--max-items <N>`，限制返回项数量
- `--recursive`，显式启用递归扫描
- `--no-recursive`，显式保持单层目录扫描
- `--cleanerml <FILE_OR_DIR>`，显式加载用户提供的 CleanerML 文件或目录顶层 `.xml` 文件，只作为只读规则证据

`preflight` 不接受 `--path`、`--cleanerml`、`--recursive` 或 `--privacy`，也不会重新扫描。`isExecutable=false` 表示校验完成但未来执行不应继续，命令本身仍返回 exit code `0`；只有参数、输入文件或输出路径错误返回 exit code `2`。

目录扫描默认只读取直接子项；传入 `--recursive` 后会扫描子树，但仍不会跟随 reparse point、junction 或 symlink。目录项大小暂记为 `0 B`，不会递归计算目录大小。

当前报告 schema 为 `1.3`。每个报告项包含路径、`itemKind`、大小、可空 `lastWriteTimeUtc`、`evidence` 和风险判断；时间戳读取失败时只输出 `null` 或 Markdown `-`，不会中断扫描。`--privacy redacted` 会替换路径 token、抑制修改时间，并处理 evidence/reasons/blockers 中的路径文本，适合分享报告时降低直接泄露风险。

通过 `src\WinSafeClean.Cli\Program.cs` 启动 CLI 时，报告会默认接入 Windows evidence providers；`CommandLineApp.Run` 仍支持测试或库调用方注入自定义 provider。

CLI 支持 Ctrl+C 取消扫描；取消时返回 exit code `130`，不输出部分报告。

Core 已包含只读 `CleanupPlan` 草案模型，可把扫描报告转换为 `Keep`、`ReportOnly` 或 `ReviewForQuarantine` 预览动作。当前清理计划 schema 为 `0.2`，会为 `ReviewForQuarantine` 项附加 `quarantinePreview`，展示拟议隔离路径和恢复元数据路径；`--privacy redacted` 会同步脱敏隔离预览路径。`plan` 本身仍不创建目录、不写元数据、不执行删除或隔离。

`quarantine` 写出的 restore metadata schema 为 `1.1`，包含隔离时源文件的 SHA256 内容 hash。`restore` 遇到带 hash 的 metadata 时会先校验隔离文件内容，hash 不匹配则拒绝恢复。

旧版 restore metadata 如果缺少内容 hash，`restore` 默认拒绝执行；必须额外传入 `--allow-legacy-metadata-without-hash` 才允许在无内容校验的情况下恢复。目录隔离和目录恢复在 Phase 3 暂不支持。

当前明确拒绝：

- `delete`
- `clean`
- `--delete`
- `--fix`
- `--quarantine`
- `--clean`

`--output` 不会覆盖已有文件，也不会写入受保护 Windows 路径。

详细设计见：

- [项目原则](PROJECT_PRINCIPLES.md)
- [项目目标](GOALS.md)
- [实现框架](docs/IMPLEMENTATION_FRAMEWORK.md)
- [TDD 策略](TDD_STRATEGY.md)
- [AI Agent 协作指南](AI_AGENT_GUIDE.md)
- [Agent 根目录约束](AGENTS.md)
- [风险模型](docs/RISK_MODEL.md)
- [使用示例](docs/USAGE.md)
- [调研摘要](docs/RESEARCH_SUMMARY.md)
- [路线图](docs/ROADMAP.md)
- [质量门禁](docs/QUALITY_GATES.md)
- [任务模板](docs/TASK_TEMPLATE.md)
- [项目进度](PROGRESS.md)
