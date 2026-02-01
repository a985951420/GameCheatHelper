# 热键注册失败诊断报告

## 问题日期
2026-02-01

## 问题概述

根据错误日志分析，发现两个主要问题：
1. **热键注册失败（错误代码 1409）**：Ctrl+F1, Ctrl+F2, Ctrl+F3 注册失败
2. **热键未绑定秘籍**：热键触发时找不到对应的秘籍

---

## 问题1：热键注册失败（错误代码 1409）

### 错误日志

```
2026-02-01 19:16:05.9817|ERROR|GameCheatHelper.Services.HotKeyManager|热键注册失败: Ctrl+F2, 错误代码: 1409
2026-02-01 19:16:05.9817|ERROR|GameCheatHelper.Services.HotKeyManager|热键注册失败: Ctrl+F3, 错误代码: 1409
```

### 错误分析

**错误代码 1409** 对应 Windows 错误 `ERROR_HOTKEY_ALREADY_REGISTERED`，表示热键已被注册。

### 可能原因

#### 原因1：系统或其他程序占用

Ctrl+F1, Ctrl+F2, Ctrl+F3 可能被以下程序占用：
- **Windows 系统快捷键**
- **杀毒软件**（如 360、腾讯管家）
- **输入法软件**（如搜狗、QQ拼音）
- **截图工具**（如 QQ截图、微信截图）
- **其他游戏辅助工具**

#### 原因2：旧的 DefaultHotKeys.json 混合配置

`Data/DefaultHotKeys.json` 包含了魔兽争霸3和星际争霸1的混合热键配置：

```json
[
  {
    "Id": 1,
    "CheatCodeId": "wc3_greedisgood",
    "Key": "F1",
    "Modifiers": 2  // Ctrl+F1
  },
  {
    "Id": 6,
    "CheatCodeId": "sc_showmethemoney",
    "Key": "F1",
    "Modifiers": 2  // Ctrl+F1 (冲突!)
  }
]
```

如果程序的某个地方仍在使用这个文件，可能导致重复注册。

### 解决方案

#### 方案1：更换热键组合（推荐）

避免使用常见的系统快捷键，建议使用：
- **Alt + 数字键**（Alt+1, Alt+2, Alt+3...）
- **Ctrl+Shift + 功能键**（Ctrl+Shift+F1, Ctrl+Shift+F2...）
- **单独的功能键**（F1-F12）

#### 方案2：热键冲突检测优化

在注册热键前检测是否被占用，并提供友好的错误提示：

```csharp
public bool RegisterHotKey(HotKey hotKey)
{
    // ... 现有代码 ...

    if (!success)
    {
        var error = Marshal.GetLastWin32Error();

        if (error == 1409) // ERROR_HOTKEY_ALREADY_REGISTERED
        {
            Logger.Error($"热键 {hotKey.DisplayText} 已被系统或其他程序占用，请更换热键组合");
        }
        else
        {
            Logger.Error($"热键注册失败: {hotKey.DisplayText}, 错误代码: {error}");
        }

        return false;
    }
}
```

#### 方案3：清理旧的热键配置

删除或重命名 `Data/DefaultHotKeys.json`，只保留独立的游戏配置文件：
```
Data/
├── Warcraft3_HotKeys.json  ✅ 保留
├── StarCraft_HotKeys.json  ✅ 保留
└── DefaultHotKeys.json     ❌ 删除或重命名
```

---

## 问题2：热键未绑定秘籍

### 错误日志

```
2026-02-01 19:16:05.9884|INFO|GameCheatHelper.Services.HotKeyManager|热键注册成功: F1 (ID: 12)
2026-02-01 19:16:05.9884|INFO|GameCheatHelper.ViewModels.MainViewModel|注册热键: F1 -> wc3_greedisgood_large

...

2026-02-01 19:16:09.0976|DEBUG|GameCheatHelper.Services.HotKeyManager|热键触发: F1
2026-02-01 19:16:09.0981|WARN|GameCheatHelper.ViewModels.MainViewModel|热键未绑定秘籍
```

### 问题分析

**现象：**
- 热键注册时显示正确的 `CheatCodeId`：`wc3_greedisgood_large`
- 热键触发时提示"热键未绑定秘籍"

**根本原因（待验证）：**
- `HotKey` 对象的 `CheatCodeId` 属性在注册后丢失
- 或者 `_registeredHotKeys` 字典中存储的对象与原始对象不同

### 调试措施（已实施）

添加了调试日志来追踪 `CheatCodeId`：

**修改1：注册时输出 CheatCodeId**

[Services/HotKeyManager.cs:81](../Services/HotKeyManager.cs#L81)

```csharp
if (success)
{
    hotKey.Id = id;
    _registeredHotKeys[id] = hotKey;
    Logger.Info($"热键注册成功: {hotKey.DisplayText} (ID: {id}), CheatCodeId: {hotKey.CheatCodeId ?? "null"}");
    return true;
}
```

**修改2：触发时输出 CheatCodeId**

[Services/HotKeyManager.cs:207](../Services/HotKeyManager.cs#L207)

```csharp
if (_registeredHotKeys.TryGetValue(id, out var hotKey))
{
    Logger.Debug($"热键触发: {hotKey.DisplayText}, CheatCodeId: {hotKey.CheatCodeId ?? "null"}");
    HotKeyPressed?.Invoke(this, hotKey);
    handled = true;
}
```

### 下一步诊断步骤

1. **关闭当前运行的 GameCheatHelper.exe**
2. **重新编译项目**
3. **运行程序并触发热键**
4. **查看日志输出**，确认：
   - 注册时 `CheatCodeId` 是否为 `null`
   - 触发时 `CheatCodeId` 是否为 `null`

### 可能的解决方案

#### 方案A：CheatCodeId 在加载时未设置

如果注册时 `CheatCodeId` 就是 `null`，说明在 `HotKeyBindingService.LoadDefaultHotKeyBindings` 中没有正确设置。

**修复：** 检查并确保 JSON 反序列化正确：

```csharp
var binding = new HotKeyBinding
{
    Id = dto.Id,
    CheatCodeId = dto.CheatCodeId,  // 确保这里有值
    HotKey = new HotKey
    {
        Id = dto.Id,
        Key = ParseKey(dto.Key),
        Modifiers = dto.Modifiers,
        CheatCodeId = dto.CheatCodeId  // 这里也要设置
    },
    Description = dto.Description
};
```

#### 方案B：HotKey 对象被复制或重新创建

如果注册时有值但触发时为 `null`，说明 `HotKey` 对象被复制或重新创建了。

**修复：** 确保 `_registeredHotKeys` 存储的是同一个对象引用：

```csharp
// 现有代码已经正确：
_registeredHotKeys[id] = hotKey;  // 直接保存引用，不创建新对象
```

---

## 当前配置文件状态

### Warcraft3_HotKeys.json

```json
[
  {
    "Id": 2,
    "CheatCodeId": "wc3_iseedeadpeople",
    "Key": "F2",
    "Modifiers": 2,  // Ctrl+F2 ← 注册失败 (1409)
    "Description": "魔兽争霸3: 显示全图"
  },
  {
    "Id": 3,
    "CheatCodeId": "wc3_whosyourdaddy",
    "Key": "F3",
    "Modifiers": 2,  // Ctrl+F3 ← 注册失败 (1409)
    "Description": "魔兽争霸3: 无敌模式"
  },
  {
    "Id": 4,
    "CheatCodeId": "wc3_greedisgood_large",
    "Key": "F1",
    "Modifiers": 0,  // F1 ← 注册成功，但触发时未绑定秘籍
    "Description": "魔兽争霸3: 获得999999金和木材"
  }
]
```

**建议修改：**
```json
[
  {
    "Id": 2,
    "CheatCodeId": "wc3_iseedeadpeople",
    "Key": "F2",
    "Modifiers": 0,  // 改为 F2（无修饰键）
    "Description": "魔兽争霸3: 显示全图"
  },
  {
    "Id": 3,
    "CheatCodeId": "wc3_whosyourdaddy",
    "Key": "F3",
    "Modifiers": 0,  // 改为 F3（无修饰键）
    "Description": "魔兽争霸3: 无敌模式"
  },
  {
    "Id": 4,
    "CheatCodeId": "wc3_greedisgood_large",
    "Key": "F1",
    "Modifiers": 0,
    "Description": "魔兽争霸3: 获得999999金和木材"
  }
]
```

---

## 测试步骤

### 步骤1：关闭应用程序

关闭所有正在运行的 `GameCheatHelper.exe` 实例。

### 步骤2：重新编译

```bash
dotnet build GameCheatHelper.csproj
```

### 步骤3：修改热键配置（可选）

编辑 `bin/Debug/net8.0-windows/Data/Warcraft3_HotKeys.json`，将 Ctrl 修饰键改为无修饰键：

```json
{
  "Modifiers": 0  // 将 2 改为 0
}
```

### 步骤4：运行程序

启动 `bin/Debug/net8.0-windows/GameCheatHelper.exe`

### 步骤5：查看日志

#### 启动时日志

查找以下信息：
```
热键注册成功: F1 (ID: X), CheatCodeId: wc3_greedisgood_large
```

**预期结果：** `CheatCodeId` 不为 `null`

#### 触发热键后日志

按下 F1，查找以下信息：
```
热键触发: F1, CheatCodeId: wc3_greedisgood_large
```

**预期结果：** `CheatCodeId` 不为 `null`

### 步骤6：测试秘籍执行

1. 启动魔兽争霸3游戏
2. 按下 F1 热键
3. 检查秘籍是否成功执行

---

## 修改文件清单

| 文件 | 修改内容 | 状态 |
|------|---------|------|
| [Services/HotKeyManager.cs](../Services/HotKeyManager.cs#L81) | 添加 CheatCodeId 调试日志（注册时） | ✅ 已完成 |
| [Services/HotKeyManager.cs](../Services/HotKeyManager.cs#L207) | 添加 CheatCodeId 调试日志（触发时） | ✅ 已完成 |

---

## 后续优化建议

### 1. 热键冲突友好提示

在 UI 中显示热键注册失败的原因，而不是仅在日志中记录：

```csharp
if (error == 1409)
{
    MessageBox.Show(
        $"热键 {hotKey.DisplayText} 已被系统或其他程序占用\n\n请尝试：\n1. 更换其他热键组合\n2. 关闭可能占用热键的程序（如输入法、截图工具）",
        "热键注册失败",
        MessageBoxButton.OK,
        MessageBoxImage.Warning
    );
}
```

### 2. 热键配置验证

在启动时验证热键配置的有效性：
- 检查是否有重复的热键组合
- 检查是否使用了系统保留的热键
- 提供热键冲突修复工具

### 3. 默认热键优化

使用不太常见的热键组合作为默认值：
- F1-F12（无修饰键）
- Alt + 数字键
- Ctrl+Shift + 功能键

---

## 常见问题

### Q1: 为什么 Ctrl+F1/F2/F3 会被占用？

**A:** 这些是常见的系统快捷键，可能被以下程序使用：
- Windows 系统功能
- 输入法切换
- 截图工具
- 浏览器快捷键

### Q2: 如何查看哪个程序占用了热键？

**A:** Windows 没有内置工具查看热键占用情况，建议：
1. 逐个关闭可疑程序
2. 使用第三方工具（如 [HotKeysList](https://www.nirsoft.net/utils/hot_keys_list.html)）
3. 更换热键组合避免冲突

### Q3: 为什么热键注册成功但触发时提示未绑定秘籍？

**A:** 可能的原因：
1. `CheatCodeId` 在加载时未正确设置
2. `HotKey` 对象在注册过程中被复制或重新创建
3. JSON 配置文件中的 `CheatCodeId` 与秘籍数据库不匹配

通过调试日志可以确认具体原因。

---

**文档版本**: 1.0
**最后更新**: 2026-02-01
**状态**: ⏳ 等待用户关闭应用程序并重新编译测试

---

## 相关文档

- [热键游戏隔离和启动显示修复](./HotKey-Game-Isolation-Fix.md)
- [热键组合键功能迭代](./HotKey-Combo-Feature-Iteration.md)
- [热键保存与冲突检测](./HotKey-Save-And-Conflict-Detection.md)
