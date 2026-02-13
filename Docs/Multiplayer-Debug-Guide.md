# 多人模式功能调试指南

## 问题描述
在星际争霸1多人模式下，"给自己加钱"和"建造加速"功能不起作用。

## 可能的原因

### 1. 内存地址不正确
星际争霸1在单人模式和多人模式下，内存布局可能不同：
- 单人模式：玩家通常是固定的索引（如0）
- 多人模式：玩家索引可能动态分配，取决于游戏房间中的位置

### 2. 玩家索引假设错误
当前代码假设玩家索引始终为0（`playerIndex = 0`），但在多人模式中：
- 玩家索引可能是1-7之间的任何值
- 需要动态检测当前玩家的实际索引

### 3. 内存保护机制
多人模式可能有额外的反作弊或内存保护机制。

## 已添加的诊断功能

### v1 - 2024-01-XX: 添加详细日志
在 `MemoryCheatService.cs` 中添加了以下诊断日志：

#### 资源添加诊断
```csharp
// 1. 显示实际内存地址
Logger.Info($"🔍 矿物地址: 0x{mineralsAddr.ToInt64():X}, 气体地址: 0x{gasAddr.ToInt64():X}");

// 2. 显示读取到的值
Logger.Info($"当前资源 - 水晶矿: {currentMinerals}, 瓦斯: {currentGas}");

// 3. 检测异常值
if (currentMinerals < 0 || currentMinerals > 1000000 || currentGas < 0 || currentGas > 1000000)
{
    Logger.Warn($"⚠️ 读取到异常资源值，可能内存地址不正确");
}

// 4. 验证写入是否成功
Logger.Info($"🔍 写入后验证 - 水晶矿: {verifyMinerals}, 瓦斯: {verifyGas}");
if (verifyMinerals != expected) 
{
    Logger.Warn($"⚠️ 写入验证失败！");
}
```

#### 建造加速诊断
```csharp
// 1. 显示实际内存地址
Logger.Info($"🔍 建造加速地址: 0x{buildSpeedAddr.ToInt64():X}");

// 2. 显示当前建造速度值
Logger.Info($"当前建造速度值: {currentValue[0]}");

// 3. 验证写入
Logger.Info($"🔍 写入后验证值: {verifyValue[0]}");
if (verifyValue[0] != 0) 
{
    Logger.Warn($"⚠️ 建造加速写入验证失败！");
}
```

## 调试步骤

### 第一步：查看日志
1. 启动 GameCheatHelper
2. 打开星际争霸1多人模式游戏
3. 点击"给自己加钱"或"建造加速"按钮
4. 查看 `logs\` 目录下的日志文件

### 第二步：分析日志输出

#### 场景A：地址正确，但读到的值异常
如果看到：
```
🔍 矿物地址: 0x57F0F0, 气体地址: 0x57F120
当前资源 - 水晶矿: -1234567, 瓦斯: 999999999
⚠️ 读取到异常资源值，可能内存地址不正确
```
**说明**：内存地址在多人模式下不同，需要重新定位正确地址。

#### 场景B：读取正常，但写入失败
如果看到：
```
当前资源 - 水晶矿: 50, 瓦斯: 0
🔍 写入后验证 - 水晶矿: 50, 瓦斯: 0
⚠️ 写入验证失败！预期 1050/500，实际 50/0
```
**说明**：内存写入被阻止，可能是：
- 内存保护机制
- 需要更高权限
- 多人模式禁止修改

#### 场景C：一切正常但游戏中不生效
如果看到：
```
🔍 写入后验证 - 水晶矿: 1050, 瓦斯: 500
✅ 资源已增加
```
但游戏中资源没变化：
**说明**：
- 玩家索引可能不是0
- 游戏有额外的资源同步机制覆盖了修改

### 第三步：尝试不同玩家索引
如果怀疑玩家索引问题，可以：

1. 记录多人游戏中你的位置（例如：玩家2）
2. 临时修改代码测试不同索引：
   ```csharp
   // 在 MainViewModel.cs 的 AddStarCraftResourcesCommand 中
   // 尝试索引 1-7
   _memoryCheatService.AddStarCraftResources(scProcess.Id, 1, 1000, 500);
   ```

## 内存地址参考

### 当前使用的地址（基于v1.16.1单人模式）
```csharp
// 资源地址
SC_MINERALS_BASE = 0x0057F0F0  // 水晶矿基址
SC_GAS_BASE = 0x0057F120       // 瓦斯基址
PLAYER_ENTRY_SIZE = 0x24       // 玩家结构体大小（36字节）

// 建造速度地址
SC_BUILD_SPEED_BASE = 0x006509C0
PLAYER_BUILD_SPEED_ENTRY_SIZE = 0x01  // 每个玩家1字节

// 人口上限地址
SC_SUPPLY_PROVIDED_ZERG = 0x00582174
SC_SUPPLY_MAX_ZERG = 0x005821A4
// ... Terran/Protoss 类似
```

### 地址计算公式
```
玩家X的矿物地址 = SC_MINERALS_BASE + (玩家索引 * PLAYER_ENTRY_SIZE)
```

## 下一步计划

### 方案1：动态检测玩家索引
实现一个功能来扫描所有玩家位置，找到哪个索引的资源值与游戏界面显示一致。

### 方案2：使用内存扫描工具验证
使用 Cheat Engine 等工具：
1. 在多人模式中搜索当前资源值
2. 找到实际地址
3. 对比与代码中使用的地址差异

### 方案3：研究多人模式内存结构
查找星际争霸1多人模式的内存结构文档或逆向工程资料。

## 临时解决方案

如果调试困难，可以考虑：
1. 仅在单人模式下启用这些功能
2. 添加"仅单人模式可用"的提示
3. 或者使用游戏内置的秘籍（如 "show me the money"）

## 相关文件
- `Services/MemoryCheatService.cs` - 内存修改实现
- `Core/MemoryEditor.cs` - 底层内存读写
- `ViewModels/MainViewModel.cs` - UI命令处理
- `logs/` - 运行日志目录
