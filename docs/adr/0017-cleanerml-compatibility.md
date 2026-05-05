# ADR 0017: CleanerML 兼容策略

日期：2026-05-06

## 状态

Accepted

## 背景

BleachBit 是成熟开源清理工具，CleanerML 是其 XML 清理规则格式。WinSafeClean 的目标不是直接执行清理，而是解释文件用途、建立关系证据并保守评估风险，因此需要判断 CleanerML 是否适合作为规则来源。

## 调研来源

- BleachBit CleanerML introduction: https://docs.bleachbit.org/cml/cleanerml.html
- BleachBit variables reference: https://docs.bleachbit.org/cml/variables.html
- BleachBit finding files guide: https://docs.bleachbit.org/cml/finding-files-to-delete.html
- CleanerML repository: https://github.com/bleachbit/cleanerml
- BleachBit XSD: https://github.com/bleachbit/bleachbit/blob/master/doc/cleaner_markup_language.xsd

## 发现

CleanerML 适合表达规则式清理目标：

- XML 格式，有 XSD。
- 支持 file、glob、walk、deep 等文件匹配。
- 支持 regex 过滤。
- 支持 Windows/Linux/macOS 等 OS 条件。
- 支持环境变量、`~` 和多值变量。
- 支持 `running` 条件，用于应用运行时中止清理。

但 CleanerML 同时包含执行型动作：

- delete、truncate、shred。
- Windows 注册表删除。
- process action。
- SQLite vacuum 等专用动作。

CleanerML 规则仓库中 `release` 目录表示较成熟规则，`pending` 表示等待验证的规则。规则内容为 GPL-3.0-or-later。

## 决策

WinSafeClean 不把 BleachBit 作为运行时依赖，也不调用 BleachBit 执行清理。

未来可以新增一个只读规则模块，例如 `WinSafeClean.CleanerRules`，以用户显式提供的 CleanerML 文件作为输入，首批只解析安全子集：

- cleaner / option 元数据。
- `os="windows"` 过滤。
- file、glob、walk.files、walk.all、walk.top 的候选路径。
- Windows `%VAR%` 环境变量和 CleanerML 常见路径变量的保守展开。
- `running` 条件作为 blocker evidence。

首批不支持或直接忽略：

- delete / truncate / shred 的执行语义。
- winreg 删除。
- process action。
- sqlite.vacuum 等专用动作。
- deep scan。
- 自动下载 winapp2.ini 或 CleanerML 仓库规则。

解析出的规则只能生成 `EvidenceType.KnownCleanupRule` 或报告注释，默认 `SuggestedAction.ReportOnly`。任何清理计划能力必须另行设计，并再次经过 TDD 和风险模型审查。

项目暂不内置 GPL CleanerML 规则文件，避免把外部规则内容和项目许可证边界混在一起。后续若要分发规则包，需要单独决定许可策略。

## 后果

优点：

- 可以复用成熟开源规则知识，但不继承执行风险。
- 保持 WinSafeClean “解释优先、默认只读”的边界。
- 避免当前阶段引入 GPL 规则内容分发问题。

限制：

- 不会立即获得 BleachBit 的完整清理能力。
- 用户提供的 CleanerML 规则需要后续解析器和安全子集测试支持。
- CleanerML 规则只能证明“某规则认为它是可清理候选”，不能单独证明删除安全。
