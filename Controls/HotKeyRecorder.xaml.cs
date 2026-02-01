using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Controls
{
    /// <summary>
    /// 热键录制控件
    /// 用户可以按下组合键来录制热键
    /// </summary>
    public partial class HotKeyRecorder : UserControl, INotifyPropertyChanged
    {
        private bool _isRecording;
        private string _hotKeyText = "点击录制热键...";
        private bool _hasHotKey;
        private HotKey? _currentHotKey;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 热键改变事件
        /// </summary>
        public event EventHandler<HotKey?>? HotKeyChanged;

        /// <summary>
        /// 当前录制的热键
        /// </summary>
        public HotKey? CurrentHotKey
        {
            get => _currentHotKey;
            set
            {
                _currentHotKey = value;
                UpdateDisplay();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否正在录制
        /// </summary>
        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                _isRecording = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 热键显示文本
        /// </summary>
        public string HotKeyText
        {
            get => _hotKeyText;
            private set
            {
                _hotKeyText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否有热键
        /// </summary>
        public bool HasHotKey
        {
            get => _hasHotKey;
            private set
            {
                _hasHotKey = value;
                OnPropertyChanged();
            }
        }

        public HotKeyRecorder()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        private void HotKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (!IsRecording)
                return;

            var key = e.Key;

            // 处理 Alt 组合键的特殊情况
            // 在 WPF 中，Alt+其他键会使 e.Key 返回 Key.System
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // 忽略单独的修饰键
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // 收集修饰键
            uint modifiers = 0;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                modifiers |= Win32API.MOD_CONTROL;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                modifiers |= Win32API.MOD_ALT;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                modifiers |= Win32API.MOD_SHIFT;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
                modifiers |= Win32API.MOD_WIN;

            // 创建热键对象
            var hotKey = new HotKey
            {
                Key = key,
                Modifiers = modifiers
            };

            CurrentHotKey = hotKey;
            IsRecording = false;

            // 触发热键改变事件
            HotKeyChanged?.Invoke(this, hotKey);

            // 失去焦点
            HotKeyTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        /// <summary>
        /// 获得焦点时开始录制
        /// </summary>
        private void HotKeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            IsRecording = true;
            HotKeyText = "按下组合键...";
        }

        /// <summary>
        /// 失去焦点时停止录制
        /// </summary>
        private void HotKeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsRecording = false;
            UpdateDisplay();
        }

        /// <summary>
        /// 清除热键
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentHotKey = null;
            HotKeyChanged?.Invoke(this, null);
        }

        /// <summary>
        /// 更新显示文本
        /// </summary>
        private void UpdateDisplay()
        {
            if (CurrentHotKey != null)
            {
                HotKeyText = CurrentHotKey.DisplayText;
                HasHotKey = true;
            }
            else
            {
                HotKeyText = "点击录制热键...";
                HasHotKey = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
