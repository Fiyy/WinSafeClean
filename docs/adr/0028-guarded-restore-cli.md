# ADR 0028: 带强确认的 restore CLI

日期：2026-05-07

## 状态

Accepted

后续变更：ADR 0029 已为执行后 restore metadata 增加 SHA256 内容 hash，并让 `restore` 在 metadata 带 hash 时执行恢复前校验。

## 背景

`quarantine` 已经能够在通过 preflight 后把文件移动到隔离路径，并写入 restore metadata。为了形成可恢复闭环，需要提供一个最小恢复入口，把隔离文件移回 metadata 记录的原始路径。

恢复同样是写操作，错误恢复可能覆盖用户新文件或把脱敏/伪造元数据当成真实路径使用，因此必须保留强门禁。

## 决策

新增 Core 恢复执行器和 CLI 命令：

```powershell
restore --metadata <restore-metadata.json> --manual-confirmation --i-understand-this-moves-files [--operation-log <FILE>] [--format json|markdown] [--output <FILE>]
```

门禁：

- 必须显式传入 `--manual-confirmation`。
- 必须显式传入 `--i-understand-this-moves-files`。
- 必须提供已存在的 restore metadata JSON。
- redacted restore metadata 一律拒绝。
- 原始路径已存在时拒绝恢复，避免覆盖。
- 隔离路径缺失时拒绝恢复。
- 目录恢复暂不支持。
- `--operation-log` 可追加 JSONL 日志，不能指向受保护 Windows 路径。

执行顺序：

1. 读取 restore metadata。
2. 验证 metadata 不是 redacted，且需要的确认已提供。
3. 验证隔离文件存在、原始路径不存在。
4. 创建原始路径父目录和 operation log 父目录。
5. 如提供 operation log，先追加 `RestoreStarted`；失败则不移动。
6. 使用不覆盖语义移动隔离文件到原始路径。
7. 如提供 operation log，追加 `RestoreCompleted`；失败只返回 warning。

`delete` 和 `clean` 仍不开放。

## 理由

restore metadata 已记录恢复所需的原始路径、隔离路径、风险等级、计划动作、原因和警告。恢复命令只依赖 metadata，可以在没有原始 cleanup plan 的情况下执行回滚，但必须拒绝 redacted metadata 并禁止覆盖已有原始路径。

## 后果

优点：

- 隔离流程具备最小回滚能力。
- 双重确认降低误触发风险。
- redacted metadata 不会进入真实恢复路径。
- operation log 可记录 started/completed 事件。

限制：

- 当前只支持文件恢复，不支持目录恢复。
- 当前不会删除 restore metadata。
- 当前只在 metadata 带 SHA256 时执行内容校验；旧 metadata 缺少 hash 时需要额外 legacy 确认，且仍只能依赖路径和人工确认。
