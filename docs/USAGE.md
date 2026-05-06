# 使用示例

WinSafeClean 当前仍是只读工具。`scan` 输出扫描报告，`plan` 输出只读清理计划预览；两者都不会删除、隔离、修复或修改注册表。

## 扫描

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
- 只支持文件隔离，不支持目录隔离。
- 不覆盖已有隔离目标或 restore metadata。
- `delete`、`clean` 和 `restore` 仍未开放。
