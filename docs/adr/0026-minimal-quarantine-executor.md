# ADR 0026: 最小隔离执行器

日期：2026-05-06

## 状态

Accepted

## 背景

项目已经有 cleanup plan、quarantine preview、restore metadata、operation log 和 preflight checklist。下一步需要 Core 层最小真实隔离执行器，但 CLI 仍不开放 `quarantine` 或 `restore`。

## 决策

新增 Core 隔离执行器：

- `IQuarantineFileSystem`
- `SystemQuarantineFileSystem`
- `QuarantineExecutionOptions`
- `QuarantineExecutionResult`
- `QuarantineExecutor`

执行器必须先调用 `QuarantinePreflightValidator`。若 checklist 不可执行，执行器返回失败结果且不调用任何写入或移动操作。

首批只支持文件隔离，不支持目录。执行顺序：

1. 运行 preflight。
2. 确认 source 是存在的文件，target 和 restore metadata 文件不存在。
3. 创建隔离目标和 restore metadata 的父目录。
4. 使用 `CreateNew` 语义写 restore metadata。
5. 使用不覆盖语义移动 source 到 quarantine target。
6. 返回内存 operation log。

如果 restore metadata 写入失败，source 不移动。如果 move 失败，执行器会删除刚写入的 restore metadata。CLI 仍不接入真实隔离。

## 理由

先写 restore metadata 再移动文件，可以避免出现“文件已隔离但没有恢复依据”的状态。执行器通过 `IQuarantineFileSystem` 抽象，测试可以模拟写入失败、移动失败、目标冲突和回滚。

## 后果

优点：

- Core 已具备最小文件隔离能力。
- preflight 不通过时零写入。
- restore metadata 写入失败不会移动源文件。
- move 失败会清理 metadata。

限制：

- 当前不支持目录隔离。
- 当前不写 operation log 文件，只在结果中返回内存 log。
- 当前没有 CLI 执行入口，`quarantine` 和 `restore` 仍被拒绝。
