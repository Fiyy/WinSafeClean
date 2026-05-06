# ADR 0022: 只读隔离预览

日期：2026-05-06

## 状态

Accepted

## 背景

Phase 3 需要为未来隔离和恢复执行器准备模型，但当前项目仍处于只读阶段。已有 `CleanupPlan` 可以把条目标记为 `ReviewForQuarantine`，但缺少拟议隔离路径和恢复元数据路径。

## 决策

将 `CleanupPlan` schema 升级到 `0.2`，并为 `ReviewForQuarantine` 项新增 `quarantinePreview`：

- `originalPath`
- `proposedQuarantinePath`
- `restoreMetadataPath`
- `restorePlanId`
- `requiresManualConfirmation`
- `warnings`

新增 `QuarantinePathPlanner`，只生成字符串路径，不创建目录、不写恢复元数据、不移动或删除文件。

路径生成规则：

- 隔离根默认规划为 LocalAppData 下的 `WinSafeClean\Quarantine`。
- 隔离根不能位于受保护 Windows 路径。
- 拟议隔离路径使用原路径的稳定 hash 和清理后的文件名，避免直接拼接完整原路径。
- 只有 `ReviewForQuarantine` 项获得 preview，`Keep` 和 `ReportOnly` 不生成 preview。
- `plan --privacy redacted` 会脱敏隔离根、隔离路径、恢复元数据路径和 restore plan id。

## 理由

隔离是高风险操作。先把隔离目标、恢复元数据路径和人工确认要求放进只读计划，可以让后续执行器按同一数据模型实现，同时避免当前 CLI 暗示已经具备执行能力。

## 后果

优点：

- `plan` 输出能展示未来隔离位置和恢复元数据位置。
- 测试锁住“不创建隔离目录”的边界。
- 脱敏计划不会重新泄露 LocalAppData 或用户目录。
- `quarantine` 和 `restore` 仍是被拒绝的执行命令。

限制：

- 当前不写入恢复元数据文件。
- 当前不移动、复制、删除或隔离任何文件。
- 当前不计算文件 hash；执行器阶段再设计实际校验字段。
