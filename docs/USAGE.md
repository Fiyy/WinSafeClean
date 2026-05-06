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
