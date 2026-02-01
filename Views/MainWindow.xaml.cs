using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using GameCheatHelper.ViewModels;
using NLog;

namespace GameCheatHelper.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Logger.Info("主窗口初始化");

            // 窗口加载完成后初始化 ViewModel
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取窗口句柄
                var windowHelper = new WindowInteropHelper(this);
                var windowHandle = windowHelper.Handle;

                // 创建 ViewModel 并设置为 DataContext
                _viewModel = new MainViewModel(windowHandle);
                DataContext = _viewModel;

                Logger.Info("ViewModel 初始化成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ViewModel 初始化失败");
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var settingsViewModel = new SettingsViewModel(_viewModel.GetConfigService());
            var settingsWindow = new SettingsWindow(settingsViewModel);
            settingsWindow.Owner = this;

            if (settingsWindow.ShowDialog() == true)
            {
                _viewModel.StatusMessage = "设置已更新，部分设置需要重启应用生效";
            }
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox.Text;
            _viewModel?.SearchCommand.Execute(searchText);
        }

        /// <summary>
        /// 添加秘籍按钮点击事件
        /// </summary>
        private void AddCheatButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddCheatCommand.Execute(null);
        }

        /// <summary>
        /// 编辑秘籍按钮点击事件
        /// </summary>
        private void EditCheatButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.EditCheatCommand.Execute(null);
        }

        /// <summary>
        /// 删除秘籍按钮点击事件
        /// </summary>
        private void DeleteCheatButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.DeleteCheatCommand.Execute(null);
        }

        /// <summary>
        /// 游戏类型选择改变事件
        /// </summary>
        private void GameTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 事件由 ViewModel 的 SelectedGameType 属性变化自动处理
            // 此方法保留用于可能的 UI 特定逻辑
        }

        /// <summary>
        /// 超链接点击事件 - 打开GitHub链接
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打开链接失败");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.Dispose();
            Logger.Info("主窗口关闭");
        }
    }
}
