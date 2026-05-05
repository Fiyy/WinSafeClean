# ADR 0009: 报告项证据模型与 schema 1.3

日期：2026-05-05

## 状态

Accepted

## 背景

项目目标要求先解释文件用途和它与程序、系统组件、服务、计划任务、启动项或注册表之间的关系。此前 `RiskAssessment.Reasons` 只能输出自由文本，不能表达结构化证据来源，也不适合后续 Windows 适配器复用。

## 决策

报告 schema 从 `1.2` 演进到 `1.3`。`ScanReportItem` 新增：

```text
evidence: EvidenceRecord[]
```

最小证据模型：

```text
EvidenceRecord
  type
  source
  confidence
  message
```

`EvidenceType` 首批覆盖：

- `ServiceReference`
- `ScheduledTaskReference`
- `StartupReference`
- `UninstallRegistryReference`
- `RunningProcessReference`
- `PathEnvironmentReference`
- `ShortcutReference`
- `FileSignature`
- `InstalledApplication`
- `MicrosoftStorePackage`
- `WindowsComponent`
- `KnownCleanupRule`
- `ProtectedPathRule`
- `Metadata`
- `CollectionFailure`

`RiskAssessment` 继续负责决策输出；`EvidenceRecord` 负责可追溯事实。当前 scanner 生成空 evidence 列表，后续 Windows 适配器逐步填充。

## 隐私

`privacyMode=redacted` 必须处理 evidence 中的 `source` 和 `message`。已知报告路径替换为同一份报告内稳定 token，其他 Windows 路径使用通用 `[redacted-path]` 占位。

## 后果

优点：

- 后续服务、计划任务、启动项、注册表和进程引用可以接入同一报告结构。
- 风险理由不再承担结构化证据职责。
- 证据收集失败可以用 `CollectionFailure` 表达，避免误降风险。

代价：

- schema `1.3` 对严格 JSON 消费者是兼容性事件。
- evidence 文本可能包含路径、命令行或注册表信息，必须经过隐私模式处理。
- Windows 适配器还需要单独实现和测试。
