# ADR 0002: 使用项目内 .NET SDK 和 xUnit 启动 TDD 核心

日期：2026-05-05

## 状态

Accepted

## 背景

当前机器没有全局 `dotnet` 命令。项目需要进入 Phase 1，并且必须按 TDD 方式启动核心逻辑开发。

## 决策

使用项目内本地 .NET 8 SDK：

- SDK 安装位置：`.tools/dotnet`
- SDK 固定文件：`global.json`
- 项目级 NuGet 源：`NuGet.config`
- 测试框架：xUnit

`.tools/` 不进入版本控制。

## 理由

本地 SDK 避免修改系统级环境，同时保证后续 Agent 可以复用同一工具链。xUnit 适合当前纯核心逻辑的 TDD 开发。

## 后果

优点：

- 不依赖全局 dotnet 安装
- 可重复运行测试
- 不污染系统 PATH

代价：

- 首次安装 SDK 会占用项目目录空间
- 新环境需要重新安装 `.tools/dotnet`

