# ADR 0014: 卸载注册表证据提供器

日期：2026-05-06

## 状态

Accepted

## 背景

Windows 应用通常在 Uninstall 注册表项中记录卸载命令、静默卸载命令、图标路径和安装目录。这些信息可以帮助判断文件是否属于已安装应用，或是否被卸载流程直接引用。

## 决策

`UninstallRegistryEvidenceProvider` 通过可注入的 `IWindowsUninstallEntrySource` 获取卸载项记录。默认实现 `RegistryWindowsUninstallEntrySource` 只读读取：

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

首批读取字段：

- `DisplayName`
- `InstallLocation`
- `UninstallString`
- `QuietUninstallString`
- `DisplayIcon`

如果目标路径与 `UninstallString`、`QuietUninstallString` 或 `DisplayIcon` 解析出的可执行路径匹配，输出 `EvidenceType.UninstallRegistryReference`，confidence 为 `0.9`。如果目标路径位于 `InstallLocation` 下，输出 `EvidenceType.InstalledApplication`，confidence 为 `0.8`。

## 理由

卸载命令和图标路径是明确的直接引用；安装目录是归属证据，但不代表文件可删除。因此两类 evidence 分开输出，避免把“属于某应用”误解成“安全删除”。

## 后果

优点：

- 可以识别卸载入口直接引用的文件。
- 可以识别目标文件位于已安装应用目录下。
- 32-bit 和 64-bit 常见卸载注册表位置均被覆盖。

限制：

- 当前不解析 MSI product database。
- 当前不读取 Appx/MSIX package registry。
- `InstallLocation` 只作为归属证据，不作为删除建议。
