# 热键保存和冲突检测功能完善

## 更新日期
2026-02-01

## 功能概述

本次更新完善了热键配置功能，添加了以下两个关键特性：
1. ✅ **自动保存热键绑定** - 编辑秘籍时自动保存热键配置
2. ✅ **智能冲突检测** - 检测热键冲突并提示用户选择处理方式

---

## 功能详情

### 1. 自动保存热键绑定

#### 功能说明
- 用户在秘籍编辑对话框中录制热键后，点击"保存"按钮即可自动保存
- 无需手动编辑 JSON 配置文件
- 支持添加新秘籍和编辑现有秘籍的热键

#### 实现位置
- **文件**: `ViewModels/MainViewModel.cs`
- **方法**: `AddCheat()` 和 `EditCheat()`

#### 保存流程

**添加新秘籍时：**
```
1. 用户在对话框中录制热键（如 Alt+F1）
2. 点击"保存"按钮
3. 检查热键冲突（见下文）
4. 保存秘籍到 DefaultCheats.json
5. 保存热键绑定到 DefaultHotKeys.json
6. 如果游戏正在运行，重新注册热键
7. 显示成功消息："秘籍 'xxx' 已添加，热键 'Alt+F1' 已绑定"
```

**编辑现有秘籍时：**
```
1. 对话框自动加载现有热键
2. 用户修改热键或清除热键
3. 点击"保存"按钮
4. 检查热键冲突
5. 更新秘籍信息
6. 更新热键绑定到 DefaultHotKeys.json
7. 如果游戏正在运行，重新注册热键
8. 显示成功消息："秘籍 'xxx' 已更新，热键 'Ctrl+F1' 已绑定"
```

---

### 2. 智能冲突检测

#### 功能说明
当用户设置的热键已被其他秘籍使用时，系统会：
1. **自动检测冲突** - 保存前检查热键是否被占用
2. **显示冲突提示** - 弹窗告知用户具体的冲突信息
3. **询问处理方式** - 用户可选择覆盖或取消

#### 冲突检测对话框

**对话框内容：**
```
┌─────────────────────────────────────────┐
│  ⚠️ 热键冲突                               │
├─────────────────────────────────────────┤
│  热键 'Alt+F1' 已被秘籍                    │
│  '魔兽3：增加资源' 使用。                   │
│                                          │
│  是否移除原有绑定并使用新的绑定？            │
├─────────────────────────────────────────┤
│            [是(Y)]    [否(N)]             │
└─────────────────────────────────────────┘
```

#### 用户选择处理

**选择"是"**：
- 移除原秘籍的热键绑定
- 将热键绑定到新秘籍
- 显示消息："秘籍 'xxx' 已添加，热键 'Alt+F1' 已绑定"

**选择"否"**：
- 取消热键绑定
- 秘籍仍然被保存，但没有热键
- 显示消息："已取消热键绑定"

#### 实现细节

**冲突检测逻辑：**
```csharp
// 检查热键是否被占用
var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
    viewModel.CurrentHotKey,
    excludeCheatCodeId  // 编辑时排除当前秘籍自己
);

if (conflictingCheatId != null)
{
    // 找到冲突的秘籍详情
    var conflictingCheat = _cheatCodeService.GetCheatById(conflictingCheatId);

    // 显示冲突对话框
    var result = MessageBox.Show(
        $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被秘籍 '{conflictingCheat.Description}' 使用。\n\n是否移除原有绑定并使用新的绑定？",
        "热键冲突",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning
    );

    // 处理用户选择
    if (result != MessageBoxResult.Yes)
    {
        viewModel.CurrentHotKey = null;  // 清除热键
    }
}
```

---

## 使用场景示例

### 场景 1：添加新秘籍并设置热键

**操作步骤：**
1. 点击"添加秘籍"按钮
2. 填写秘籍信息：
   - 游戏类型：魔兽争霸3
   - 秘籍代码：keysersoze
   - 描述：增加黄金
3. 点击"快捷键"输入框
4. 按下 `Ctrl+Alt+F1`
5. 输入框显示 "Ctrl+Alt+F1"
6. 点击"保存"按钮

**预期结果：**
- ✅ 秘籍已添加到 `DefaultCheats.json`
- ✅ 热键绑定已保存到 `DefaultHotKeys.json`
- ✅ 状态栏显示："秘籍 'keysersoze' 已添加，热键 'Ctrl+Alt+F1' 已绑定"

---

### 场景 2：热键冲突 - 用户选择覆盖

**前置条件：**
- 热键 `Alt+F1` 已被秘籍 "greedisgood" 使用

**操作步骤：**
1. 点击"添加秘籍"按钮
2. 填写新秘籍信息
3. 按下 `Alt+F1` 录制热键
4. 点击"保存"按钮

**预期行为：**
1. ✅ 弹出冲突对话框：
   ```
   热键 'Alt+F1' 已被秘籍 '魔兽3：greedisgood' 使用。

   是否移除原有绑定并使用新的绑定？
   ```
2. 用户点击"是"
3. ✅ 移除 "greedisgood" 的热键绑定
4. ✅ 将 `Alt+F1` 绑定到新秘籍
5. ✅ 状态栏显示："秘籍 'xxx' 已添加，热键 'Alt+F1' 已绑定"

---

### 场景 3：热键冲突 - 用户取消绑定

**前置条件：**
- 热键 `Ctrl+F1` 已被秘籍 "iseedeadpeople" 使用

**操作步骤：**
1. 点击"添加秘籍"按钮
2. 填写新秘籍信息
3. 按下 `Ctrl+F1` 录制热键
4. 点击"保存"按钮

**预期行为：**
1. ✅ 弹出冲突对话框
2. 用户点击"否"
3. ✅ 新秘籍被保存，但没有热键绑定
4. ✅ 原秘籍 "iseedeadpeople" 的热键保持不变
5. ✅ 状态栏显示："已取消热键绑定"

---

### 场景 4：编辑秘籍并更换热键

**前置条件：**
- 秘籍 "greedisgood" 当前热键是 `Ctrl+F1`
- 热键 `Alt+F2` 已被秘籍 "iseedeadpeople" 使用

**操作步骤：**
1. 选择秘籍 "greedisgood"
2. 点击"编辑"按钮
3. 对话框显示当前热键 "Ctrl+F1"
4. 点击热键输入框，按下 `Alt+F2`
5. 点击"保存"按钮

**预期行为：**
1. ✅ 弹出冲突对话框
2. 用户点击"是"（覆盖）
3. ✅ 移除 "iseedeadpeople" 的热键绑定
4. ✅ 将 `Alt+F2` 绑定到 "greedisgood"
5. ✅ "greedisgood" 的原热键 `Ctrl+F1` 被释放
6. ✅ 状态栏显示："秘籍 'greedisgood' 已更新，热键 'Alt+F2' 已绑定"

---

## 技术实现

### 修改文件清单

| 文件 | 修改内容 |
|------|---------|
| `ViewModels/MainViewModel.cs` | 添加热键冲突检测和处理逻辑 |
| `ViewModels/HotKeyConflictResult.cs` | 新增冲突检测结果类（未使用） |

### 核心代码片段

#### 添加秘籍时的冲突检测（MainViewModel.cs）

```csharp
// 检查热键冲突
if (viewModel.CurrentHotKey != null)
{
    var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
        viewModel.CurrentHotKey,
        null  // 新建秘籍，不需要排除
    );

    if (conflictingCheatId != null)
    {
        // 显示冲突对话框
        var conflictingCheat = _cheatCodeService.GetCheatById(conflictingCheatId);
        var result = MessageBox.Show(
            $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被秘籍 '{conflictingCheat.Description}' 使用。\n\n是否移除原有绑定并使用新的绑定？",
            "热键冲突",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
        {
            viewModel.CurrentHotKey = null;  // 用户选择不覆盖
            StatusMessage = "已取消热键绑定";
        }
    }
}
```

#### 编辑秘籍时的冲突检测（MainViewModel.cs）

```csharp
// 检查热键冲突（排除当前秘籍自己）
if (viewModel.CurrentHotKey != null)
{
    var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
        viewModel.CurrentHotKey,
        updatedCheat.Id  // 排除当前秘籍
    );

    if (conflictingCheatId != null)
    {
        // 显示冲突对话框...
        if (result != MessageBoxResult.Yes)
        {
            // 恢复原有热键
            viewModel.CurrentHotKey = existingBinding?.HotKey;
            StatusMessage = "已取消热键更改";
        }
    }
}
```

---

## 数据流程图

### 添加秘籍 + 热键保存流程

```
┌─────────────────┐
│  用户点击"添加"    │
└────────┬────────┘
         ↓
┌─────────────────┐
│  填写秘籍信息     │
│  录制热键        │
└────────┬────────┘
         ↓
┌─────────────────┐
│  点击"保存"       │
└────────┬────────┘
         ↓
┌─────────────────┐
│  验证基本信息     │
└────────┬────────┘
         ↓
┌──────────────────────┐
│  检查热键冲突         │
│  (CheckHotKeyOccupied)│
└────────┬─────────────┘
         ↓
    ┌────┴────┐
    │ 有冲突？ │
    └────┬────┘
         ↓
    是 ↙   ↘ 否
      ↓       ↓
┌─────────┐  ┌──────────┐
│显示对话框│  │直接保存   │
│询问覆盖？│  │          │
└────┬────┘  └────┬─────┘
     ↓            ↓
  是 ↙ ↘ 否     ┌──────────┐
    ↓   ↓      │保存秘籍   │
┌────┐ ┌───┐  │保存热键   │
│覆盖│ │取消│  │注册热键   │
└──┬─┘ └─┬─┘  └────┬─────┘
   ↓     ↓         ↓
┌──────────────────────┐
│  更新UI和状态消息      │
└──────────────────────┘
```

---

## 编译测试结果

```bash
✅ Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:03.56
```

---

## 功能测试清单

### 基础功能测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 保存新热键 | 添加秘籍并设置热键，检查 JSON 文件 | ✅ 待测试 |
| 更新热键 | 编辑秘籍并修改热键，检查 JSON 文件 | ✅ 待测试 |
| 清除热键 | 编辑秘籍并清除热键（点击 ✕） | ✅ 待测试 |
| 状态消息 | 保存后显示正确的状态消息 | ✅ 待测试 |

### 冲突检测测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 检测新增冲突 | 添加秘籍时使用已占用的热键 | ✅ 待测试 |
| 检测编辑冲突 | 编辑秘籍时使用已占用的热键 | ✅ 待测试 |
| 排除自身 | 编辑秘籍但不修改热键，不应提示冲突 | ✅ 待测试 |
| 用户选择覆盖 | 冲突时选择"是"，检查绑定是否更新 | ✅ 待测试 |
| 用户取消绑定 | 冲突时选择"否"，检查绑定是否保持 | ✅ 待测试 |

### 集成测试

| 测试项 | 测试内容 | 状态 |
|--------|---------|------|
| 游戏运行时 | 修改热键后，游戏中热键立即生效 | ✅ 待测试 |
| 重启应用 | 重启后热键配置正确加载 | ✅ 待测试 |
| JSON 同步 | 修改后 DefaultHotKeys.json 内容正确 | ✅ 待测试 |

---

## 已知限制

1. **单个热键绑定** - 每个秘籍只能绑定一个热键
2. **手动编辑 JSON** - 如果用户手动编辑 JSON 文件可能导致数据不一致

---

## 后续改进建议

1. **批量热键管理界面** - 提供统一的热键管理界面
2. **热键模板** - 预设常用的热键组合方案
3. **热键导入导出** - 支持热键配置的备份和恢复
4. **热键使用统计** - 记录热键使用频率，优化推荐

---

**文档版本**: 1.0
**最后更新**: 2026-02-01
**状态**: ✅ 功能完成，待测试验证
