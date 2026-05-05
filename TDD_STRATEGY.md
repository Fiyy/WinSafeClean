# TDD 策略

## 总原则

本项目的核心价值在于“不要误删”。因此测试不是附属物，而是风险控制系统的一部分。

所有核心判断逻辑必须先写测试，再写实现。

## 测试分层

### Unit Tests

覆盖纯逻辑：

- 路径分类
- 禁止目录识别
- 风险等级计算
- 证据权重合成
- 清理计划生成
- 报告序列化

Unit tests 不访问真实系统目录，不依赖当前机器状态。

### Integration Tests

覆盖 Windows API 或文件系统适配器：

- 测试临时目录扫描
- 测试模拟文件锁定
- 测试签名读取失败时的降级逻辑
- 测试计划任务、服务、注册表适配器的错误处理

Integration tests 只能操作测试沙箱目录。

### Golden Tests

固定输入，固定报告输出：

- 同一组扫描样本必须得到稳定风险等级
- 报告字段变更必须显式更新快照

### Regression Tests

任何发现过的误判都必须加入回归测试：

- 把应禁止删除的路径判断为可删除
- 把应用依赖文件判断为残留
- 把硬链接或符号链接重复计算
- 把系统工具可清理项目误判为可手动删除

## Red-Green-Refactor 流程

1. 写一个描述期望行为的失败测试。
2. 写最小代码让测试通过。
3. 重构代码结构。
4. 重新运行全部相关测试。
5. 更新 `PROGRESS.md`。

## 必测规则

以下功能没有测试不得合并：

- 删除或隔离执行器
- 禁止目录保护
- 风险等级从高降到低的逻辑
- 文件归属识别
- 清理计划生成
- 恢复元数据读写

## 测试命名约定

使用行为描述式命名：

```text
ShouldBlockWindowsInstallerCache()
ShouldMarkTempFilesAsLowRiskWhenNotRecentlyModified()
ShouldRequireQuarantineForUnknownLargeAppDataFolder()
ShouldNeverGenerateDeleteActionForWinSxS()
```

