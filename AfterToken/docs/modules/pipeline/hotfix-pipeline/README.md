# 热更管线

## 职责

基于 HybridCLR 实现 C# 热更代码的编译、打包与加载。

## 核心文件

| 文件 | 路径 | 说明 |
|---|---|---|
| `GameLogic` 程序集 | `Assets/GameScripts/HotFix/GameLogic/` | 热更业务逻辑 |
| `GameProto` 程序集 | `Assets/GameScripts/HotFix/GameProto/` | 热更数据结构 |
| `HybridCLRSettings.asset` | `ProjectSettings/` | HybridCLR 配置 |

## 已完成

- HybridCLR 环境配置
- 热更 DLL 编译与加载流程

## 待完成

- AOT 元数据补充（如需要）
