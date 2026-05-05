# ADR 0003: 禁止路径识别必须先做 Windows 语义规范化

日期：2026-05-05

## 状态

Accepted

## 背景

禁止目录保护不能只依赖字符串前缀。Windows 路径可能通过 `..`、extended-length 前缀、本机 admin share、重复分隔符或非 C 盘 Windows 根目录表示同一类敏感位置。

## 决策

路径风险分类器在匹配禁止目录前必须规范化路径，并且保护任意根目录下的 `Windows\...` 系统路径，而不是只保护 `C:\Windows\...`。

当前覆盖：

- `Windows\Installer`
- `Windows\WinSxS`
- `Windows\System32\DriverStore`
- `Windows\System32`
- `Windows\SysWOW64`
- `Windows\SystemApps`
- `Windows\servicing`
- `Windows\INF`

## 理由

清理工具的错误方向应偏向保守。误把敏感系统目录判为 Unknown 或 LowRisk 的代价高于把少量相似目录判为需要人工复核。

## 后果

优点：

- 降低路径绕过风险
- 支持离线 Windows 目录或非 C 盘系统目录的保守识别
- 明确 DriverStore 的独立保护意图

代价：

- 某些非系统用途的 `Windows` 目录可能被保守阻断
- 后续需要在报告中解释阻断原因，避免用户误解为扫描失败

