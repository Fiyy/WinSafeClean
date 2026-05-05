# ADR 0006: Phase 1 报告项记录类型与有限时间元数据

日期：2026-05-05

## 状态

Accepted

## 背景

当前 `ScanReportItem` 只包含 `Path`、`SizeBytes` 和 `Risk`。文件、目录、缺失路径和不可访问路径在报告中都可能出现，其中目录大小在 Phase 1 暂记为 `0 B`。如果没有显式类型字段，报告消费者无法区分 `0 B` 文件、目录和未知项。

风险模型后续还需要“长期未修改”一类证据，但 Phase 1 必须保持只读，不读取文件内容，也不扩展到用户行为追踪。

## 决策

Phase 1 的下一次报告 schema 扩展应加入：

- `itemKind`：取值为 `File`、`Directory`、`Unknown`。
- `lastWriteTimeUtc`：可空字段；读取失败时保持缺失或 `null`，不得让扫描失败。

Phase 1 不加入：

- `createdTimeUtc`：Windows 创建时间在复制、解压、同步和恢复场景下语义不稳定。
- `lastAccessTimeUtc`：Windows last access 可能关闭或延迟更新，并且更接近用户行为轨迹，隐私代价高。

本 ADR 记录 schema 方向。字段实现需要通过后续 TDD 任务落地，并同步更新 JSON/Markdown serializer 与 CLI 集成测试。

## 理由

`itemKind` 是低隐私、强语义字段，能避免报告消费者误读目录大小。

`lastWriteTimeUtc` 与“长期未修改”的风险判断相关，且可以通过只读元数据获取。它只作为报告证据，不触发删除、隔离或修复。

暂缓 `createdTimeUtc` 和 `lastAccessTimeUtc` 可以减少误判来源，并控制报告中暴露的用户活动信息。

## 后果

优点：

- 报告项语义更明确。
- 后续风险模型可以基于有限元数据做解释。
- 仍然保持 Phase 1 只读边界。

代价：

- 报告 schema 会从 `1.0` 演进，需要明确兼容策略。
- Markdown 和 JSON 输出都需要新增测试。
- 时间戳会提高报告敏感度，后续需要设计脱敏或摘要模式。
