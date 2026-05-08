# 路线图

## Phase 0 - 项目初始化

目标：建立原则、目标、架构和 TDD 约束。

状态：完成

交付物：

- README
- 项目原则
- 项目目标
- 实现框架
- 风险模型
- TDD 策略
- AI Agent 指南
- ADR

## Phase 1 - 只读核心 MVP

目标：实现可测试的只读扫描和风险报告。

状态：完成

交付物：

- .NET solution
- Core 测试项目
- 路径分类器
- 禁止目录识别
- 文件扫描抽象
- 风险评估模型
- JSON 报告
- Markdown 报告

完成标准：

- 不需要管理员权限
- 不执行删除
- 所有核心逻辑有单元测试

## Phase 2 - Windows 证据收集

目标：建立文件与系统、程序之间的关系证据。

状态：完成

交付物：

- 服务引用扫描
- 计划任务引用扫描
- 启动项扫描
- 卸载注册表扫描
- 运行进程引用扫描
- 文件签名读取

完成标准：

- 证据收集失败时可降级
- 不触发 MSI 修复
- 不修改系统状态

## Phase 3 - 清理计划与隔离

目标：生成可预览、可恢复的文件级清理计划。

状态：文件级闭环完成

交付物：

- Dry-run plan
- Read-only quarantine preview
- Restore metadata model
- Operation log model
- Preflight checklist
- Read-only preflight CLI
- Minimal quarantine executor in Core
- Guarded quarantine CLI
- Guarded restore CLI
- Restore metadata content hash
- 文件级回滚命令

完成标准：

- 默认不直接删除
- 高风险和禁止项不能进入删除计划
- 回滚路径经过测试
- 目录隔离和目录恢复明确暂缓

## Phase 4 - UI

目标：使用 WPF 提供普通用户能理解的界面。

状态：WPF MVP shell 完成

交付物：

- 空间占用视图
- 风险分组视图
- 文件解释面板
- 证据详情
- 清理计划预览
- WPF shell
- Cleanup plan overview view model
- Scan report risk grouping view model
- Scan report JSON reader
- Read-only operation command builder
- Preflight checklist reader
- UI empty states
- Risk and preflight status visual cues
- Read-only operation input validation feedback
- WPF startup smoke verification

完成标准：

- 用户能先理解再操作
- 危险操作需要明确确认
- 不用营销式风险文案

## Phase 5 - 发布准备

目标：建立可重复、本地优先、不会执行清理动作的发布流程。

状态：本地发布、版本元数据和归档基线完成

交付物：

- Local publish script
- Ignored publish artifacts
- Publish safety boundary ADR
- Publish smoke verification
- Release checklist
- Release notes template
- Version metadata
- CLI version command
- Release archives
- SHA256 checksum manifest

完成标准：

- 发布流程默认先跑测试
- 发布流程不运行生成程序
- 发布流程不请求管理员权限
- 发布流程不执行扫描、隔离、恢复、删除或清理命令
