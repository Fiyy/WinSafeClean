# ADR 0023: 恢复元数据和操作日志模型

日期：2026-05-06

## 状态

Accepted

## 背景

只读隔离预览已经能生成拟议隔离路径和恢复元数据路径。下一步需要为未来执行器准备恢复依据和审计日志，但当前仍不能移动、删除、复制文件，也不能写入 `.restore.json`。

## 决策

新增 `WinSafeClean.Core.Quarantine` 模块：

- `RestoreMetadata`
- `RestoreMetadataGenerator`
- `RestoreMetadataJsonSerializer`
- `QuarantineOperationLog`
- `QuarantineOperationLogEntry`
- `QuarantineOperationType`
- `QuarantineOperationStatus`
- `QuarantineOperationLogJsonSerializer`

恢复元数据 schema 从 `1.0` 起步，引用 `CleanupPlan` schema `0.2` 的 `quarantinePreview` 字段。恢复元数据记录：

- 原始路径
- 拟议隔离路径
- 恢复元数据路径
- restore plan id
- 计划时风险等级和动作
- 计划原因与警告
- 是否要求人工确认
- 是否为 redacted 元数据

操作日志 schema 从 `1.0` 起步，采用事件式模型。当前只定义字段和序列化，不接执行器。

## 理由

恢复元数据是未来恢复操作的依据，操作日志用于审计和诊断。两者不应推动 `CleanupPlan` schema 再升级，也不应让只读 `plan` 命令产生任何真实文件。

## 后果

优点：

- 恢复 metadata 和 operation log 有独立 schema fixture。
- `RestoreMetadataGenerator` 只从已有 `quarantinePreview` 生成内存模型。
- 未来执行器可以基于同一格式写入元数据和追加日志。

限制：

- 当前不创建 `.restore.json` 文件。
- 当前不追加 operation log。
- 当前不计算内容 hash；实际执行阶段再补移动前后校验。
