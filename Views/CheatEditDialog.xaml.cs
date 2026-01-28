using System.Windows;
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

        public CheatEditDialog()
        {
            InitializeComponent();
            Logger.Info("秘籍编辑对话框初始化");
        }

        public CheatEditDialog(CheatEditViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
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
