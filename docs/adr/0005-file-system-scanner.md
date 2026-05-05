# ADR 0005: Phase 1 文件扫描器只做非递归只读枚举

日期：2026-05-05

## 状态

Accepted

## 背景

Phase 1 需要从“评估单个显式路径”推进到“生成扫描报告”。目录递归扫描、目录大小汇总和完整元数据读取都可能让首版变慢、难测，并让用户误以为工具已经具备完整清理判断能力。

## 决策

新增 `WinSafeClean.Core.FileInventory.FileSystemScanner`：

```text
Scan(path, options) -> IReadOnlyList<ScanReportItem>
```

扫描器通过 `IFileSystem` 探针访问路径规范化、存在性判断、目录枚举和文件长度读取。默认运行时使用系统文件系统适配器；测试可以注入探针稳定模拟权限、路径过长、IO 和安全策略异常。

当前行为：

- 文件：返回单个 item，大小为文件长度。
- 目录：只枚举直接子项，不递归。
- 文件项：`itemKind = File`，记录文件长度和可读取的 `lastWriteTimeUtc`。
- 目录项：`itemKind = Directory`，大小暂记为 `0 B`，记录可读取的 `lastWriteTimeUtc`。
- 缺失路径：返回单个 `Unknown / ReportOnly` item。
- 缺失但受保护的 Windows 路径：保留 `Blocked` 风险。
- `MaxItems`：限制实际枚举和返回项数量。
- 非法路径语法：降级为单个 `Unknown / ReportOnly` item。
- 目录枚举或文件元数据读取失败：降级为单个 `Unknown / ReportOnly` item。
- 时间戳读取失败：只将 `lastWriteTimeUtc` 置为 `null`，不让扫描失败。
- 结果路径：输出完整路径，便于报告稳定。

CLI 接入：

```text
scan --path <PATH> [--format json|markdown] [--privacy full|redacted] [--output <FILE>] [--max-items <N>] [--no-recursive]
```

`--no-recursive` 当前是显式安全开关；默认也保持非递归。`--recursive` 暂不支持，在设计遍历顺序、全局 `MaxItems`、重解析点、权限降级和取消机制前必须保持拒绝状态。

## 理由

非递归只读扫描足以验证报告管线和风险模型，同时避免首版在大目录上产生长时间扫描、权限噪音或用户误解。

## 后果

优点：

- 行为稳定，测试容易覆盖
- 不需要管理员权限
- 不读取文件内容，不修改扫描目标
- 目录不会被递归汇总拖慢

代价：

- 暂时无法回答完整目录总大小
- 暂时无法发现深层目录中的候选项
- 后续需要单独设计递归扫描、跳过规则和取消机制
