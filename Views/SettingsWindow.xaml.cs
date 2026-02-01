using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
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
                Logger.Info($"打开超链接: {e.Uri.AbsoluteUri}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"打开超链接失败: {e.Uri.AbsoluteUri}");
                MessageBox.Show("无法打开链接", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 确认操作
                var confirmResult = MessageBox.Show(
                    "即将执行发布脚本生成可发布的单文件可执行程序。\n\n" +
                    "此操作将：\n" +
                    "• 清理旧的发布文件\n" +
                    "• 编译 Release 版本\n" +
                    "• 生成单文件自包含 EXE\n" +
                    "• 复制必要的配置和数据文件\n\n" +
                    "是否继续？",
                    "生成发布包",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }

                // 获取批处理文件路径
                string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                string publishScriptPath = System.IO.Path.Combine(projectDir, "publish.bat");

                // 检查批处理文件是否存在
                if (!System.IO.File.Exists(publishScriptPath))
                {
                    // 尝试在上层目录查找（开发环境）
                    string devScriptPath = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(projectDir, "..", "..", "..", "publish.bat"));

                    if (System.IO.File.Exists(devScriptPath))
                    {
                        publishScriptPath = devScriptPath;
                    }
                    else
                    {
                        MessageBox.Show(
                            $"未找到发布脚本文件！\n\n" +
                            $"请确保 publish.bat 位于项目根目录。\n\n" +
                            $"查找路径:\n{publishScriptPath}",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Logger.Error($"发布脚本不存在: {publishScriptPath}");
                        return;
                    }
                }

                Logger.Info($"开始执行发布脚本: {publishScriptPath}");

                // 配置进程启动信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = publishScriptPath,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(publishScriptPath),
                    UseShellExecute = true,  // 使用shell执行，显示命令行窗口
                    Verb = "runas"  // 可选：以管理员权限运行
                };

                // 启动发布脚本
                var process = Process.Start(startInfo);

                if (process != null)
                {
                    MessageBox.Show(
                        "发布脚本已启动！\n\n" +
                        "请在命令行窗口中查看编译进度。\n" +
                        "编译完成后，发布文件将位于 publish\\ 目录。",
                        "发布中",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Logger.Info("发布脚本已成功启动");
                }
                else
                {
                    MessageBox.Show(
                        "无法启动发布脚本！",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Logger.Error("发布脚本启动失败");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 用户取消了 UAC 提升或权限问题
                Logger.Warn(ex, "发布脚本执行被取消或权限不足");
                MessageBox.Show(
                    "执行已取消或权限不足。\n\n" +
                    "如果需要管理员权限，请允许 UAC 提升。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "执行发布脚本时发生错误");
                MessageBox.Show(
                    $"执行发布脚本失败！\n\n错误信息：\n{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
