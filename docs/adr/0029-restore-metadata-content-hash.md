# ADR 0029: restore metadata 内容 hash 校验

日期：2026-05-07

## 状态

Accepted

## 背景

`quarantine` 会把文件移动到隔离目录并写入 restore metadata，`restore` 再基于 metadata 把隔离文件移回原路径。若隔离文件在恢复前被篡改、替换或损坏，单靠路径 metadata 无法发现。

项目原则要求未知证据增加谨慎度，恢复路径也必须尽可能避免把错误内容写回用户原路径。

## 决策

restore metadata 增加可选内容 hash 字段，并在带 hash 的执行后 metadata 中使用 schema `1.1`：

- `contentHashAlgorithm`
- `contentHash`

当前只支持 `SHA256`，以小写十六进制字符串存储。

`quarantine` 执行器在写 restore metadata 前计算源文件 SHA256，并写入 schema `1.1` metadata。若 hash 计算失败，隔离中止，源文件不移动。

`restore` 执行器在恢复前检查 metadata：

- metadata 带 `SHA256` hash 时，必须先计算隔离文件 hash 并匹配。
- hash 不匹配、算法未知或 hash 读取失败时，拒绝恢复且不移动文件。
- 旧 metadata 没有 hash 时默认拒绝恢复。
- 旧 metadata 只有在调用方显式允许 legacy metadata without content hash 时才可恢复。

## 理由

把 hash 写入 restore metadata 可以让恢复命令验证“要恢复的内容仍是隔离时的内容”。hash 计算放在移动前，可以避免写出缺少内容证据的新 metadata。

旧 metadata 仍保留显式例外，是为了兼容早期生成的恢复文件；例外必须由调用方额外确认，不能和普通带 hash metadata 走同一默认路径。

## 后果

优点：

- 新隔离文件具备恢复前内容完整性校验。
- 被替换或损坏的隔离文件不会被恢复到原路径。
- restore metadata schema `1.0` 仍可反序列化。

限制：

- 当前只支持 SHA256。
- 当前不记录文件大小或多算法 hash。
- 旧 metadata 缺少 hash 时需要额外 legacy 确认，且仍只能依赖路径和人工确认。
