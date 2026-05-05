# ADR 0007: 报告隐私模式与 schema 1.2

日期：2026-05-05

## 状态

Accepted

## 背景

Phase 1 报告已经包含完整路径和 `lastWriteTimeUtc`。这些信息能帮助解释文件，但也会暴露用户名、目录结构、文件名和活动时间线。用户可能需要把报告发给他人协助判断，因此需要一个只读、可测试的隐私输出模式。

## 决策

报告 schema 从 `1.1` 演进到 `1.2`，新增顶层字段：

- `privacyMode`: `Full` 或 `Redacted`

CLI 新增：

```text
--privacy full|redacted
```

默认值为 `full`，保持本机诊断报告的精确性。

`redacted` 模式：

- 将每个报告内路径替换为稳定 token，如 `[redacted-path-0001]`。
- 同一路径在同一份报告中使用同一个 token。
- 将 `lastWriteTimeUtc` 置为 `null`；Markdown 显示为 `-`。
- 替换 `evidence`、`reasons` 和 `blockers` 中已知路径。
- 保留 `itemKind`、`sizeBytes`、风险等级、置信度和建议动作。

## 理由

`--privacy full|redacted` 比 `--redact-paths` 更准确，因为隐私风险不只来自 path 字段，也来自时间戳和自由文本解释。

redacted token 只保证单份报告内的关联性，不跨报告稳定，避免形成额外追踪标识。

## 后果

优点：

- 用户可以分享低敏报告，减少直接暴露本机路径。
- 风险判断仍可读，便于远程协助。
- 实现保持只读，不影响扫描目标。

代价：

- `redacted` 不是加密或匿名化，不能保证完全去标识。
- Markdown 或 JSON 的严格消费者需要理解 schema `1.2` 和 `privacyMode`。
- `redacted` 报告不能作为精确定位文件的执行依据；后续清理计划必须使用 full 报告或本地状态重新确认。

后续 ADR 0009 将 schema 演进到 `1.3`，新增报告项 evidence；redacted 模式同样处理 evidence 中的路径文本。
