# ADR 0004: Phase 1 CLI 只做只读扫描报告

日期：2026-05-05

## 状态

Accepted

## 背景

Phase 1 的目标是只读核心 MVP。CLI 是用户最容易直接执行的入口，如果过早引入 `clean`、`delete`、`quarantine`、`fix` 等操作，会破坏项目的安全边界。

## 决策

Phase 1 CLI 只支持：

```text
scan --path <PATH> [--format json|markdown] [--privacy full|redacted] [--output <FILE>] [--max-items <N>] [--recursive|--no-recursive]
```

`--output` 只允许创建不存在的新报告文件。它不能覆盖已有文件或目录，也不能写入受保护 Windows 路径。

CLI 必须拒绝执行型命令和选项：

- `delete`
- `clean`
- `quarantine`
- `restore`
- `plan`
- `--delete`
- `--fix`
- `--quarantine`

CLI 复用 Core 的 `ScanReport`、`ScanReportItem`、`PathRiskClassifier`、JSON serializer 和 Markdown serializer。

`--privacy redacted` 只改变报告输出视图，不修改扫描目标，也不启用任何清理能力。

## 理由

CLI 首版的价值是验证报告管线和风险模型，而不是释放空间。把执行能力推迟到 Phase 3 可以避免用户误以为当前工具已经具备完整清理安全保障。

## 后果

优点：

- 保持 Phase 1 安全边界清晰
- 更容易用 TDD 覆盖参数和只读行为
- 后续扫描器可以在不改变 CLI 安全语义的前提下逐步增强
- 报告文件写入受到覆盖保护，避免把 `--output` 变成隐式修改扫描目标的能力

代价：

- CLI 当前只报告显式传入路径本身，不做完整目录遍历
- 暂时不能直接释放磁盘空间
