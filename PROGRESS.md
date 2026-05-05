# 项目进度

## 当前状态

阶段：1 - 只读核心 MVP

日期：2026-05-05

## 已完成

- 创建项目目录结构
- 建立项目 README
- 建立项目原则
- 建立项目目标
- 建立 TDD 策略
- 建立 AI Agent 协作指南
- 建立调研摘要
- 建立风险模型
- 建立实现框架
- 建立路线图
- 建立首个 ADR
- 建立根目录 `AGENTS.md`
- 建立质量门禁
- 建立任务模板
- 补充子 Agent 分工优先原则
- 初始化 git 仓库
- 安装项目内本地 .NET 8 SDK 到 `.tools/dotnet`
- 新增 `global.json`
- 新增项目级 `NuGet.config`
- 新增 `scripts/bootstrap-dotnet.ps1`
- 新增 `scripts/test.ps1`
- 初始化 `WinSafeClean.sln`
- 创建 `WinSafeClean.Core`
- 创建 `WinSafeClean.Core.Tests`
- 按 TDD 建立禁止路径识别测试
- 实现最小路径风险分类核心
- 建立 JSON 报告模型测试
- 实现最小 JSON 报告序列化
- 根据子 Agent 审查补充路径规范化安全测试
- 支持 `..`、`\\?\`、本机 admin share、重复分隔符和非 C 盘 Windows 目录保护
- 验证命令：`pwsh -NoProfile -File scripts\test.ps1`
- 测试通过：28 passed
- 创建公开 GitHub 仓库：https://github.com/Fiyy/WinSafeClean

## 正在进行

- Phase 1 只读核心 MVP

## 下一步

1. 添加 Markdown 报告输出测试。
2. 创建 CLI 项目，但保持只读。
3. 添加文件扫描抽象测试。
4. 添加扫描报告生成流程测试。

## 待决策

- UI 使用 WPF 还是 WinUI 3。
- 是否兼容 BleachBit CleanerML 作为规则输入。
- 是否在 MVP 使用 SQLite 保存扫描历史。
- 是否提供 PowerShell 模块入口。
