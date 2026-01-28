using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using NLog;
using GameCheatHelper.Views;

namespace GameCheatHelper
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;

        public App()
        {
            // 设置全局异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Logger.Info("=== GameCheatHelper 启动 ===");
            Logger.Info($"版本: 1.0.0");
            Logger.Info($"启动时间: {DateTime.Now}");

            // 初始化系统托盘图标
            InitializeTrayIcon();

            // 创建主窗口（不立即显示）
            _mainWindow = new MainWindow();
            _mainWindow.StateChanged += MainWindow_StateChanged;
            _mainWindow.Closing += MainWindow_Closing;

            // 显示主窗口
            _mainWindow.Show();
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // 使用默认图标
                Visible = true,
                Text = "GameCheatHelper - 游戏秘籍助手"
            };

            // 双击托盘图标显示主窗口
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示主窗口", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;

            Logger.Info("系统托盘图标已初始化");
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        private void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }
        }

        /// <summary>
        /// 主窗口状态改变事件
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow != null && _mainWindow.WindowState == WindowState.Minimized)
            {
                // 最小化到托盘
                _mainWindow.Hide();
                _notifyIcon?.ShowBalloonTip(2000, "GameCheatHelper", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 主窗口关闭事件
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 可以根据设置决定是否最小化到托盘而不是真正关闭
            // 这里先默认允许关闭
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            Logger.Info("=== GameCheatHelper 退出 ===");
            LogManager.Shutdown();
            base.OnExit(e);
        }

        /// <summary>
        /// UI线程未处理异常
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "UI线程发生未处理的异常");

            System.Windows.MessageBox.Show(
                $"发生错误:\n{e.Exception.Message}\n\n程序将继续运行。",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        /// <summary>
        /// 非UI线程未处理异常
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Fatal(ex, "应用程序域发生未处理的异常");

                System.Windows.MessageBox.Show(
                    $"发生严重错误:\n{ex.Message}\n\n程序即将退出。",
                    "严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
