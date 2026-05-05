# ADR 0016: CLI evidence provider 组合边界

日期：2026-05-06

## 状态

Accepted

## 背景

Windows evidence providers 已能读取服务、计划任务、启动项、卸载注册表和运行进程，但报告生成路径还需要一个明确组合边界。单元测试不应反复读取真实注册表和进程，否则测试会变慢且依赖本机状态。

## 决策

`ScanReportGenerator.Generate` 新增可选 `IFileEvidenceProvider` 参数。传入 provider 时，generator 会为每个扫描项附加 evidence；provider 失败时降级为 `EvidenceType.CollectionFailure`。

CLI 的生产入口 `Program.cs` 作为 composition root，显式传入：

- `CompositeFileEvidenceProvider`
- `WindowsEvidenceProviderFactory.CreateDefaultProviders()`

`CommandLineApp.Run` 本身保持可测试：未显式传入 provider 时使用空 provider，测试可以注入 stub provider，不读取真实系统状态。

报告 schema 仍为 `1.3`，因为 `evidence` 字段已经存在，本次只是接入真实数据来源。

## 理由

把 Windows provider 组合放在 `Program.cs`，可以让生产 CLI 默认具备 evidence 能力，同时保持命令解析和报告渲染测试稳定快速。

## 后果

优点：

- 生产 CLI 扫描会输出真实 Windows evidence。
- Core 不依赖 Windows 项目。
- CLI 单元测试不依赖真实注册表、任务计划程序或进程状态。

限制：

- 当前没有单独的端到端测试启动 `Program.cs` 进程验证默认组合。
- 默认 provider 组合仍只在 Windows 进程入口启用，库调用者需要主动传入 provider。
