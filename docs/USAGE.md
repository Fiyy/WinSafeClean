# 使用示例

WinSafeClean 的 `scan`、`plan` 和 `preflight` 仍是只读命令。`quarantine` 和 `restore` 是带强确认的真实文件移动命令；项目仍不提供删除、清理、修复或注册表修改命令。

## WPF UI

启动 UI：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Ui
```

当前 UI 支持：

- 打开 scan report JSON，查看大小、风险、类型、原因、阻断理由和 evidence。
- 打开 cleanup plan JSON，查看动作、风险、原因和只读隔离预览路径。
- 打开 preflight checklist JSON，查看可执行状态、检查状态汇总和检查消息。
- 构建 `scan`、`plan` 和 `preflight` 命令文本，便于复制到终端执行。

UI 不会执行命令，不会移动或删除文件，也不会构建 `quarantine`、`restore`、`delete` 或 `clean` 命令。

## 本地发布

```powershell
pwsh -NoProfile -File .\scripts\publish.ps1 -Restore
```

默认输出：

- `artifacts\publish\WinSafeClean.Cli`
- `artifacts\publish\WinSafeClean.Ui`

可选参数：

- `-Runtime win-x64|win-arm64`
- `-Configuration Release|Debug`
- `-SelfContained`
- `-SkipTests`
- `-OutputRoot <PATH>`

发布脚本只运行测试和 `dotnet publish`，不会运行发布后的程序，不会请求管理员权限，也不会执行扫描、隔离、恢复、删除或清理命令。`OutputRoot` 不能位于 Windows 目录、源码目录、测试目录、文档目录或 `.tools` 工具链目录内。

## 扫描

查看版本：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- --version
```

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --format markdown
```

递归扫描需要显式开启：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --recursive --max-items 200
```

分享报告前建议使用 redacted 模式：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- scan --path C:\Temp --privacy redacted --output .\scan-redacted.json
```

## 只读计划

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- plan --path C:\Temp --format markdown
```

计划动作含义：

- `Keep`：保守保留，不进入清理候选。
- `ReportOnly`：仅报告信息，不建议清理。
- `ReviewForQuarantine`：只表示可进入人工审阅的隔离候选，不会自动隔离。

当某个条目进入 `ReviewForQuarantine` 时，JSON/Markdown 计划会包含只读 `quarantinePreview`，展示拟议隔离路径、恢复元数据路径和人工确认警告。该预览不会创建隔离目录，不会写入恢复元数据，也不会移动或删除文件。使用 `--privacy redacted` 时，隔离根、隔离路径、恢复元数据路径和 restore plan id 也会脱敏。

## CleanerML

WinSafeClean 可以显式加载用户提供的 CleanerML 文件或目录顶层 `.xml` 文件，把规则候选转换为 `KnownCleanupRule` evidence。

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- plan --path C:\Temp --cleanerml .\rules\example.xml
```

安全边界：

- 不自动下载规则。
- 不执行 CleanerML 的 delete、truncate、shred、winreg 或 process 动作。
- 不递归读取规则目录。
- CleanerML 命中只能作为候选证据，不能单独证明删除安全。

## 输出文件

`--output` 只允许写入不存在的新文件：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- plan --path C:\Temp --output .\plan.json
```

如果输出路径已存在、父目录不存在，或目标位于受保护 Windows 路径，命令会拒绝执行。

## 执行前校验

`preflight` 读取已有的 cleanup plan JSON 和 restore metadata JSON，输出只读 checklist：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- preflight --plan .\plan.json --metadata .\abcd.restore.json --manual-confirmation --format markdown
```

该命令不会重新扫描、不会写恢复元数据、不会追加日志、不会创建隔离目录，也不会执行隔离或恢复。`isExecutable=false` 是校验结果，不是命令失败。

## 隔离

`quarantine` 是真实写操作，会移动文件，并写入 restore metadata。必须同时提供人工确认和危险操作确认：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- quarantine --plan .\plan.json --metadata .\abcd.restore.json --manual-confirmation --i-understand-this-moves-files --operation-log .\operations.jsonl
```

安全边界：

- 执行前会重新运行 preflight checklist。
- 不通过 preflight 不会移动文件。
- 写出的 restore metadata schema 为 `1.1`，包含源文件 SHA256 内容 hash。
- 只支持文件隔离，不支持目录隔离。
- 不覆盖已有隔离目标或 restore metadata。
- `delete` 和 `clean` 仍未开放。

## 恢复

`restore` 是真实写操作，会把隔离文件移回 restore metadata 记录的原始路径。必须同时提供人工确认和危险操作确认：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- restore --metadata .\abcd.restore.json --manual-confirmation --i-understand-this-moves-files --operation-log .\operations.jsonl
```

旧版 metadata 缺少内容 hash 时，需要额外确认：

```powershell
.\.tools\dotnet\dotnet.exe run --project .\src\WinSafeClean.Cli -- restore --metadata .\legacy.restore.json --manual-confirmation --i-understand-this-moves-files --allow-legacy-metadata-without-hash
```

安全边界：

- 只接受 full-fidelity restore metadata，拒绝 redacted metadata。
- metadata 带 SHA256 内容 hash 时，恢复前必须与隔离文件当前内容匹配。
- metadata 缺少内容 hash 时默认拒绝恢复，除非显式传入 `--allow-legacy-metadata-without-hash`。
- 原始路径已经存在时不会覆盖。
- 隔离文件缺失时不会创建占位文件。
- 只支持文件恢复，不支持目录恢复。
- `--operation-log` 只追加 JSONL，不会覆盖已有日志。
