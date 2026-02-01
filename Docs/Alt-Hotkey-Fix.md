# Alt 组合键录制问题修复说明

## 问题描述

用户反馈在热键录制控件中，按下 Alt 组合键（如 Alt+F1、Alt+F2）时无法正确录制。

## 问题原因

在 WPF 中，键盘事件处理 Alt 键时有特殊行为：

1. **正常按键**：`e.Key` 直接返回按下的键（如 `Key.F1`）
2. **Alt 组合键**：`e.Key` 返回 `Key.System`，实际按键需要从 `e.SystemKey` 获取

### 原代码问题

```csharp
// 忽略单独的修饰键
if (key == Key.LeftCtrl || key == Key.RightCtrl ||
    key == Key.LeftAlt || key == Key.RightAlt ||
    key == Key.LeftShift || key == Key.RightShift ||
    key == Key.LWin || key == Key.RWin ||
    key == Key.System)  // ❌ 问题：这里会忽略所有 Alt 组合键
{
    return;
}
```

当用户按下 `Alt+F1` 时：
1. `e.Key` 返回 `Key.System`
2. 在第 106 行被忽略，直接 `return`
3. 后续的热键创建代码无法执行
4. **结果：Alt 组合键无法录制**

## 解决方案

### 修复后的代码

```csharp
// 处理 Alt 组合键的特殊情况
// 在 WPF 中，Alt+其他键会使 e.Key 返回 Key.System
if (key == Key.System)
{
    key = e.SystemKey;  // ✅ 先转换为实际按键
}

// 忽略单独的修饰键
if (key == Key.LeftCtrl || key == Key.RightCtrl ||
    key == Key.LeftAlt || key == Key.RightAlt ||
    key == Key.LeftShift || key == Key.RightShift ||
    key == Key.LWin || key == Key.RWin)  // ✅ 移除了 Key.System
{
    return;
}
```

### 执行流程（修复后）

当用户按下 `Alt+F1` 时：

1. **接收事件**：`e.Key` = `Key.System`
2. **转换按键**：检测到 `Key.System`，转换为 `key` = `e.SystemKey` (F1)
3. **检查修饰键**：F1 不是修饰键，继续执行
4. **收集修饰键**：`Keyboard.Modifiers` 检测到 `ModifierKeys.Alt`
5. **创建热键**：`{ Key = F1, Modifiers = MOD_ALT }`
6. **显示结果**：`DisplayText` = "Alt+F1"

## 测试验证

### 测试用例

| 组合键 | 预期结果 | 测试状态 |
|--------|---------|---------|
| Alt+F1 | 显示 "Alt+F1" | ✅ 待测试 |
| Alt+F2 | 显示 "Alt+F2" | ✅ 待测试 |
| Ctrl+Alt+F1 | 显示 "Ctrl+Alt+F1" | ✅ 待测试 |
| Shift+Alt+F1 | 显示 "Shift+Alt+F1" | ✅ 待测试 |
| Alt+A | 显示 "Alt+A" | ✅ 待测试 |

### 测试步骤

1. **启动应用程序**
   ```bash
   dotnet run
   ```

2. **打开秘籍编辑对话框**
   - 点击"添加秘籍"或"编辑"按钮

3. **测试热键录制**
   - 点击"快捷键"输入框
   - 按下 `Alt+F1`
   - 验证显示 "Alt+F1"

4. **测试其他组合**
   - 测试 `Ctrl+Alt+F1`
   - 测试 `Shift+Alt+F2`
   - 测试 `Alt+A`、`Alt+B` 等字母键

5. **保存测试**
   - 保存秘籍
   - 检查 `Data/DefaultHotKeys.json`
   - 验证 `modifiers` 值包含 Alt (1 或 3, 5, 7, 9 等)

## 技术细节

### WPF 键盘事件特殊性

| 按键场景 | e.Key | e.SystemKey | 说明 |
|---------|-------|-------------|------|
| 单独 F1 | F1 | None | 普通按键 |
| Ctrl+F1 | F1 | None | Ctrl 不改变 e.Key |
| Alt+F1 | System | F1 | ✅ Alt 会改变 e.Key |
| Shift+F1 | F1 | None | Shift 不改变 e.Key |

### 修饰键位掩码

```csharp
// Alt 组合键的 Modifiers 值
MOD_ALT = 0x0001              // 1  - 单独 Alt
MOD_CONTROL | MOD_ALT = 0x0003 // 3  - Ctrl+Alt
MOD_SHIFT | MOD_ALT = 0x0005   // 5  - Shift+Alt
MOD_ALT | MOD_WIN = 0x0009     // 9  - Alt+Win
// ... 更多组合
```

## 相关文件

- **修复文件**：[Controls/HotKeyRecorder.xaml.cs](../Controls/HotKeyRecorder.xaml.cs)
- **修改行数**：92-147 行（`HotKeyTextBox_PreviewKeyDown` 方法）
- **提交信息**：修复 Alt 组合键录制问题

## 编译测试结果

```bash
✅ Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:07.61
```

## 后续建议

1. **扩展测试**：添加单元测试覆盖所有修饰键组合
2. **UI 提示**：在界面上提示 Alt 键的特殊性
3. **文档更新**：在用户指南中补充 Alt 键说明

---

**修复日期**：2026-02-01
**修复版本**：v1.1-hotkey-combo-fix
**状态**：✅ 已修复，待测试验证
