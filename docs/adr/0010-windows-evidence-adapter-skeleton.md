# ADR 0010: Windows 证据适配器骨架

日期：2026-05-05

## 状态

Accepted

## 背景

Phase 2 需要收集文件与 Windows 服务、计划任务、启动项、卸载注册表和运行进程之间的关系。实现这些读取前，需要先建立稳定边界，保证任何适配器失败都不会让扫描崩溃，也不会降低风险判断。

## 决策

在 Core 中新增：

- `IFileEvidenceProvider`
- `CompositeFileEvidenceProvider`

组合器负责依次调用 provider，并将 provider 异常降级为 `EvidenceType.CollectionFailure`。

新增 `WinSafeClean.Windows` 项目，提供默认 Windows provider 骨架：

- `ServiceEvidenceProvider`
- `ScheduledTaskEvidenceProvider`
- `StartupEntryEvidenceProvider`
- `UninstallRegistryEvidenceProvider`
- `RunningProcessEvidenceProvider`

当前 Windows provider 只返回空 evidence，不读取系统状态。后续每个 provider 必须通过 TDD 单独实现。

## 理由

先建立组合与失败降级边界，可以避免后续 Windows API 读取失败时影响 CLI 或扫描流程。Windows 适配器独立于 Core，便于测试和后续替换。

## 后果

优点：

- Phase 2 有明确扩展点。
- 证据收集失败会被结构化记录。
- Core 不依赖 Windows API。

代价：

- 当前还不会发现真实服务、计划任务、注册表或进程引用。
- 每个 Windows provider 仍需要单独设计读取方式和权限降级测试。
