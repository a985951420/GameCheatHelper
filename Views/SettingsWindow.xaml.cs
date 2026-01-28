using System.Windows;
using GameCheatHelper.ViewModels;
using NLog;

namespace GameCheatHelper.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private SettingsViewModel? _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            Logger.Info("设置窗口初始化");
        }

        public SettingsWindow(SettingsViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.Save())
            {
                MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("保存设置失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要恢复默认设置吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel?.ResetToDefaults();
                MessageBox.Show("已恢复默认设置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
