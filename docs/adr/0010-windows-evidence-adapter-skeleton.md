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
- `FileSignatureEvidenceProvider`
- `RunningProcessEvidenceProvider`

当前 Windows provider 通过骨架逐步填充真实读取逻辑。服务 `ImagePath` 读取已在 ADR 0011 中实现和记录，计划任务 action 读取已在 ADR 0012 中实现和记录，启动项注册表读取已在 ADR 0013 中实现和记录，卸载注册表读取已在 ADR 0014 中实现和记录，运行进程映像路径读取已在 ADR 0015 中实现和记录，文件签名读取已在 ADR 0021 中实现和记录。

## 理由

先建立组合与失败降级边界，可以避免后续 Windows API 读取失败时影响 CLI 或扫描流程。Windows 适配器独立于 Core，便于测试和后续替换。

## 后果

优点：

- Phase 2 有明确扩展点。
- 证据收集失败会被结构化记录。
- Core 不依赖 Windows API。

代价：

- 当前会发现服务 `ImagePath`、计划任务 Exec action、注册表启动项、卸载注册表和运行进程映像路径对目标文件的直接引用，也会标记目标路径位于已安装应用目录下的归属证据，并读取 Authenticode 文件签名来源证据。
- 每个 Windows provider 仍需要单独设计读取方式和权限降级测试。
