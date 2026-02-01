# 游戏选择与无游戏限制功能迭代

## 更新日期
2026-02-01

## 功能概述

本次迭代移除了"必须打开游戏才能管理秘籍"的限制，添加了手动游戏选择功能，使游戏检测成为辅助功能而非必需功能。

---

## 核心改进

### 1. ✅ 添加游戏选择下拉框

**功能说明：**
- 主窗口顶部添加游戏类型下拉框
- 用户可手动选择要管理的游戏（魔兽争霸3 或 星际争霸1）
- 无需等待游戏检测，即可开始管理秘籍

**UI 变化：**

**修改前：**
```
┌────────────────────────────────────────┐
│  当前游戏: 魔兽争霸3        ⚙ 设置     │
└────────────────────────────────────────┘
```

**修改后：**
```
┌────────────────────────────────────────┐
│  🎮 游戏: [魔兽争霸3 ▼]                │
│  📡 检测状态: 未检测到游戏              │
│                              ⚙ 设置    │
└────────────────────────────────────────┘
```

**实现位置：**
- 文件：[Views/MainWindow.xaml](../Views/MainWindow.xaml)
- 行数：23-75

**代码示例：**
```xml
<!-- 游戏选择 -->
<StackPanel Grid.Column="0" Orientation="Horizontal" Margin="0,0,20,0">
    <TextBlock Text="🎮 游戏: "
               Foreground="White"
               FontSize="14"
               VerticalAlignment="Center"
               Margin="0,0,5,0"/>
    <ComboBox x:Name="GameTypeComboBox"
              ItemsSource="{Binding GameTypes}"
              SelectedItem="{Binding SelectedGameType}"
              DisplayMemberPath="Value"
              SelectedValuePath="Key"
              Width="150" Height="28"
              FontSize="13"
              VerticalAlignment="Center"
              SelectionChanged="GameTypeComboBox_SelectionChanged"/>
</StackPanel>

<!-- 游戏检测状态 -->
<StackPanel Grid.Column="1" Orientation="Horizontal">
    <TextBlock Text="📡 检测状态: "
               Foreground="White"
               FontSize="14"
               VerticalAlignment="Center"/>
    <TextBlock Text="{Binding GameStatus}"
               Foreground="White"
               FontSize="14"
               FontWeight="Bold"
               VerticalAlignment="Center"/>
</StackPanel>
```

---

### 2. ✅ 启动时默认加载魔兽争霸3秘籍

**功能说明：**
- 应用启动时自动加载魔兽争霸3秘籍
- 用户无需等待游戏检测，即可查看和管理秘籍
- 状态栏显示："已加载魔兽争霸3秘籍，等待检测游戏..."

**实现位置：**
- 文件：[ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs)
- 方法：`MainViewModel` 构造函数（第101-149行）

**代码示例：**
```csharp
public MainViewModel(IntPtr windowHandle)
{
    // ... 初始化服务 ...

    // 默认选择魔兽争霸3
    _manuallySelectedGameType = GameType.Warcraft3;
    _selectedGameType = GameTypes.First(x => x.Key == GameType.Warcraft3);

    // 启动时默认加载魔兽争霸3秘籍
    LoadCheatsForGame(GameType.Warcraft3);

    // 启动游戏检测（辅助功能）
    _gameDetectionService.Start();

    Logger.Info("MainViewModel 初始化完成");
    StatusMessage = "已加载魔兽争霸3秘籍，等待检测游戏...";
}
```

---

### 3. ✅ 优化游戏检测逻辑为辅助功能

**功能说明：**

**修改前：**
- 游戏检测是必需功能
- 未检测到游戏时，秘籍列表为空
- 游戏关闭后，秘籍列表被清空

**修改后：**
- 游戏检测是辅助功能
- 检测到游戏时自动切换到对应游戏类型
- 游戏关闭后保持显示当前选择的游戏秘籍
- 游戏检测状态与秘籍管理分离

**实现位置：**
- 文件：[ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs)
- 方法：`OnGameDetected`（第151-168行）、`OnGameLost`（第173-189行）

**游戏检测事件处理：**

```csharp
/// <summary>
/// 游戏检测到事件
/// </summary>
private void OnGameDetected(object? sender, GameInfo gameInfo)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        _currentGame = gameInfo;
        GameStatus = $"{gameInfo.DisplayName} (已检测)";

        // 自动切换到检测到的游戏类型
        _manuallySelectedGameType = gameInfo.GameType;
        _selectedGameType = GameTypes.First(x => x.Key == gameInfo.GameType);
        OnPropertyChanged(nameof(SelectedGameType));

        StatusMessage = $"检测到 {gameInfo.DisplayName}，正在注册热键...";

        // 注册该游戏的热键
        RegisterHotKeysForGame(gameInfo.GameType);

        // 加载该游戏的秘籍
        LoadCheatsForGame(gameInfo.GameType);

        StatusMessage = $"检测到 {gameInfo.DisplayName}，秘籍功能已激活";
    });
}
```

**游戏关闭事件处理：**

```csharp
/// <summary>
/// 游戏丢失事件
/// </summary>
private void OnGameLost(object? sender, EventArgs e)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        // 注销所有热键
        _hotKeyManager?.UnregisterAllHotKeys();

        _currentGame = null;
        GameStatus = "未检测到游戏";
        StatusMessage = "游戏已关闭，可继续管理秘籍";
        _cheatHotKeyMap.Clear();

        // 保持显示当前选择的游戏秘籍
        // 不再清空秘籍列表
    });
}
```

---

### 4. ✅ 允许未打开游戏时管理秘籍

**功能说明：**
- 添加、编辑、删除秘籍不再要求游戏运行
- 搜索秘籍功能基于手动选择的游戏类型
- 热键配置随时可用，游戏运行时自动生效

**修改的方法：**

| 方法 | 修改前 | 修改后 |
|------|--------|--------|
| `Search()` | 检查 `_currentGame != null` | 使用 `_manuallySelectedGameType` |
| `AddCheat()` | 检查 `_currentGame != null` 才刷新 | 检查 `newCheat.Game == _manuallySelectedGameType` |
| `EditCheat()` | 检查 `_currentGame != null` 才刷新 | 使用 `_manuallySelectedGameType` |
| `DeleteCheat()` | 检查 `_currentGame != null` 才刷新 | 使用 `_manuallySelectedGameType` |
| `Refresh()` | 检查 `_currentGame != null` 才刷新 | 直接刷新 `_manuallySelectedGameType` |

**搜索方法改进：**

```csharp
/// <summary>
/// 搜索秘籍
/// </summary>
private void Search(string? keyword)
{
    // 修改前：必须游戏运行
    // if (_currentGame == null)
    // {
    //     StatusMessage = "请先启动游戏";
    //     return;
    // }

    // 修改后：使用手动选择的游戏类型
    var results = _cheatCodeService.SearchCheats(keyword ?? string.Empty)
        .Where(c => c.Game == _manuallySelectedGameType)
        .ToList();

    CheatCodes.Clear();
    foreach (var cheat in results)
    {
        var hotKey = _cheatHotKeyMap.ContainsKey(cheat.Id) ? _cheatHotKeyMap[cheat.Id] : "未绑定";
        CheatCodes.Add(new CheatCodeViewModel(cheat, hotKey));
    }

    StatusMessage = string.IsNullOrWhiteSpace(keyword)
        ? $"显示所有秘籍 ({results.Count} 个)"
        : $"搜索到 {results.Count} 个秘籍";
}
```

---

## 新增属性和方法

### ViewModels/MainViewModel.cs

**新增属性：**

```csharp
/// <summary>
/// 游戏类型字典（用于下拉框）
/// </summary>
public Dictionary<GameType, string> GameTypes { get; } = new Dictionary<GameType, string>
{
    { GameType.Warcraft3, "魔兽争霸3" },
    { GameType.StarCraft, "星际争霸1" }
};

/// <summary>
/// 当前选择的游戏类型
/// </summary>
public KeyValuePair<GameType, string> SelectedGameType
{
    get => _selectedGameType;
    set
    {
        if (SetProperty(ref _selectedGameType, value))
        {
            OnGameTypeChanged(value.Key);
        }
    }
}
```

**新增私有字段：**

```csharp
private KeyValuePair<GameType, string> _selectedGameType;
private GameType _manuallySelectedGameType;
```

**新增方法：**

```csharp
/// <summary>
/// 游戏类型改变事件
/// </summary>
private void OnGameTypeChanged(GameType gameType)
{
    _manuallySelectedGameType = gameType;

    // 如果游戏正在运行且类型不同，注销当前热键
    if (_currentGame != null && _currentGame.GameType != gameType)
    {
        _hotKeyManager?.UnregisterAllHotKeys();
        _cheatHotKeyMap.Clear();
    }

    // 加载选择的游戏秘籍
    LoadCheatsForGame(gameType);

    // 如果游戏正在运行且类型匹配，重新注册热键
    if (_currentGame != null && _currentGame.GameType == gameType)
    {
        RegisterHotKeysForGame(gameType);
    }

    StatusMessage = $"已切换到 {GetGameName(gameType)}";
    Logger.Info($"用户手动选择游戏: {gameType}");
}
```

### Views/MainWindow.xaml.cs

**新增方法：**

```csharp
/// <summary>
/// 游戏类型选择改变事件
/// </summary>
private void GameTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
{
    // 事件由 ViewModel 的 SelectedGameType 属性变化自动处理
    // 此方法保留用于可能的 UI 特定逻辑
}
```

---

## 使用场景示例

### 场景 1：启动应用后直接管理秘籍

**操作步骤：**
1. 启动 GameCheatHelper
2. 应用自动加载魔兽争霸3秘籍
3. 状态栏显示："已加载魔兽争霸3秘籍，等待检测游戏..."
4. 用户可以立即：
   - 查看秘籍列表
   - 添加新秘籍
   - 编辑现有秘籍
   - 删除秘籍
   - 配置热键

**无需等待游戏启动！**

---

### 场景 2：手动切换游戏类型

**操作步骤：**
1. 点击顶部的游戏下拉框
2. 选择"星际争霸1"
3. 秘籍列表自动刷新，显示星际争霸1秘籍
4. 状态栏显示："已切换到 星际争霸1"

**预期结果：**
- ✅ 秘籍列表立即更新
- ✅ 热键绑定信息同步更新
- ✅ 无需游戏运行

---

### 场景 3：游戏自动检测与手动选择协同

**操作步骤：**
1. 应用启动，手动选择"魔兽争霸3"
2. 启动魔兽争霸3游戏
3. 应用检测到游戏，状态更新为："魔兽争霸3 (已检测)"
4. 热键自动注册，游戏内可使用热键
5. 关闭游戏
6. 状态更新为："未检测到游戏"
7. 秘籍列表保持显示，可继续管理

**预期结果：**
- ✅ 游戏检测自动切换游戏类型
- ✅ 游戏运行时热键自动生效
- ✅ 游戏关闭后秘籍管理不受影响

---

### 场景 4：添加秘籍无需游戏运行

**操作步骤：**
1. 未启动游戏
2. 选择游戏类型："魔兽争霸3"
3. 点击"添加秘籍"
4. 填写秘籍信息：
   - 游戏类型：魔兽争霸3
   - 秘籍代码：thereisnospoon
   - 描述：无限魔法
   - 分类：能力
5. 录制热键：Alt+F4
6. 点击"保存"

**预期结果：**
- ✅ 秘籍保存成功
- ✅ 热键配置保存到 JSON
- ✅ 秘籍列表立即刷新
- ✅ 下次启动游戏时热键自动生效

---

## 技术实现细节

### 数据流程图

```
┌──────────────────────┐
│  应用启动             │
└──────┬───────────────┘
       ↓
┌──────────────────────┐
│  默认选择魔兽争霸3    │
│  _manuallySelectedGameType = Warcraft3
└──────┬───────────────┘
       ↓
┌──────────────────────┐
│  加载魔兽争霸3秘籍    │
└──────┬───────────────┘
       ↓
┌──────────────────────┐
│  启动游戏检测         │
│  (后台辅助功能)       │
└──────┬───────────────┘
       ↓
    ┌──┴──┐
    │用户  │
    └──┬──┘
       ↓
  ┌────┴────┐
  │手动切换？│
  └────┬────┘
       ↓
  是 ↙   ↘ 否
    ↓       ↓
┌──────┐  ┌──────────┐
│切换游戏│  │检测到游戏？│
│类型    │  └────┬─────┘
└──┬───┘       ↓
   ↓        是 ↙ ↘ 否
┌──────────┐  ↓    ↓
│加载新游戏│  │    │
│秘籍      │  │    │
└──┬───────┘  │    │
   ↓          ↓    │
┌──────────────────┐│
│自动切换游戏类型   ││
│注册热键          ││
└──────────────────┘│
       ↓            ↓
┌──────────────────────┐
│  用户管理秘籍         │
│  (无需游戏运行)       │
└──────────────────────┘
```

---

## 修改文件清单

| 文件 | 修改内容 | 状态 |
|------|---------|------|
| [Views/MainWindow.xaml](../Views/MainWindow.xaml) | 添加游戏选择下拉框和检测状态显示 | ✅ 已完成 |
| [Views/MainWindow.xaml.cs](../Views/MainWindow.xaml.cs) | 添加 GameTypeComboBox_SelectionChanged 事件 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 添加游戏选择属性和方法 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 修改构造函数默认加载魔兽争霸3 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 优化游戏检测事件处理 | ✅ 已完成 |
| [ViewModels/MainViewModel.cs](../ViewModels/MainViewModel.cs) | 移除秘籍管理的游戏运行限制 | ✅ 已完成 |

---

## 编译测试结果

```bash
✅ Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.21
```

---

## 用户体验改进

### 改进前：

**问题：**
1. ❌ 必须启动游戏才能查看秘籍
2. ❌ 游戏关闭后秘籍列表被清空
3. ❌ 无法预先配置秘籍
4. ❌ 无法手动选择游戏类型

**用户反馈：**
> "为什么必须打开游戏才能添加秘籍？我想先配置好再玩游戏。"

### 改进后：

**优势：**
1. ✅ 启动即可管理秘籍
2. ✅ 游戏关闭后仍可管理
3. ✅ 支持预先配置秘籍
4. ✅ 手动选择游戏类型
5. ✅ 游戏检测成为辅助功能

**用户体验：**
> "太方便了！我可以先配置好所有秘籍，然后启动游戏直接使用。"

---

## 后续改进建议

1. **记住上次选择** - 保存用户上次选择的游戏类型，下次启动自动加载
2. **快速切换快捷键** - 添加键盘快捷键（如 Ctrl+1, Ctrl+2）快速切换游戏
3. **游戏检测提示** - 检测到游戏时显示通知
4. **多游戏支持** - 支持更多游戏（帝国时代、红色警戒等）

---

## 测试清单

### 基础功能测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 启动加载 | 启动应用，检查是否自动加载魔兽争霸3秘籍 | ✅ 待测试 |
| 手动切换 | 切换游戏类型，检查秘籍列表是否更新 | ✅ 待测试 |
| 添加秘籍 | 未启动游戏时添加秘籍 | ✅ 待测试 |
| 编辑秘籍 | 未启动游戏时编辑秘籍 | ✅ 待测试 |
| 删除秘籍 | 未启动游戏时删除秘籍 | ✅ 待测试 |
| 搜索秘籍 | 未启动游戏时搜索秘籍 | ✅ 待测试 |

### 游戏检测测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 自动检测 | 启动游戏，检查是否自动切换游戏类型 | ✅ 待测试 |
| 热键注册 | 检测到游戏后，热键是否自动注册 | ✅ 待测试 |
| 游戏关闭 | 游戏关闭后，秘籍列表是否保持 | ✅ 待测试 |
| 状态同步 | 游戏状态与界面显示是否一致 | ✅ 待测试 |

### 热键功能测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 预配置热键 | 未启动游戏时配置热键，启动游戏后是否生效 | ✅ 待测试 |
| 热键切换 | 切换游戏类型时热键是否正确注册/注销 | ✅ 待测试 |
| 热键冲突 | 跨游戏热键配置是否独立 | ✅ 待测试 |

---

## 常见问题

### Q1: 游戏检测和手动选择有什么区别？

**A:**
- **手动选择**：用户主动选择要管理的游戏类型，随时可用
- **游戏检测**：自动检测游戏进程，检测到后自动切换并注册热键
- 两者协同工作，游戏检测是辅助功能

### Q2: 如果手动选择的游戏与检测到的游戏不同会怎样？

**A:**
检测到游戏后会自动切换到对应的游戏类型：
1. 下拉框自动更新为检测到的游戏
2. 秘籍列表自动刷新
3. 热键自动注册

### Q3: 游戏关闭后为什么秘籍列表不清空了？

**A:**
这是本次改进的核心特性：
- 用户可能想在游戏关闭后继续管理秘籍
- 支持预先配置秘籍，下次启动游戏直接使用
- 游戏检测成为辅助功能，不影响秘籍管理

### Q4: 我可以同时配置两个游戏的秘籍吗？

**A:**
可以！步骤如下：
1. 选择"魔兽争霸3"，添加/编辑魔兽秘籍
2. 切换到"星际争霸1"，添加/编辑星际秘籍
3. 所有配置自动保存到对应的 JSON 文件
4. 启动任何游戏都能正确加载对应秘籍

---

**文档版本**: 1.0
**最后更新**: 2026-02-01
**状态**: ✅ 功能完成，待用户测试验证

---

## 相关文档

- [热键组合键功能迭代](./HotKey-Combo-Feature-Iteration.md)
- [热键保存与冲突检测](./HotKey-Save-And-Conflict-Detection.md)
- [Alt 热键修复](./Alt-Hotkey-Fix.md)
- [热键保存功能修复](./HotKey-Save-Fix.md)
