# ADR 0019: Evidence provider 内部取消

日期：2026-05-06

## 状态

Accepted

## 背景

扫描取消机制已经覆盖文件系统枚举和 report generator，但 evidence provider 内部读取服务、计划任务、注册表、进程或 CleanerML 规则时也可能耗时。取消不能被误记为 `CollectionFailure`。

## 决策

`IFileEvidenceProvider.CollectEvidence` 新增可选 `CancellationToken` 参数：

```csharp
IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default);
```

所有 provider 保持 `CollectEvidence(path)` 兼容调用方式，同时在枚举循环中检查 token。

`CompositeFileEvidenceProvider` 和 `ScanReportGenerator` 遇到 `OperationCanceledException` 时直接重新抛出，不生成 `CollectionFailure` evidence。

## 理由

取消是用户主动终止，不是证据读取失败。把取消记录为 failure 会让报告语义错误，并可能误导风险判断。

## 后果

优点：

- evidence 收集可以响应扫描取消。
- 取消语义贯穿 Core、CLI、Windows provider 和 CleanerRules provider。
- 保持现有调用兼容性。

限制：

- 底层 Windows API 调用本身无法被中途抢占，只能在 provider 控制的循环边界响应取消。
