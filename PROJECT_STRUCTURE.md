# GameCheatHelper - 项目结构

## 📁 目录结构

```
GameCheatHelper/
│
├── Core/                           # 核心逻辑层
│   ├── Models/                     # 数据模型
│   │   ├── GameInfo.cs             ✅ 游戏信息模型
│   │   ├── CheatCode.cs            ✅ 秘籍代码模型
│   │   ├── HotKey.cs               ✅ 热键模型
│   │   └── AppConfig.cs            ✅ 应用配置模型
│   ├── CheatExecutor.cs            ✅ 秘籍执行引擎
│   └── KeyboardSimulator.cs        ✅ 键盘输入模拟器
│
├── Services/                       # 服务层（业务逻辑）
│   ├── GameDetectionService.cs     ✅ 游戏检测服务
│   ├── HotKeyManager.cs            ✅ 全局热键管理器
│   ├── CheatCodeService.cs         ✅ 秘籍代码管理服务
│   └── ConfigService.cs            ✅ 配置管理服务
│
├── ViewModels/                     # MVVM 视图模型层
│   ├── ViewModelBase.cs            ✅ ViewModel 基类
│   └── MainViewModel.cs            ✅ 主窗口 ViewModel
│
├── Views/                          # WPF 视图层
│   ├── MainWindow.xaml             ✅ 主窗口界面
│   └── MainWindow.xaml.cs          ✅ 主窗口代码后置
│
├── Utilities/                      # 工具类
│   ├── Win32API.cs                 ✅ Windows API 封装
│   └── RelayCommand.cs             ✅ MVVM 命令实现
│
├── Data/                           # 数据文件
│   └── DefaultCheats.json          ✅ 默认秘籍库（27个秘籍）
│
├── Docs/                           # 文档目录
│   ├── SPEC.md                     ✅ 产品规格说明书
│   ├── Task.md                     ✅ 任务分解文档
│   ├── Development.md              ✅ 开发文档
│   ├── Design.md                   ✅ 系统设计文档
│   └── Iteration.md                ✅ 迭代计划文档
│
├── App.xaml                        ✅ 应用程序资源定义
├── App.xaml.cs                     ✅ 应用程序入口点
├── NLog.config                     ✅ 日志配置文件
├── GameCheatHelper.csproj          ✅ 项目文件
├── README.md                       ✅ 项目说明文档
└── .gitignore                      ✅ Git 忽略文件

```

## 📊 项目统计

### 代码文件
- **C# 类文件**: 15 个
- **XAML 文件**: 2 个
- **配置文件**: 3 个
- **数据文件**: 1 个
- **文档文件**: 6 个

### 代码行数（估算）
- 核心层：~800 行
- 服务层：~600 行
- 视图模型层：~350 行
- 视图层：~250 行
- 工具类：~400 行
- **总计约**: 2400+ 行代码

### 功能模块
✅ **已完成的核心模块**:
1. Windows API 封装（键盘模拟、窗口管理、热键注册）
2. 游戏进程检测（支持魔兽争霸3和星际争霸1）
3. 全局热键管理（注册、注销、事件监听）
4. 秘籍执行引擎（自动输入秘籍代码）
5. 秘籍管理服务（加载、搜索、增删改查）
6. 配置管理服务（JSON持久化、导入导出）
7. MVVM 架构（ViewModelBase、RelayCommand）
8. 主窗口 UI 和数据绑定

## 🎮 内置秘籍

### 魔兽争霸3（13个秘籍）
- greedisgood - 获得金木资源
- iseedeadpeople - 显示全图
- whosyourdaddy - 无敌模式
- thereisnospoon - 无限魔法
- warpten - 快速建造
- synergy - 关闭科技树
- 以及更多...

### 星际争霸1（14个秘籍）
- show me the money - 获得矿物和气体
- black sheep wall - 显示全图
- power overwhelming - 无敌模式
- operation cwal - 快速建造
- the gathering - 无限能量
- 以及更多...

## 🏗️ 架构设计

### 分层架构
```
┌─────────────────────────────────┐
│   Presentation Layer (Views)    │  WPF UI
├─────────────────────────────────┤
│   ViewModel Layer               │  MVVM 模式
├─────────────────────────────────┤
│   Business Layer (Services)     │  业务逻辑
├─────────────────────────────────┤
│   Core Layer (Models + Logic)   │  核心功能
├─────────────────────────────────┤
│   Infrastructure (Win32 API)    │  底层 API
└─────────────────────────────────┘
```

### 技术栈
- **框架**: .NET 8.0 (Windows)
- **UI**: WPF (Windows Presentation Foundation)
- **模式**: MVVM (Model-View-ViewModel)
- **日志**: NLog
- **JSON**: Newtonsoft.Json
- **API**: Windows API (User32.dll)

## 🚀 快速开始

### 1. 编译项目
```bash
dotnet restore
dotnet build
```

### 2. 运行项目
```bash
dotnet run
```

### 3. 发布项目
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## 📋 待实现功能

根据开发文档，以下功能需要后续实现：

### 第一优先级（P0）
- [ ] 热键与秘籍的绑定配置
- [ ] 热键配置持久化
- [ ] 完整的错误处理和日志记录

### 第二优先级（P1）
- [ ] 设置窗口（输入延迟、开机自启、托盘等）
- [ ] 秘籍编辑对话框
- [ ] 秘籍添加/删除功能
- [ ] 秘籍搜索功能

### 第三优先级（P2）
- [ ] 系统托盘功能
- [ ] 自定义热键绑定界面
- [ ] 配置导入/导出界面
- [ ] 单元测试

## 📝 使用说明

### 基本流程
1. 启动 GameCheatHelper
2. 启动游戏（魔兽争霸3 或 星际争霸1）
3. 应用自动检测游戏
4. 在游戏中按下配置的热键即可自动输入秘籍

### 热键配置
目前热键需要通过代码配置，后续版本将提供 UI 界面配置。

## 🔧 开发指南

### 添加新游戏支持
1. 在 `GameType` 枚举中添加新游戏类型
2. 在 `GameDetectionService` 中添加检测逻辑
3. 在 `DefaultCheats.json` 中添加游戏秘籍

### 添加新秘籍
编辑 `Data/DefaultCheats.json` 文件，按照现有格式添加秘籍。

### 自定义输入延迟
修改配置文件中的 `Settings.InputDelay` 值（单位：毫秒）。

## 📄 许可证

MIT License

---

**项目状态**: 核心功能已完成，可编译运行 ✅
**最后更新**: 2026-01-28
**版本**: v1.0.0-alpha
