using System.Windows;
using GameCheatHelper.Controls;
using GameCheatHelper.ViewModels;
using NLog;

namespace GameCheatHelper.Views
{
    /// <summary>
    /// CheatEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CheatEditDialog : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CheatEditViewModel? _viewModel;
        private HotKeyRecorder? _hotKeyRecorder;

        public CheatEditDialog()
        {
            InitializeComponent();
            InitializeHotKeyRecorder();
            Logger.Info("秘籍编辑对话框初始化");
        }

        public CheatEditDialog(CheatEditViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 设置热键录制控件的初始值
            if (_hotKeyRecorder != null && _viewModel.CurrentHotKey != null)
            {
                _hotKeyRecorder.CurrentHotKey = _viewModel.CurrentHotKey;
            }
        }

        /// <summary>
        /// 初始化热键录制控件
        /// </summary>
        private void InitializeHotKeyRecorder()
        {
            _hotKeyRecorder = new HotKeyRecorder();
            _hotKeyRecorder.HotKeyChanged += HotKeyRecorder_HotKeyChanged;
            HotKeyRecorderPlaceholder.Child = _hotKeyRecorder;
        }

        /// <summary>
        /// 热键改变事件处理
        /// </summary>
        private void HotKeyRecorder_HotKeyChanged(object? sender, Core.Models.HotKey? e)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentHotKey = e;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.Validate())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
