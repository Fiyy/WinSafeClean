# ADR 0011: 服务 ImagePath 证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

Phase 2 的第一类真实 Windows 关系证据是“文件是否被 Windows 服务引用”。服务的 `ImagePath` 可能包含引号、参数、环境变量、设备路径前缀或未加引号的可执行文件路径。读取逻辑必须保持只读，并且可测试。

## 决策

`ServiceEvidenceProvider` 通过可注入的 `IWindowsServiceSource` 获取服务记录。默认实现 `RegistryWindowsServiceSource` 只读访问：

- `HKLM\SYSTEM\CurrentControlSet\Services`
- `DisplayName`
- `ImagePath`

匹配前使用 `ServiceImagePathParser` 提取服务可执行文件路径，处理：

- 带引号路径和后续参数
- 未加引号路径和后续参数
- `%SystemRoot%` 等环境变量
- `\SystemRoot\...`
- `\??\...`

匹配成功时输出 `EvidenceType.ServiceReference`，source 使用服务名和可选显示名，confidence 为 `0.95`。

## 理由

直接读取注册表可以获得服务原始 `ImagePath`，避免 WMI 或 ServiceController 抽象隐藏参数细节。服务来源接口可注入，测试不依赖真实注册表，也不会修改系统状态。

## 后果

优点：

- 可以发现目标文件被 Windows 服务直接引用的关系。
- provider 在非 Windows 环境下默认返回空 evidence，避免平台 API 警告和运行时失败。
- 解析逻辑有独立单元测试覆盖常见 `ImagePath` 形态。

限制：

- 当前只匹配服务 `ImagePath` 中的可执行文件，不解析 svchost 托管服务的 `ServiceDll`。
- 当前不读取驱动服务之外的更深层注册表参数。
- 真实注册表读取仍依赖当前进程权限；读取失败由组合 evidence provider 降级记录。
