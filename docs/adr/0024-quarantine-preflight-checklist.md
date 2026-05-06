# ADR 0024: 隔离执行前校验清单

日期：2026-05-06

## 状态

Accepted

## 背景

恢复元数据和操作日志模型已经存在，但真实隔离执行器仍未实现。为了避免未来执行器直接信任计划或元数据，需要先建立执行前安全门禁。

## 决策

新增 `QuarantinePreflightValidator` 和 preflight checklist schema `1.0`。校验器只返回内存 checklist，不创建目录、不写恢复元数据、不移动或删除文件。

首批阻断条件：

- `CleanupPlan` / restore metadata schema 不受支持。
- restore metadata 为 redacted。
- 路径或 restore plan id 含 redacted token。
- restore metadata 无法匹配 `CleanupPlan` 中的 `quarantinePreview`。
- 计划动作不是 `ReviewForQuarantine`。
- 隔离 preview 或 metadata 不要求人工确认。
- 风险等级不是 `LowRisk` 或 `SafeCandidate`。
- 未提供人工确认。
- 隔离根位于受保护 Windows 路径。
- 隔离路径或恢复元数据路径逃逸隔离根。
- 源路径已经在隔离根内。
- 源路径、隔离目标路径、恢复元数据路径互相冲突。

## 理由

执行器应该是最后一步。preflight checklist 先把不可执行条件结构化，可以让未来 CLI 或 UI 在真正移动文件前展示明确阻断原因。

## 后果

优点：

- 执行前安全规则有单元测试和 JSON fixture。
- 未来执行器必须显式通过 checklist。
- 当前仍不会执行任何文件系统变更。

限制：

- 当前不读取磁盘状态。
- 当前不检查目标文件是否存在、锁定、ACL 或剩余空间。
- 当前不接 CLI `preflight` 命令。
