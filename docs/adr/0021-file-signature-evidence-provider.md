# ADR 0021: 文件签名证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

WinSafeClean 需要解释文件来源，但不能把“文件有签名”误当成“文件可以删除”。Windows Authenticode 签名可以提供发布者、证书颁发者和指纹等来源线索，适合作为 evidence，但不适合作为清理许可。

## 决策

新增 Windows 文件签名 evidence provider：

- `FileSignatureEvidenceProvider`
- `IWindowsFileSignatureSource`
- `AuthenticodeFileSignatureSource`
- `WindowsFileSignatureRecord`

默认 provider factory 接入 `FileSignatureEvidenceProvider`。默认实现仅在 Windows 上读取 Authenticode 证书元数据；非 Windows 环境返回空 evidence。

读取边界：

- 只读取用户正在扫描的普通文件路径。
- 缺失路径、目录、无签名文件、不可访问文件或无效签名读取失败时返回空 evidence。
- 取消请求通过 `OperationCanceledException` 传播，不转换为 `CollectionFailure`。
- evidence message 不重复目标路径，只包含签名主题、颁发者和 thumbprint。

`EvidenceType.FileSignature` 的 confidence 设置为 `0.6`，表示来源元数据而非安全结论。

## 理由

签名可以帮助用户判断文件可能来自哪个发布者，但签名证书可能过期、吊销、被滥用，或仅证明来源而不证明文件可再生。因此它不能降低风险，也不能触发 `ReviewForQuarantine`。

## 后果

优点：

- 报告可以解释签名来源。
- 缺失、无签名或读取失败不会中断扫描。
- 默认 CLI 扫描会包含签名 evidence。

限制：

- 当前不验证证书链信任状态。
- 当前不检查吊销状态。
- `FileSignature` 只作为解释性 evidence，不参与清理许可。
