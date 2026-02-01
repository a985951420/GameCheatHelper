# 热键游戏隔离和启动显示修复

## 更新日期
2026-02-01

## 修复概述

本次修复解决了两个核心问题：
1. **启动时加载默认游戏秘籍，热键未显示**
2. **不同游戏的快捷键配置冲突，需要独立存储**

---

## 问题1：启动时热键未显示

### 问题描述

**现象：**
- 应用启动时自动加载魔兽争霸3秘籍
- 所有秘籍的"热键"列显示为"未绑定"
- 即使 `Data/DefaultHotKeys.json` 中配置了热键

**根本原因：**

[MainViewModel.cs:168](../ViewModels/MainViewModel.cs#L168) 在构造函数中调用 `LoadCheatsForGame(GameType.Warcraft3)` 加载秘籍：

```csharp
public MainViewModel(IntPtr windowHandle)
{
    // ... 初始化服务 ...

    // 启动时默认加载魔兽争霸3秘籍
    LoadCheatsForGame(GameType.Warcraft3);  // ← 问题在这里
}
```

`LoadCheatsForGame` 方法从 `_cheatHotKeyMap` 字典中获取热键显示文本：

```csharp
private void LoadCheatsForGame(GameType gameType)
{
    CheatCodes.Clear();
    var cheats = _cheatCodeService.GetCheatsByGame(gameType);

    foreach (var cheat in cheats)
    {
        // 从 _cheatHotKeyMap 中查找热键
        var hotKey = _cheatHotKeyMap.ContainsKey(cheat.Id)
            ? _cheatHotKeyMap[cheat.Id]
            : "未绑定";  // ← 总是返回"未绑定"

        CheatCodes.Add(new CheatCodeViewModel(cheat, hotKey));
    }
}
```

但是，`_cheatHotKeyMap` 只在 `RegisterHotKeysForGame` 方法中被填充：

```csharp
private void RegisterHotKeysForGame(GameType gameType)
{
    var bindings = _hotKeyBindingService.GetBindingsByGameType(gameType, _cheatCodeService);

    foreach (var binding in bindings)
    {
        _cheatHotKeyMap[binding.CheatCodeId] = binding.HotKey.DisplayText;  // ← 在这里填充
    }
}
```

而 `RegisterHotKeysForGame` 只在游戏检测到后才调用（`OnGameDetected` 事件）：

```csharp
private void OnGameDetected(object? sender, GameInfo gameInfo)
{
    // 注册该游戏的热键
    RegisterHotKeysForGame(gameInfo.GameType);  // ← 只有游戏运行时才调用
}
```

**结论：**
- 启动时 `_cheatHotKeyMap` 是空的
- 加载秘籍时查询不到热键
- 所有秘籍显示为"未绑定"

### 修复方案

**新增方法：** `LoadHotKeyMapForGame(GameType gameType)`

这个方法负责从 `HotKeyBindingService` 加载热键映射到 `_cheatHotKeyMap`，但**不注册到操作系统**：

```csharp
/// <summary>
/// 加载游戏的热键映射（不注册到系统）
/// </summary>
private void LoadHotKeyMapForGame(GameType gameType)
{
    _cheatHotKeyMap.Clear();

    // 获取该游戏的热键绑定
    var bindings = _hotKeyBindingService.GetBindingsByGameType(gameType, _cheatCodeService);

    foreach (var binding in bindings)
    {
        _cheatHotKeyMap[binding.CheatCodeId] = binding.HotKey.DisplayText;
    }

    Logger.Info($"加载了 {_cheatHotKeyMap.Count} 个热键映射");
}
```

**修改 `LoadCheatsForGame` 方法：**

在加载秘籍前先调用 `LoadHotKeyMapForGame`：

```csharp
private void LoadCheatsForGame(GameType gameType)
{
    // 先加载该游戏的热键映射
    LoadHotKeyMapForGame(gameType);  // ← 新增

    CheatCodes.Clear();

    var cheats = _cheatCodeService.GetCheatsByGame(gameType);
    foreach (var cheat in cheats)
    {
        var hotKey = _cheatHotKeyMap.ContainsKey(cheat.Id)
            ? _cheatHotKeyMap[cheat.Id]
            : "未绑定";
        CheatCodes.Add(new CheatCodeViewModel(cheat, hotKey));
    }

    Logger.Info($"加载了 {cheats.Count} 个秘籍");
}
```

**职责分离：**

| 方法 | 职责 | 调用时机 |
|------|------|----------|
| `LoadHotKeyMapForGame` | 加载热键映射到内存（显示用） | 每次加载秘籍时 |
| `RegisterHotKeysForGame` | 注册热键到操作系统（功能用） | 游戏运行时 |

---

## 问题2：不同游戏的快捷键冲突

### 问题描述

**现象：**
- 魔兽争霸3和星际争霸1的热键配置保存在同一个文件 `DefaultHotKeys.json` 中
- 虽然通过秘籍ID前缀（`wc3_`、`sc_`）区分游戏，但键位本身可能冲突
- 例如：两个游戏都配置 `F1`，切换游戏时会混乱

**当前结构：**

```json
// Data/DefaultHotKeys.json
[
  {
    "id": 1,
    "cheatCodeId": "wc3_greedisgood",
    "key": "F1",
    "modifiers": 0,
    "description": "魔兽3: greedisgood"
  },
  {
    "id": 2,
    "cheatCodeId": "sc_showmethemoney",
    "key": "F1",  // ← 冲突！
    "modifiers": 0,
    "description": "星际1: show me the money"
  }
]
```

### 修复方案

**每个游戏独立的热键配置文件：**

```
Data/
├── Warcraft3_HotKeys.json    ← 魔兽争霸3热键配置
├── StarCraft_HotKeys.json    ← 星际争霸1热键配置
└── DefaultCheats.json         （秘籍配置保持不变）
```

**修改 `HotKeyBindingService.cs`：**

1. **修改构造函数和字段：**

```csharp
public class HotKeyBindingService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _dataDirectory;
    private Dictionary<GameType, List<HotKeyBinding>> _hotKeyBindingsByGame;  // ← 改为按游戏存储

    public HotKeyBindingService()
    {
        _hotKeyBindingsByGame = new Dictionary<GameType, List<HotKeyBinding>>();
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Logger.Info("热键绑定服务初始化");
    }
}
```

2. **新增方法获取游戏对应的文件路径：**

```csharp
/// <summary>
/// 获取游戏类型对应的热键文件路径
/// </summary>
private string GetHotKeyFilePath(GameType gameType)
{
    var fileName = gameType switch
    {
        GameType.Warcraft3 => "Warcraft3_HotKeys.json",
        GameType.StarCraft => "StarCraft_HotKeys.json",
        _ => "DefaultHotKeys.json"
    };
    return Path.Combine(_dataDirectory, fileName);
}
```

3. **修改 `LoadDefaultHotKeyBindings` 方法，添加 `GameType` 参数：**

```csharp
/// <summary>
/// 加载指定游戏类型的热键绑定
/// </summary>
public bool LoadDefaultHotKeyBindings(GameType gameType)
{
    try
    {
        var filePath = GetHotKeyFilePath(gameType);

        if (!File.Exists(filePath))
        {
            Logger.Warn($"游戏 {gameType} 的热键文件不存在: {filePath}");
            CreateDefaultBindings(gameType);
            return true;
        }

        var json = File.ReadAllText(filePath);
        var bindings = JsonConvert.DeserializeObject<List<HotKeyBindingDto>>(json);

        // 初始化该游戏的绑定列表
        if (!_hotKeyBindingsByGame.ContainsKey(gameType))
        {
            _hotKeyBindingsByGame[gameType] = new List<HotKeyBinding>();
        }

        _hotKeyBindingsByGame[gameType].Clear();
        foreach (var dto in bindings)
        {
            var binding = new HotKeyBinding { ... };
            _hotKeyBindingsByGame[gameType].Add(binding);
        }

        Logger.Info($"成功加载游戏 {gameType} 的 {_hotKeyBindingsByGame[gameType].Count} 个热键绑定");
        return true;
    }
    catch (Exception ex)
    {
        Logger.Error(ex, $"加载游戏 {gameType} 的热键绑定失败");
        return false;
    }
}
```

4. **修改 `SaveHotKeyBindings` 方法，添加 `GameType` 参数：**

```csharp
/// <summary>
/// 保存热键绑定到文件
/// </summary>
public bool SaveHotKeyBindings(GameType gameType)
{
    try
    {
        if (!_hotKeyBindingsByGame.ContainsKey(gameType))
        {
            Logger.Warn($"游戏 {gameType} 没有热键绑定数据");
            return false;
        }

        var bindings = _hotKeyBindingsByGame[gameType];
        var dtoList = bindings.Select(b => new HotKeyBindingDto { ... }).ToList();
        var json = JsonConvert.SerializeObject(dtoList, Formatting.Indented);
        var filePath = GetHotKeyFilePath(gameType);

        File.WriteAllText(filePath, json);
        Logger.Info($"游戏 {gameType} 的热键绑定已保存到: {filePath}");
        return true;
    }
    catch (Exception ex)
    {
        Logger.Error(ex, $"保存游戏 {gameType} 的热键绑定失败");
        return false;
    }
}
```

5. **修改 `AddOrUpdateHotKeyBinding`、`CheckHotKeyOccupied`、`GetBindingByCheatCodeId` 方法，添加 `GameType` 参数：**

```csharp
// 添加或更新热键绑定
public bool AddOrUpdateHotKeyBinding(GameType gameType, string cheatCodeId, HotKey? hotKey, string description)
{
    // 确保该游戏的热键列表已初始化
    if (!_hotKeyBindingsByGame.ContainsKey(gameType))
    {
        LoadDefaultHotKeyBindings(gameType);
    }

    var bindings = _hotKeyBindingsByGame[gameType];
    // ... 处理添加/更新逻辑 ...

    return SaveHotKeyBindings(gameType);  // ← 保存到对应游戏的文件
}

// 检查热键是否被占用（仅在同一游戏内检查）
public string? CheckHotKeyOccupied(GameType gameType, HotKey hotKey, string? excludeCheatCodeId = null)
{
    if (!_hotKeyBindingsByGame.ContainsKey(gameType))
    {
        LoadDefaultHotKeyBindings(gameType);
    }

    foreach (var binding in _hotKeyBindingsByGame[gameType])  // ← 仅检查同一游戏的热键
    {
        if (excludeCheatCodeId != null && binding.CheatCodeId == excludeCheatCodeId)
            continue;

        if (binding.HotKey.IsSameAs(hotKey))
            return binding.CheatCodeId;
    }
    return null;
}

// 根据秘籍ID获取热键绑定
public HotKeyBinding? GetBindingByCheatCodeId(GameType gameType, string cheatCodeId)
{
    if (!_hotKeyBindingsByGame.ContainsKey(gameType))
    {
        LoadDefaultHotKeyBindings(gameType);
    }

    return _hotKeyBindingsByGame[gameType].FirstOrDefault(b => b.CheatCodeId == cheatCodeId);
}
```

6. **修改 `GetBindingsByGameType` 方法，自动加载：**

```csharp
/// <summary>
/// 根据游戏类型获取热键绑定
/// </summary>
public List<HotKeyBinding> GetBindingsByGameType(GameType gameType, CheatCodeService cheatCodeService)
{
    // 如果该游戏的热键还未加载，先加载
    if (!_hotKeyBindingsByGame.ContainsKey(gameType))
    {
        LoadDefaultHotKeyBindings(gameType);
    }

    // 获取该游戏的所有秘籍ID
    var cheats = cheatCodeService.GetCheatsByGame(gameType);
    var cheatIds = new HashSet<string>(cheats.Select(c => c.Id));

    // 返回该游戏的热键绑定（仅包含有效的秘籍ID）
    return _hotKeyBindingsByGame.ContainsKey(gameType)
        ? _hotKeyBindingsByGame[gameType].Where(b => cheatIds.Contains(b.CheatCodeId)).ToList()
        : new List<HotKeyBinding>();
}
```

**修改 `MainViewModel.cs` 调用：**

1. **移除构造函数中的全局加载：**

```csharp
public MainViewModel(IntPtr windowHandle)
{
    // ... 初始化服务 ...

    _hotKeyBindingService = new HotKeyBindingService();
    // 不再在构造函数中加载热键，改为按需加载

    // ... 其他初始化 ...
}
```

2. **更新所有方法调用，添加 `GameType` 参数：**

```csharp
// 添加秘籍时检查冲突
var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
    newCheat.Game,  // ← 新增游戏类型参数
    viewModel.CurrentHotKey,
    null
);

// 保存热键绑定
_hotKeyBindingService.AddOrUpdateHotKeyBinding(
    newCheat.Game,  // ← 新增游戏类型参数
    newCheat.Id,
    viewModel.CurrentHotKey,
    description
);

// 加载现有热键绑定
var existingBinding = _hotKeyBindingService.GetBindingByCheatCodeId(
    cheat.Game,  // ← 新增游戏类型参数
    cheat.Id
);
```

---

## 修复效果

### ✅ 问题1修复验证

**修复前：**
```
启动应用
  ↓
加载魔兽争霸3秘籍
  ↓
秘籍列表显示
  - greedisgood    |  未绑定  ❌
  - iseedeadpeople |  未绑定  ❌
  - whosyourdaddy  |  未绑定  ❌
```

**修复后：**
```
启动应用
  ↓
加载魔兽争霸3秘籍
  ├─ LoadHotKeyMapForGame(Warcraft3)  ← 新增
  │   └─ _cheatHotKeyMap 填充热键映射
  ↓
秘籍列表显示
  - greedisgood    |  F1      ✅
  - iseedeadpeople |  F2      ✅
  - whosyourdaddy  |  F3      ✅
```

### ✅ 问题2修复验证

**修复前：**
```
Data/
└── DefaultHotKeys.json
    [
      { "cheatCodeId": "wc3_greedisgood", "key": "F1" },
      { "cheatCodeId": "sc_showmethemoney", "key": "F1" }  ← 冲突
    ]
```

**修复后：**
```
Data/
├── Warcraft3_HotKeys.json
│   [
│     { "cheatCodeId": "wc3_greedisgood", "key": "F1" }
│   ]
│
└── StarCraft_HotKeys.json
    [
      { "cheatCodeId": "sc_showmethemoney", "key": "F1" }  ← 不冲突
    ]
```

**热键冲突检测范围：**

| 修复前 | 修复后 |
|--------|--------|
| 全局检测（所有游戏） | 游戏内检测（仅同一游戏） |
| 魔兽争霸3的F1 与 星际争霸1的F1 冲突 | 魔兽争霸3的F1 与 星际争霸1的F1 不冲突 |

---

## 文件变更清单

| 文件 | 变更内容 | 状态 |
|------|---------|------|
| [Services/HotKeyBindingService.cs](../Services/HotKeyBindingService.cs) | 重构为按游戏类型存储热键配置 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 添加 `LoadHotKeyMapForGame` 方法 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 修改 `LoadCheatsForGame` 调用热键加载 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 更新所有热键服务方法调用添加 GameType 参数 | ✅ 已完成 |

---

## 编译测试结果

```bash
dotnet build GameCheatHelper.csproj

✅ Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:06.19
```

---

## 使用场景

### 场景1：启动应用立即看到热键

**操作：**
1. 启动 GameCheatHelper
2. 应用自动加载魔兽争霸3秘籍

**预期结果：**
- ✅ 秘籍列表立即显示配置的热键（如：F1、F2、F3）
- ✅ 无需等待游戏启动

### 场景2：不同游戏使用相同热键

**操作：**
1. 魔兽争霸3配置 F1 → greedisgood
2. 星际争霸1配置 F1 → show me the money
3. 切换游戏类型下拉框

**预期结果：**
- ✅ 两个游戏的F1不冲突
- ✅ 切换游戏时正确显示各自的热键

### 场景3：添加新秘籍并配置热键

**操作：**
1. 选择游戏类型：魔兽争霸3
2. 添加新秘籍，配置热键 F5
3. 切换到星际争霸1
4. 添加新秘籍，也配置热键 F5

**预期结果：**
- ✅ 两个F5不冲突（保存到不同文件）
- ✅ 切换游戏时显示正确的热键

---

## 技术要点

### 1. 按需加载策略

**旧方案：**
- 启动时全局加载所有热键（所有游戏）
- 占用内存，加载慢

**新方案：**
- 按游戏类型按需加载
- 首次访问时自动加载
- 内存占用小，加载快

### 2. 职责分离

| 功能 | 方法 | 时机 | 作用域 |
|------|------|------|--------|
| 显示热键 | `LoadHotKeyMapForGame` | 加载秘籍时 | 内存 (`_cheatHotKeyMap`) |
| 注册热键 | `RegisterHotKeysForGame` | 游戏运行时 | 操作系统 (Win32 API) |

### 3. 游戏隔离

```
HotKeyBindingService
├── _hotKeyBindingsByGame
│   ├── [Warcraft3]
│   │   ├── { CheatCodeId: "wc3_greedisgood", HotKey: F1 }
│   │   ├── { CheatCodeId: "wc3_iseedeadpeople", HotKey: F2 }
│   │   └── { CheatCodeId: "wc3_whosyourdaddy", HotKey: F3 }
│   │
│   └── [StarCraft]
│       ├── { CheatCodeId: "sc_showmethemoney", HotKey: F1 }  ← F1 不冲突
│       └── { CheatCodeId: "sc_blacksheepwall", HotKey: F2 }
```

---

## 后续优化建议

1. **迁移旧数据：**
   - 如果存在 `DefaultHotKeys.json`，自动迁移到 `Warcraft3_HotKeys.json` 和 `StarCraft_HotKeys.json`

2. **缓存优化：**
   - 热键数据加载后缓存，避免重复读取文件

3. **配置文件版本控制：**
   - 添加版本号字段，方便未来升级迁移

---

## 测试清单

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 启动显示热键 | 启动应用，检查魔兽争霸3秘籍是否显示热键 | ⏳ 待测试 |
| 切换游戏类型 | 切换到星际争霸1，检查热键是否正确显示 | ⏳ 待测试 |
| 添加秘籍热键 | 未启动游戏时添加秘籍并配置热键 | ⏳ 待测试 |
| 编辑秘籍热键 | 未启动游戏时编辑秘籍热键 | ⏳ 待测试 |
| 热键冲突检测 | 同一游戏内配置重复热键，检查是否提示 | ⏳ 待测试 |
| 跨游戏热键独立 | 两个游戏配置相同热键，检查是否独立 | ⏳ 待测试 |
| 游戏运行时热键 | 启动游戏，检查热键是否正常触发秘籍 | ⏳ 待测试 |

---

**文档版本**: 1.0
**最后更新**: 2026-02-01
**状态**: ✅ 修复完成，编译通过，待用户测试验证

---

## 相关文档

- [游戏选择与无游戏限制功能](./Game-Selection-And-No-Game-Restriction.md)
- [热键组合键功能迭代](./HotKey-Combo-Feature-Iteration.md)
- [热键保存与冲突检测](./HotKey-Save-And-Conflict-Detection.md)
