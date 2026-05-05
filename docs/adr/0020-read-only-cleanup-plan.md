# ADR 0020: 只读清理计划草案

日期：2026-05-06

## 状态

Accepted

## 背景

WinSafeClean 的目标是先解释和评估，再考虑清理。Phase 3 需要清理计划，但当前仍不能执行删除或隔离。因此需要一个只读计划草案模型，将扫描报告转换为可审阅的预览动作。

## 决策

在 Core 中新增 `Planning` 模块：

- `CleanupPlan`
- `CleanupPlanItem`
- `CleanupPlanAction`
- `CleanupPlanGenerator`

首批 action 只包含：

- `Keep`
- `ReportOnly`
- `ReviewForQuarantine`

生成规则：

- `Blocked` 一律 `Keep`。
- `HighRisk` 一律 `Keep`。
- 存在服务、计划任务、启动项、卸载注册表、运行进程或已安装应用归属 evidence 时一律 `Keep`。
- 存在 `CollectionFailure` evidence 时一律 `Keep`。
- 只有 `LowRisk` 或 `SafeCandidate` 且命中 `KnownCleanupRule` 时，才进入 `ReviewForQuarantine`。
- 其他情况 `ReportOnly`。

## 理由

计划模型用于解释“为什么不能清理”或“为什么只进入人工审阅候选”，不是执行器。将计划动作限制在预览语义，可以先稳定风险边界，再设计隔离和恢复。

## 后果

优点：

- 建立 Phase 3 的只读计划基础。
- 主动引用和证据失败不会进入清理候选。
- CleanerML 规则只能推动人工审阅，不会直接触发删除。

限制：

- 当前没有执行器、隔离区或回滚。
- 当前没有计划 JSON/Markdown serializer。
- `ReviewForQuarantine` 只是候选状态，不表示可自动隔离。
