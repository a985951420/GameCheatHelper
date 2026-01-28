# 游戏秘籍快捷输入工具 - 开发文档

## 1. 开发环境配置

### 1.1 系统要求
- Windows 10 (1809+) 或 Windows 11
- .NET 6.0 SDK 或更高版本
- Visual Studio 2022 或 JetBrains Rider

### 1.2 推荐的 Visual Studio 工作负载
- .NET 桌面开发
- 通用 Windows 平台开发 (可选)

### 1.3 必需的开发工具
- Git for Windows
- NuGet Package Manager
- WPF Designer

### 1.4 推荐的扩展
- ReSharper / IntelliCode
- CodeMaid (代码清理)
- XAML Styler (XAML 格式化)

### 1.5 项目克隆与初始化

```bash
# 克隆项目
git clone <repository-url>
cd GameCheatHelper

# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run --project GameCheatHelper/GameCheatHelper.csproj
```

---

## 2. 项目结构

### 2.1 解决方案结构

```
GameCheatHelper/
│
├── GameCheatHelper/                # 主项目
│   ├── Core/                       # 核心逻辑层
│   │   ├── Models/                 # 数据模型
│   │   │   ├── GameInfo.cs
│   │   │   ├── CheatCode.cs
│   │   │   ├── HotKey.cs
│   │   │   └── AppConfig.cs
│   │   ├── CheatExecutor.cs        # 秘籍执行引擎
│   │   └── KeyboardSimulator.cs    # 键盘模拟器
│   │
│   ├── Services/                   # 服务层
│   │   ├── GameDetectionService.cs
│   │   ├── HotKeyManager.cs
│   │   ├── CheatCodeService.cs
│   │   └── ConfigService.cs
│   │
│   ├── ViewModels/                 # MVVM 视图模型
│   │   ├── ViewModelBase.cs
│   │   ├── MainViewModel.cs
│   │   ├── CheatListViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Views/                      # WPF 视图
│   │   ├── MainWindow.xaml
│   │   ├── CheatListView.xaml
│   │   ├── CheatEditDialog.xaml
│   │   └── SettingsWindow.xaml
│   │
│   ├── Utilities/                  # 工具类
│   │   ├── Win32API.cs             # Windows API 封装
│   │   ├── RelayCommand.cs         # MVVM 命令
│   │   └── Logger.cs               # 日志工具
│   │
│   ├── Resources/                  # 资源文件
│   │   ├── Icons/
│   │   ├── Styles/
│   │   └── Themes/
│   │
│   ├── Data/                       # 数据文件
│   │   └── DefaultCheats.json
│   │
│   ├── App.xaml                    # 应用程序入口
│   ├── App.xaml.cs
│   └── NLog.config                 # 日志配置
│
├── GameCheatHelper.Tests/          # 测试项目
│   ├── CoreTests/
│   ├── ServicesTests/
│   └── ViewModelTests/
│
├── Docs/                           # 文档目录
│   ├── SPEC.md
│   ├── Task.md
│   ├── Development.md
│   ├── Design.md
│   └── Iteration.md
│
├── .gitignore
├── README.md
└── GameCheatHelper.sln             # 解决方案文件
```

### 2.2 命名空间约定

```csharp
GameCheatHelper                      // 根命名空间
├── Core                             // 核心逻辑
│   └── Models                       // 数据模型
├── Services                         // 业务服务
├── ViewModels                       // 视图模型
├── Views                            // 视图
└── Utilities                        // 工具类
```

---

## 3. 核心模块开发指南

### 3.1 Windows API 封装 (Win32API.cs)

#### 3.1.1 P/Invoke 声明规范

```csharp
using System;
using System.Runtime.InteropServices;

namespace GameCheatHelper.Utilities
{
    public static class Win32API
    {
        // 用户输入相关
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // 窗口管理
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // 热键管理
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 常量定义
        public const int WM_HOTKEY = 0x0312;

        // 修饰键
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint Type;
            public INPUTUNION Union;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT MouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT KeyboardInput;
            [FieldOffset(0)]
            public HARDWAREINPUT HardwareInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        // 输入类型
        public const uint INPUT_KEYBOARD = 1;

        // 键盘事件标志
        public const uint KEYEVENTF_KEYDOWN = 0x0000;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
    }
}
```

### 3.2 键盘模拟器 (KeyboardSimulator.cs)

#### 3.2.1 实现示例

```csharp
using System;
using System.Threading;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    public class KeyboardSimulator
    {
        private readonly int _delayBetweenKeys; // 毫秒

        public KeyboardSimulator(int delayBetweenKeys = 10)
        {
            _delayBetweenKeys = delayBetweenKeys;
        }

        /// <summary>
        /// 输入文本字符串
        /// </summary>
        public void TypeText(string text)
        {
            foreach (char c in text)
            {
                TypeCharacter(c);
                Thread.Sleep(_delayBetweenKeys);
            }
        }

        /// <summary>
        /// 输入单个字符
        /// </summary>
        private void TypeCharacter(char character)
        {
            var inputs = new Win32API.INPUT[2];

            // 按下
            inputs[0].Type = Win32API.INPUT_KEYBOARD;
            inputs[0].Union.KeyboardInput.Vk = 0;
            inputs[0].Union.KeyboardInput.Scan = character;
            inputs[0].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_UNICODE;

            // 抬起
            inputs[1].Type = Win32API.INPUT_KEYBOARD;
            inputs[1].Union.KeyboardInput.Vk = 0;
            inputs[1].Union.KeyboardInput.Scan = character;
            inputs[1].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_UNICODE | Win32API.KEYEVENTF_KEYUP;

            Win32API.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32API.INPUT)));
        }

        /// <summary>
        /// 按下特殊键（如 Enter）
        /// </summary>
        public void PressKey(ushort virtualKeyCode)
        {
            var inputs = new Win32API.INPUT[2];

            // 按下
            inputs[0].Type = Win32API.INPUT_KEYBOARD;
            inputs[0].Union.KeyboardInput.Vk = virtualKeyCode;
            inputs[0].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_KEYDOWN;

            // 抬起
            inputs[1].Type = Win32API.INPUT_KEYBOARD;
            inputs[1].Union.KeyboardInput.Vk = virtualKeyCode;
            inputs[1].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_KEYUP;

            Win32API.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32API.INPUT)));
        }

        // 常用虚拟键码
        public const ushort VK_RETURN = 0x0D;
        public const ushort VK_ESCAPE = 0x1B;
        public const ushort VK_BACK = 0x08;
    }
}
```

### 3.3 游戏检测服务 (GameDetectionService.cs)

#### 3.3.1 实现示例

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Services
{
    public class GameDetectionService : IDisposable
    {
        private readonly Timer _detectionTimer;
        private GameInfo _currentGame;

        public event EventHandler<GameInfo> GameDetected;
        public event EventHandler GameLost;

        public GameDetectionService(int detectionIntervalMs = 2000)
        {
            _detectionTimer = new Timer(detectionIntervalMs);
            _detectionTimer.Elapsed += OnDetectionTimerElapsed;
        }

        public void Start()
        {
            _detectionTimer.Start();
        }

        public void Stop()
        {
            _detectionTimer.Stop();
        }

        private void OnDetectionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var detectedGame = DetectGame();

            if (detectedGame != null && _currentGame == null)
            {
                // 检测到新游戏
                _currentGame = detectedGame;
                GameDetected?.Invoke(this, _currentGame);
            }
            else if (detectedGame == null && _currentGame != null)
            {
                // 游戏已关闭
                _currentGame = null;
                GameLost?.Invoke(this, EventArgs.Empty);
            }
        }

        private GameInfo DetectGame()
        {
            // 检测魔兽争霸3
            var war3Process = Process.GetProcesses()
                .FirstOrDefault(p => p.ProcessName.ToLower().Contains("war3") ||
                                     p.ProcessName.ToLower().Contains("warcraft"));

            if (war3Process != null)
            {
                return new GameInfo
                {
                    GameType = GameType.Warcraft3,
                    ProcessId = war3Process.Id,
                    ProcessName = war3Process.ProcessName,
                    WindowHandle = war3Process.MainWindowHandle
                };
            }

            // 检测星际争霸1
            var scProcess = Process.GetProcesses()
                .FirstOrDefault(p => p.ProcessName.ToLower().Contains("starcraft"));

            if (scProcess != null)
            {
                return new GameInfo
                {
                    GameType = GameType.StarCraft,
                    ProcessId = scProcess.Id,
                    ProcessName = scProcess.ProcessName,
                    WindowHandle = scProcess.MainWindowHandle
                };
            }

            return null;
        }

        public void Dispose()
        {
            _detectionTimer?.Dispose();
        }
    }
}
```

### 3.4 热键管理器 (HotKeyManager.cs)

#### 3.4.1 实现示例

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Interop;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Services
{
    public class HotKeyManager : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, HotKey> _registeredHotKeys;
        private int _nextHotKeyId = 1;

        public event EventHandler<HotKey> HotKeyPressed;

        public HotKeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _registeredHotKeys = new Dictionary<int, HotKey>();

            // 添加消息钩子
            var source = HwndSource.FromHwnd(_windowHandle);
            source?.AddHook(WndProc);
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        public bool RegisterHotKey(HotKey hotKey)
        {
            var id = _nextHotKeyId++;

            var success = Win32API.RegisterHotKey(
                _windowHandle,
                id,
                hotKey.Modifiers,
                (uint)hotKey.Key
            );

            if (success)
            {
                hotKey.Id = id;
                _registeredHotKeys[id] = hotKey;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        public bool UnregisterHotKey(HotKey hotKey)
        {
            if (hotKey.Id == 0 || !_registeredHotKeys.ContainsKey(hotKey.Id))
                return false;

            var success = Win32API.UnregisterHotKey(_windowHandle, hotKey.Id);

            if (success)
            {
                _registeredHotKeys.Remove(hotKey.Id);
            }

            return success;
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32API.WM_HOTKEY)
            {
                var id = wParam.ToInt32();

                if (_registeredHotKeys.TryGetValue(id, out var hotKey))
                {
                    HotKeyPressed?.Invoke(this, hotKey);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            // 注销所有热键
            foreach (var id in _registeredHotKeys.Keys)
            {
                Win32API.UnregisterHotKey(_windowHandle, id);
            }

            _registeredHotKeys.Clear();
        }
    }
}
```

### 3.5 秘籍执行器 (CheatExecutor.cs)

#### 3.5.1 实现示例

```csharp
using System;
using System.Threading.Tasks;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    public class CheatExecutor
    {
        private readonly KeyboardSimulator _keyboard;

        public CheatExecutor(int inputDelay = 10)
        {
            _keyboard = new KeyboardSimulator(inputDelay);
        }

        /// <summary>
        /// 执行秘籍输入
        /// </summary>
        public async Task<bool> ExecuteCheat(CheatCode cheat, IntPtr gameWindowHandle)
        {
            try
            {
                // 1. 激活游戏窗口
                if (!Win32API.SetForegroundWindow(gameWindowHandle))
                {
                    return false;
                }

                // 等待窗口激活
                await Task.Delay(100);

                // 2. 打开聊天框（按下 Enter）
                _keyboard.PressKey(KeyboardSimulator.VK_RETURN);
                await Task.Delay(50);

                // 3. 输入秘籍代码
                _keyboard.TypeText(cheat.Code);
                await Task.Delay(50);

                // 4. 确认输入（再次按 Enter）
                _keyboard.PressKey(KeyboardSimulator.VK_RETURN);

                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                Logger.Error($"执行秘籍失败: {cheat.Code}", ex);
                return false;
            }
        }
    }
}
```

---

## 4. MVVM 架构实现

### 4.1 ViewModelBase

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameCheatHelper.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
```

### 4.2 RelayCommand

```csharp
using System;
using System.Windows.Input;

namespace GameCheatHelper.Utilities
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
```

---

## 5. 编码规范

### 5.1 命名规范

- **类名**: PascalCase (例如: `GameDetectionService`)
- **接口**: I + PascalCase (例如: `IGameDetector`)
- **方法**: PascalCase (例如: `RegisterHotKey`)
- **属性**: PascalCase (例如: `GameType`)
- **私有字段**: _camelCase (例如: `_currentGame`)
- **参数**: camelCase (例如: `gameInfo`)
- **常量**: UPPER_CASE (例如: `VK_RETURN`)

### 5.2 注释规范

```csharp
/// <summary>
/// 注册全局热键
/// </summary>
/// <param name="hotKey">要注册的热键对象</param>
/// <returns>注册是否成功</returns>
public bool RegisterHotKey(HotKey hotKey)
{
    // 实现代码
}
```

### 5.3 异步编程规范

- 异步方法以 `Async` 后缀命名
- 使用 `async`/`await` 而非 `.Result` 或 `.Wait()`
- 处理 `TaskCanceledException`

```csharp
public async Task<bool> ExecuteCheatAsync(CheatCode cheat)
{
    try
    {
        await Task.Delay(100);
        // ...
    }
    catch (TaskCanceledException)
    {
        return false;
    }
}
```

---

## 6. 调试技巧

### 6.1 调试热键注册

如果热键无法注册，检查:
1. 热键是否已被其他程序占用
2. 应用程序是否有足够权限
3. 窗口句柄是否有效

```csharp
if (!RegisterHotKey(hotKey))
{
    var error = Marshal.GetLastWin32Error();
    Debug.WriteLine($"热键注册失败，错误代码: {error}");
}
```

### 6.2 调试键盘输入

使用记事本测试键盘模拟:
```csharp
// 在记事本中测试
var notepadHandle = Win32API.FindWindow("Notepad", null);
Win32API.SetForegroundWindow(notepadHandle);
_keyboard.TypeText("test");
```

### 6.3 日志记录

```csharp
Logger.Debug("游戏检测: 未发现游戏进程");
Logger.Info($"检测到游戏: {gameInfo.ProcessName}");
Logger.Error("热键注册失败", exception);
```

---

## 7. 常见问题

### Q1: 热键无法注册
**A**: 检查是否以管理员权限运行程序，或热键是否被占用。

### Q2: 秘籍输入失败
**A**: 增加输入延迟，确保游戏窗口已激活。

### Q3: 游戏检测不到
**A**: 检查进程名称是否匹配，尝试添加更多进程名变体。

### Q4: 中文输入问题
**A**: 使用 Unicode 输入方法 (`KEYEVENTF_UNICODE`)。

---

## 8. 构建与发布

### 8.1 Debug 构建

```bash
dotnet build --configuration Debug
```

### 8.2 Release 构建

```bash
dotnet build --configuration Release
dotnet publish -c Release -r win-x64 --self-contained false
```

### 8.3 单文件发布

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

---

**文档版本**: v1.0
**创建日期**: 2026-01-28
**最后更新**: 2026-01-28
