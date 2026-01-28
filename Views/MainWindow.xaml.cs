using System.Windows;
using NLog;

namespace GameCheatHelper.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            Logger.Info("主窗口初始化");

            // 加载数据
            LoadData();

            // 设置状态
            UpdateStatus("应用程序已启动，等待检测游戏...");
        }

        /// <summary>
        /// 加载秘籍数据
        /// </summary>
        private void LoadData()
        {
            // TODO: 从服务加载秘籍数据
            Logger.Debug("加载秘籍数据");
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
            Logger.Info($"状态: {message}");
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
                UpdateStatus("请输入搜索关键词");
                return;
            }

            UpdateStatus($"搜索: {searchText}");
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
                UpdateStatus("秘籍已删除");
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            Logger.Info("主窗口关闭");
        }
    }
}
