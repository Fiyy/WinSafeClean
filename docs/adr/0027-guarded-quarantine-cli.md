# ADR 0027: 带强确认的 quarantine CLI

日期：2026-05-06

## 状态

Accepted

## 背景

Core 已有最小隔离执行器，但 CLI 之前只提供只读命令。为了让 MVP 具备完整的“计划 -> 校验 -> 隔离”闭环，需要开放真实隔离入口，同时保留强门禁。

## 决策

新增 CLI 命令：

```powershell
quarantine --plan <cleanup-plan.json> --metadata <restore-metadata.json> --manual-confirmation --i-understand-this-moves-files [--operation-log <FILE>] [--format json|markdown] [--output <FILE>]
```

门禁：

- 必须显式传入 `--manual-confirmation`。
- 必须显式传入 `--i-understand-this-moves-files`。
- 必须提供 cleanup plan JSON 和 restore metadata JSON。
- 执行器仍会运行 preflight checklist；不通过则不移动。
- `--operation-log` 可追加 JSONL 日志，不能指向受保护 Windows 路径。

`delete`、`clean`、`restore` 仍不开放。`scan`、`plan` 和 `preflight` 仍是只读命令。

## 理由

隔离是项目的第一个真实写操作，必须比普通命令更难误触发。双重确认加 preflight 可以让用户明确知道该操作会移动文件，并让执行器在移动前再次验证计划、metadata 和路径边界。

## 后果

优点：

- MVP 具备真实文件隔离能力。
- CLI 输出结构化执行结果。
- 可选 operation log 记录 started/completed 事件。

限制：

- 当前只支持文件隔离，不支持目录隔离。
- 当前没有 restore CLI。
- 当前不执行 delete/clean。
