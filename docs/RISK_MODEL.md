# 风险模型

## 风险等级

### Blocked

禁止直接清理。

典型情况：

- Windows Installer cache
- WinSxS
- System32
- DriverStore
- SysWOW64
- SystemApps
- Windows servicing
- Windows INF
- 已知系统服务路径
- 当前运行进程正在使用的关键模块

允许动作：

- Keep
- SuggestWindowsTool

路径判断必须先做 Windows 语义规范化。以下形式如果最终指向禁止目录，也必须被识别为 Blocked：

- `..` 或 `.` 路径段
- `\\?\` extended-length 路径前缀
- 本机 admin share，例如 `\\localhost\c$`
- 重复路径分隔符
- 非 C 盘的 Windows 根目录，例如 `D:\Windows\System32`

### HighRisk

不建议清理。

典型情况：

- 存在服务、计划任务、启动项引用
- 属于已安装程序目录
- 包含 DLL、EXE、配置、插件、数据库
- 归属不明但位于 AppData 或 ProgramData
- 证据收集失败且路径敏感

允许动作：

- Keep
- ReportOnly

### MediumRisk

需要谨慎处理。

典型情况：

- 疑似应用缓存但缺少明确规则
- 疑似卸载残留但仍存在注册表引用
- 大型日志或备份目录
- 长期未修改但归属不明

允许动作：

- ReportOnly
- Quarantine

### LowRisk

通常可以清理，但仍需要预览和确认。

典型情况：

- 用户临时目录中过期文件
- 浏览器或应用缓存，且规则明确
- 崩溃转储或日志文件
- 回收站

允许动作：

- ReportOnly
- Quarantine
- Delete

### SafeCandidate

高置信度安全候选项。

典型情况：

- 明确可再生缓存
- 明确临时文件
- 已知清理规则覆盖
- 无活跃引用
- 不在敏感路径

允许动作：

- ReportOnly
- Quarantine
- Delete

## 风险提升规则

出现以下任一证据时，风险至少提升到 HighRisk：

- 被运行中进程引用
- 被 Windows 服务引用
- 被计划任务引用
- 被启动项引用
- 位于系统敏感目录
- 位于已安装程序目录且不是明确缓存
- 文件签名发布者为 Microsoft 且位于系统目录

## 风险降低规则

只有同时满足多个条件才可降低风险：

- 位于已知缓存或临时目录
- 有明确清理规则
- 最近没有被使用
- 无服务、任务、进程、启动项引用
- 不在禁止目录
- 可通过应用重新生成

文件签名只能解释发布者来源，不能单独降低风险，也不能作为清理许可。

## 输出要求

每个风险判断必须包含：

- 风险等级
- 置信度
- 建议动作
- 证据列表
- 阻断理由
- 用户可理解的解释
