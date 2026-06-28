# Luban 工具链

## 安装位置

Luban 主程序 `Luban.dll` 应放在本目录下：

```
Tools/Luban/Luban.dll
```

> 注意：`Luban.dll` 不放在 `Assets/` 下，避免被 Unity 编译。

## 重要：需要完整工具链

**仅放置 `Luban.dll` 是不够的。** 运行 `dotnet Luban.dll` 还需要以下文件：

- `Luban.runtimeconfig.json`
- `Luban.deps.json`
- `hostpolicy.dll`
- `hostfxr.dll`
- 以及其他 Luban 依赖的 DLL

完整目录示例：

```
Tools/Luban/
├── Luban.dll
├── Luban.runtimeconfig.json
├── Luban.deps.json
├── hostpolicy.dll
├── hostfxr.dll
└── ... 其他依赖 DLL
```

如果缺少这些文件，运行生成脚本会报错：

```
A fatal error was encountered. The library 'hostpolicy.dll' required to execute the application was not found...
```

## 如何获取完整 Luban 工具链

由于当前环境无法直接下载，请从以下任一方式获取：

### 方式一：GitHub Release（推荐）

访问 Luban 官方仓库 Release 页面：

```
https://github.com/focus-creative-games/luban/releases
```

下载对应版本的 Luban 工具链压缩包（通常命名为 `luban-x.x.x-win-x64.zip` 或类似），解压后**整个文件夹内容**放到 `Tools/Luban/`。

### 方式二：Gitee 镜像

如果 GitHub 访问慢，可尝试 Gitee 镜像：

```
https://gitee.com/focus-creative-games/luban/releases
```

### 方式三：从已有项目拷贝

如果其他项目已使用 Luban，可直接拷贝其完整 `Tools/Luban/` 目录内容到本目录。

## 验证安装

放置完整工具链后，在仓库根目录执行：

```bash
dotnet Tools/Luban/Luban.dll --help
```

如果输出 Luban 的帮助信息，说明安装成功。

## 运行生成

验证安装后，运行项目生成脚本：

```bash
Configs/GameConfig/gen_code_bin_to_project.bat
```

## 版本要求

- Luban 版本：3.x 或兼容版本
- .NET SDK：8.0 或更高（当前环境为 9.0.306，已满足）
