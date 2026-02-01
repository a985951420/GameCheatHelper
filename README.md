# 游戏秘籍助手 - GameCheatHelper

**开发者**: Time
**项目地址**: https://github.com/a985951420/GameCheatHelper
**版本**: v1.0.0

---

## 项目简介

GameCheatHelper 是一个基于 C# 和 Windows API 的桌面应用程序，用于在经典单机游戏（魔兽争霸3、星际争霸1）中通过快捷键快速输入官方游戏秘籍代码。

## 主要特性

- 🎮 **游戏检测**: 自动检测魔兽争霸3和星际争霸1游戏进程
- ⌨️ **全局热键**: 支持自定义全局快捷键，游戏运行时快速输入秘籍
- 📝 **秘籍管理**: 内置常用秘籍库，支持自定义添加秘籍
- ⚙️ **灵活配置**: 输入延迟、开机自启、托盘最小化等可配置
- 🎨 **友好界面**: 基于 WPF 的现代化用户界面

## 支持的游戏

### 魔兽争霸3 (Warcraft III)
- greedisgood - 获得金木资源
- iseedeadpeople - 显示全图
- whosyourdaddy - 无敌模式
- 以及更多...

### 星际争霸1 (StarCraft)
- show me the money - 获得矿物和气体
- black sheep wall - 显示全图
- power overwhelming - 无敌模式
- 以及更多...

## 系统要求

- Windows 10 (1809+) 或 Windows 11
- .NET 8.0 Runtime 或更高版本
- 管理员权限（用于全局热键）

## 快速开始

### 安装

1. 下载最新版本的 `GameCheatHelper.zip`
2. 解压到任意目录
3. 右键以管理员身份运行 `GameCheatHelper.exe`

### 使用方法

1. **启动游戏**: 先启动魔兽争霸3或星际争霸1
2. **运行工具**: 启动 GameCheatHelper，工具会自动检测游戏
3. **使用秘籍**: 在游戏中按下配置的快捷键（默认 F1-F5）即可自动输入秘籍

## 开发文档

### 项目结构

```
GameCheatHelper/
├── Core/                    # 核心逻辑
│   ├── Models/              # 数据模型
│   ├── CheatExecutor.cs     # 秘籍执行器
│   └── KeyboardSimulator.cs # 键盘模拟
├── Services/                # 业务服务
│   ├── GameDetectionService.cs
│   ├── HotKeyManager.cs
│   ├── CheatCodeService.cs
│   └── ConfigService.cs
├── ViewModels/              # MVVM 视图模型
├── Views/                   # WPF 视图
├── Utilities/               # 工具类
│   └── Win32API.cs          # Windows API 封装
└── Data/                    # 数据文件
    └── DefaultCheats.json
```

### 技术栈

- **框架**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **架构**: MVVM 模式
- **API**: Windows API (User32.dll)
- **依赖**:
  - Newtonsoft.Json - JSON 配置
  - NLog - 日志记录

### 编译项目

```bash
# 克隆项目
git clone <repository-url>
cd GameCheatHelper

# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行
dotnet run
```

### 发布

```bash
# Release 构建
dotnet publish -c Release -r win-x64 --self-contained false

# 单文件发布
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 文档

- [SPEC.md](SPEC.md) - 产品规格说明书
- [Task.md](Task.md) - 任务分解文档
- [Development.md](Development.md) - 开发文档
- [Design.md](Design.md) - 系统设计文档
- [Iteration.md](Iteration.md) - 迭代计划文档

## 常见问题

### Q: 热键无法注册？
A: 请确保以管理员权限运行程序，或更换其他未被占用的热键。

### Q: 秘籍输入失败？
A: 尝试增加输入延迟（在设置中调整），确保游戏窗口处于激活状态。

### Q: 检测不到游戏？
A: 检查游戏进程名称是否正确，部分修改版游戏可能无法识别。

## 安全说明

本工具**仅使用游戏官方提供的秘籍功能**，不会：
- ❌ 修改游戏内存
- ❌ 注入游戏进程
- ❌ 修改游戏文件
- ❌ 用于联机对战

本工具通过模拟键盘输入的方式，自动化输入秘籍代码，完全合法且安全。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License

## 联系方式

- **项目主页**: https://github.com/a985951420/GameCheatHelper
- **问题反馈**: https://github.com/a985951420/GameCheatHelper/issues
- **开发者**: Time

---

**版本**: v1.0.0
**最后更新**: 2026-02-01
