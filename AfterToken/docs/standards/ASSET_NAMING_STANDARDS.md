# AfterToken 资源命名规范

> **状态**：项目级权威资源命名规范  
> **适用范围**：所有进入 YooAsset 热更资源包的资源（`Assets/AssetRaw/` 及其子目录）  
> **关联文档**：
> - [代码规范](./CODING_STANDARDS.md)
> - [UI 规范](./UI_STANDARDS.md)
> - [代码审查清单](./CODE_REVIEW_CHECKLIST.md)
> - 资源管线详细说明：`docs/framework/02-AssetPipeline.md`

---

## 1. 总体原则

1. **热更资源统一放 `Assets/AssetRaw/`**。
2. **不要把热更资源放在 `Assets/Resources/`**；`Resources/` 仅保留 Launcher 阶段必须的最小资源。
3. **YooAsset 地址 = 文件名（不含扩展名）**（`AddressByFileName`），因此同一包内禁止同名资源。
4. **目录结构即打包分组**，新增目录/移动资源会影响 bundle 划分，修改后必须重新 `SimulateBuild`。
5. **命名清晰、无空格、无中文、无特殊字符**（允许 `_`、`-`、数字、字母）。

---

## 2. 顶层目录组织

```
Assets/AssetRaw/
├── Actor/              # 角色 Prefab（玩家、NPC、敌人）
├── Audios/             # 音频（BGM、音效）
├── Configs/            # 配置数据（当前少量文本，后续接入 Luban 后可能迁移）
├── DLL/                # 热更 DLL .bytes
├── Effects/            # 特效 Prefab（粒子、动画特效）
├── Fonts/              # 字体（含 TMP FontAsset）
├── Materials/          # 材质
├── Prefabs/            # 通用 Prefab（Player、Projectile、Enemy 等）
├── Scenes/             # 可寻址场景
├── Shaders/            # Shader
├── Textures/           # 贴图 / RenderTexture
├── UI/                 # UI Prefab，按 UI 名称子目录存放
│   ├── BattleMainUI/BattleMainUI.prefab
│   ├── DamageNumberUI/DamageNumberUI.prefab
│   └── ...
└── UIRaw/              # UI 原始资源
    ├── Atlas/          # 图集
    └── Raw/            # 原始图片
```

---

## 3. 各类型资源命名规则

### 3.1 UI Prefab

- 路径：`Assets/AssetRaw/UI/{Name}/{Name}.prefab`
- 文件名与类名、`[Window]` 的 `location` 一致。
- 子目录名使用 `PascalCase`，与窗口类名去掉 `UI` 后缀后一致（如 `BattleMainUI` → `BattleMainUI`）。

### 3.2 角色 / 通用 Prefab

- 路径：
  - `Assets/AssetRaw/Actor/{Name}.prefab`（角色）
  - `Assets/AssetRaw/Prefabs/{Name}.prefab`（通用）
- 文件名使用 `PascalCase`：
  - `Player.prefab`
  - `Enemy_Grunt.prefab`（同类多变体可用 `_Variant` 后缀）
  - `Projectile_RifleBullet.prefab`

### 3.3 场景

- 路径：`Assets/AssetRaw/Scenes/{Name}.unity`
- 文件名使用 `PascalCase`：
  - `Main.unity`
  - `Battle.unity`
  - `Farm.unity`

### 3.4 特效

- 路径：`Assets/AssetRaw/Effects/{Category}/{Name}.prefab`
- 文件名使用 `PascalCase`，可加 `FX_` 前缀：
  - `FX_MuzzleFlash.prefab`
  - `FX_HitSparks.prefab`
  - `FX_Explosion_Fire.prefab`

### 3.5 音频

- 路径：`Assets/AssetRaw/Audios/{Category}/{Name}.{ext}`
- 分类目录：
  - `BGM/`：背景音乐
  - `SFX/`：音效（可继续按系统分子目录，如 `SFX/Weapon/`）
- 文件名使用 `PascalCase`：
  - `BGM_MainMenu.ogg`
  - `SFX_RifleShot.wav`
  - `SFX_Reload.wav`

### 3.6 材质 / Shader

- 路径：
  - `Assets/AssetRaw/Materials/{Name}.mat`
  - `Assets/AssetRaw/Shaders/{Name}.shader`
- 文件名使用 `PascalCase`：
  - `Player.mat`
  - `UI_Gray.shader`

### 3.7 贴图 / 纹理

- 路径：`Assets/AssetRaw/Textures/{Category}/{Name}.{ext}`
- 分类目录：`Characters/`、`Environment/`、`UI/`（如为 UI 原始贴图可放 `UIRaw/Raw/`）。
- 文件名使用 `PascalCase`，用途后缀可选：
  - `Player_Diffuse.png`
  - `Player_Normal.png`
  - `Grass_Tile.png`

### 3.8 字体

- 路径：`Assets/AssetRaw/Fonts/{Name}.asset`
- TMP FontAsset：`MainUIFont.asset`
- 源字体文件（若需保留）：`MainFont.ttf`

### 3.9 UI 原始资源

```
Assets/AssetRaw/UIRaw/
├── Atlas/
│   ├── Common/CommonAtlas.png        # 通用图集
│   ├── Battle/BattleAtlas.png        # 战斗图集
│   └── SingleAtlas/                  # 单张大图
└── Raw/
    ├── Icon_Gold.png
    └── Icon_Diamond.png
```

- 图集按系统/模块分类，避免单张图集过大。
- 原始图片文件名使用 `PascalCase`，可加 `_` 连接用途：
  - `Btn_Close.png`
  - `Bg_MainMenu.png`
  - `Icon_Weapon_Rifle.png`

### 3.10 配置 / DLL

- 配置：`Assets/AssetRaw/Configs/json/{Name}.json`（Luban `cs-newtonsoft-json` + `json` 输出目录）。
- DLL：`Assets/AssetRaw/DLL/GameLogic.dll.bytes`、`GameProto.dll.bytes`。

---

## 4. YooAsset 收集器配置

配置文件：`Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`

| Group | 路径 | AddressRule | PackRule | FilterRule |
|-------|------|-------------|----------|------------|
| Actor | `Assets/AssetRaw/Actor` | AddressByFileName | PackDirectory | CollectAll |
| Audios | `Assets/AssetRaw/Audios` | AddressByFileName | PackDirectory | CollectAll |
| Configs | `Assets/AssetRaw/Configs` | AddressByFileName | PackDirectory | CollectAll |
| DLL | `Assets/AssetRaw/DLL` | AddressByFileName | PackDirectory | CollectAll |
| Effects | `Assets/AssetRaw/Effects` | AddressByFileName | PackDirectory | CollectAll |
| Fonts | `Assets/AssetRaw/Fonts` | AddressByFileName | PackDirectory | CollectAll |
| Materials | `Assets/AssetRaw/Materials` | AddressByFileName | PackDirectory | CollectAll |
| Prefabs | `Assets/AssetRaw/Prefabs` | AddressByFileName | PackDirectory | CollectAll |
| Scenes | `Assets/AssetRaw/Scenes` | AddressByFileName | PackDirectory | CollectAll |
| UI | `Assets/AssetRaw/UI` | AddressByFileName | PackSeparately | CollectPrefab |
| UIRaw | `Assets/AssetRaw/UIRaw/Atlas`<br>`Assets/AssetRaw/UIRaw/Raw` | AddressByFileName | PackDirectory | CollectAll |

### 4.1 UI 收集器关键说明

```xml
<Collector CollectPath="Assets/AssetRaw/UI"
           AddressRule="AddressByFileName"
           PackRule="PackSeparately"
           FilterRule="CollectPrefab" />
```

- `CollectPrefab`：只收集 `.prefab` 文件，不收集文件夹资产。
- `PackSeparately`：每个 UI Prefab 独立成包。
- 这样既能使用 `Assets/AssetRaw/UI/{Name}/{Name}.prefab` 目录约定，又不会因文件夹与 Prefab 同名导致地址重复。

---

## 5. 资源地址使用规范

代码中通过 `GameModule.Resource` 加载时，`location` 必须与 YooAsset 地址一致：

```csharp
// UI
[Window(UILayer.UI, location: "BattleMainUI")]
public class BattleMainUI : UIWindow { }

// 角色
var player = await GameModule.Resource.LoadGameObjectAsync("Player");

// 场景
await GameModule.Scene.LoadSceneAsync("Battle");

// 字体
var font = await GameModule.Resource.LoadAssetAsync<TMP_FontAsset>("MainUIFont");
```

---

## 6. 禁止事项

1. 禁止把热更资源放在 `Assets/Resources/`（Launcher 最小资源除外）。
2. 禁止同一 YooAsset Group 内出现同名资源（会导致地址冲突）。
3. 禁止资源文件名含空格、中文、特殊符号。
4. 禁止在 UI Group 根目录直接放 `.prefab`，必须使用子目录。
5. 禁止手动移动 `AssetRaw` 资源后不重新执行 `SimulateBuild`。

---

## 7. 新增资源检查清单

- [ ] 资源路径符合本规范。
- [ ] 文件名使用 `PascalCase` 或约定的前缀。
- [ ] 同一 Group 内无同名资源。
- [ ] UI Prefab 放在 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`。
- [ ] 修改收集器后执行 `SimulateBuild` 无报错。
- [ ] 代码中 `location` 与文件名一致。
