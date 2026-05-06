# ADR 0025: 只读 preflight CLI

日期：2026-05-06

## 状态

Accepted

## 背景

Core 已有隔离执行前校验清单，但用户还没有 CLI 入口验证一个 cleanup plan 和 restore metadata 是否满足未来执行器门禁。该入口必须保持只读，不能被误解为隔离执行。

## 决策

新增只读 CLI 命令：

```powershell
preflight --plan <cleanup-plan.json> --metadata <restore-metadata.json> [--manual-confirmation] [--format json|markdown] [--output <FILE>]
```

命令行为：

- 只读取显式传入的 cleanup plan JSON 和 restore metadata JSON。
- 输出 preflight checklist JSON 或 Markdown。
- 不重新扫描、不加载 CleanerML、不生成计划。
- 不创建隔离目录、不写 `.restore.json`、不追加 operation log、不移动或删除文件。
- `isExecutable=false` 仍返回 exit code `0`，因为这是成功完成校验后的结果。
- 参数错误、文件缺失、JSON 不能解析或输出路径不安全时返回 exit code `2`。

`quarantine` 和 `restore` 执行命令继续被拒绝。

## 理由

preflight 是未来执行器之前的诊断/门禁输出，不是执行器本身。把它作为独立只读命令，可以让后续 UI 或 CLI 在真正执行前复用同一 checklist。

## 后果

优点：

- 用户可以验证计划和恢复元数据是否满足执行前条件。
- checklist 可输出到 stdout 或安全的新文件。
- Markdown 输出便于人工审阅。

限制：

- 当前不支持读取目录或批量 metadata。
- 当前不写 operation log。
- 当前不提供真实隔离或恢复命令。
