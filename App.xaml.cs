using System;
using System.Windows;
using NLog;

namespace GameCheatHelper
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

            // 这里可以初始化依赖注入容器
            // 这里可以加载配置文件
        }

        protected override void OnExit(ExitEventArgs e)
        {
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

            MessageBox.Show(
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

                MessageBox.Show(
                    $"发生严重错误:\n{ex.Message}\n\n程序即将退出。",
                    "严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
