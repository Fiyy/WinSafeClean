# 实现框架

## 总体架构

项目采用分层架构，核心判断逻辑与 Windows 适配器隔离。

```text
WinSafeClean
├─ Core
│  ├─ FileInventory
│  ├─ Ownership
│  ├─ Evidence
│  ├─ Risk
│  ├─ Plan
│  └─ Reporting
├─ WindowsAdapters
│  ├─ Registry
│  ├─ Services
│  ├─ ScheduledTasks
│  ├─ Processes
│  ├─ Installer
│  └─ Signatures
├─ Cli
├─ Ui
└─ Tests
```

## 模块职责

### FileInventory

负责扫描文件和目录：

- 路径
- 类型：文件、目录或未知
- 大小
- 修改时间
- 后续可评估创建时间和访问时间，但 Phase 1 暂不进入报告 schema
- 属性
- 文件哈希
- 硬链接和符号链接信息

### Ownership

负责判断文件归属：

- 已安装应用
- Microsoft Store package
- Windows 组件
- 用户配置目录
- 开发工具缓存
- 浏览器缓存
- 未知来源

### Evidence

负责收集关系证据：

- 运行进程引用
- 服务引用
- 计划任务引用
- 启动项引用
- PATH 引用
- 快捷方式引用
- 注册表卸载项引用
- 文件签名和发布者

### Risk

负责把候选项转换为风险等级：

- SafeCandidate
- LowRisk
- MediumRisk
- HighRisk
- Blocked
- Unknown

风险判断必须输出原因和证据列表。

### Plan

负责生成可执行计划：

- Keep
- ReportOnly
- SuggestWindowsTool
- Quarantine
- Delete

MVP 只允许生成 `Keep`、`ReportOnly`、`SuggestWindowsTool`，不执行真实删除。

### Reporting

负责输出报告：

- JSON
- Markdown
- full / redacted 隐私模式
- 后续 UI 数据模型

## 推荐项目结构

```text
src/
  WinSafeClean.Core/
  WinSafeClean.Windows/
  WinSafeClean.Cli/
tests/
  WinSafeClean.Core.Tests/
  WinSafeClean.Windows.Tests/
docs/
  adr/
```

## 数据模型草案

```text
ScanItem
  Path
  Kind
  SizeBytes
  LastWriteTimeUtc
  Attributes
  Evidence[]
  RiskAssessment
  SuggestedAction

Evidence
  Type
  Source
  Confidence
  Message

RiskAssessment
  Level
  Confidence
  Reasons[]
  Blockers[]

CleanupPlan
  Items[]
  DryRun
  RequiresElevation
```

## Windows API 注意事项

- 不使用 `Win32_Product` 枚举软件，避免触发 MSI 修复。
- 系统组件清理优先建议 DISM 或 Windows 设置中的存储清理。
- Windows Installer cache 不做手动删除。
- 读取注册表、服务、计划任务失败时必须降级为 Unknown 或 HighRisk，而不是 LowRisk。
