# ADR 0018: 扫描取消机制

日期：2026-05-06

## 状态

Accepted

## 背景

递归扫描、证据收集和后续规则解析都可能运行较长时间。用户需要能取消扫描，且取消不能表现为清理失败或部分删除，因为当前项目仍是只读报告器。

## 决策

`FileSystemScanOptions` 新增 `CancellationToken`。Core scanner 在以下位置检查取消：

- 扫描入口。
- 目录直接子项枚举循环。
- 递归目录枚举前。
- 递归目录项处理循环。

`ScanReportGenerator` 在为扫描项附加 evidence 前检查取消。

CLI 将 `OperationCanceledException` 转换为：

- exit code `130`
- stderr `Scan cancelled.`

`Program.cs` 监听 `Console.CancelKeyPress`，拦截 Ctrl+C 并取消 token。

## 理由

使用 .NET 标准 `CancellationToken` 可以让 Core、CLI 和未来 UI 共享同一取消模型。取消通过异常向上传播，避免返回看似完整但实际被截断的报告。

## 后果

优点：

- 长时间扫描可以被用户取消。
- Core 不依赖 CLI 或 UI。
- CLI 取消语义符合常见 Ctrl+C 退出码。

限制：

- 当前取消后不输出部分报告。
- evidence provider 本身还没有 token 参数，取消只发生在调用 provider 前，而不是 provider 内部。
