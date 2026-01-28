using System;
using System.Windows;
using System.Windows.Interop;
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
            MessageBox.Show("设置功能即将推出！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (_viewModel != null)
                    _viewModel.StatusMessage = "请输入搜索关键词";
                return;
            }

            if (_viewModel != null)
                _viewModel.StatusMessage = $"搜索: {searchText}";

            // TODO: 实现搜索功能
        }

        /// <summary>
        /// 添加秘籍按钮点击事件
        /// </summary>
        private void AddCheatButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("添加秘籍功能即将推出！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 编辑秘籍按钮点击事件
        /// </summary>
        private void EditCheatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CheatDataGrid.SelectedItem == null)
            {
                MessageBox.Show("请先选择要编辑的秘籍", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("编辑秘籍功能即将推出！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 删除秘籍按钮点击事件
        /// </summary>
        private void DeleteCheatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CheatDataGrid.SelectedItem == null)
            {
                MessageBox.Show("请先选择要删除的秘籍", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "确定要删除选中的秘籍吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: 实现删除功能
                if (_viewModel != null)
                    _viewModel.StatusMessage = "秘籍已删除";
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
