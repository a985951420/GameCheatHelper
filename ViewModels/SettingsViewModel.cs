using System;
using GameCheatHelper.Services;
using NLog;

namespace GameCheatHelper.ViewModels
{
    /// <summary>
    /// 设置窗口 ViewModel
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ConfigService _configService;

        private int _detectionInterval;
        private int _inputDelay;
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _minimizeToTray;
        private bool _closeToTray;
        private string _language;

        /// <summary>
        /// 游戏检测间隔
        /// </summary>
        public int DetectionInterval
        {
            get => _detectionInterval;
            set => SetProperty(ref _detectionInterval, value);
        }

        /// <summary>
        /// 输入延迟
        /// </summary>
        public int InputDelay
        {
            get => _inputDelay;
            set => SetProperty(ref _inputDelay, value);
        }

        /// <summary>
        /// 开机自启动
        /// </summary>
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetProperty(ref _startWithWindows, value);
        }

        /// <summary>
        /// 启动时最小化
        /// </summary>
        public bool StartMinimized
        {
            get => _startMinimized;
            set => SetProperty(ref _startMinimized, value);
        }

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetProperty(ref _minimizeToTray, value);
        }

        /// <summary>
        /// 关闭到托盘
        /// </summary>
        public bool CloseToTray
        {
            get => _closeToTray;
            set => SetProperty(ref _closeToTray, value);
        }

        /// <summary>
        /// 语言
        /// </summary>
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SettingsViewModel(ConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _language = "简体中文";

            // 加载当前配置
            LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            var config = _configService.Config;
            DetectionInterval = config.Settings.DetectionInterval;
            InputDelay = config.Settings.InputDelay;
            StartWithWindows = config.Settings.StartWithWindows;
            StartMinimized = config.Settings.StartMinimized;
            MinimizeToTray = config.Settings.MinimizeToTray;
            CloseToTray = config.Settings.CloseToTray;

            Logger.Info("加载设置完成");
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public bool Save()
        {
            try
            {
                var config = _configService.Config;
                config.Settings.DetectionInterval = DetectionInterval;
                config.Settings.InputDelay = InputDelay;
                config.Settings.StartWithWindows = StartWithWindows;
                config.Settings.StartMinimized = StartMinimized;
                config.Settings.MinimizeToTray = MinimizeToTray;
                config.Settings.CloseToTray = CloseToTray;

                var success = _configService.SaveConfig();

                if (success)
                {
                    Logger.Info("设置已保存");

                    // 如果设置了开机自启动，配置注册表
                    if (StartWithWindows)
                    {
                        ConfigureStartup(true);
                    }
                    else
                    {
                        ConfigureStartup(false);
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "保存设置失败");
                return false;
            }
        }

        /// <summary>
        /// 恢复默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            DetectionInterval = 1000;
            InputDelay = 50;
            StartWithWindows = false;
            StartMinimized = false;
            MinimizeToTray = true;
            CloseToTray = false;

            Logger.Info("已恢复默认设置");
        }

        /// <summary>
        /// 配置开机自启动
        /// </summary>
        private void ConfigureStartup(bool enable)
        {
            try
            {
                const string appName = "GameCheatHelper";
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key != null)
                {
                    if (enable)
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                        Logger.Info("已启用开机自启动");
                    }
                    else
                    {
                        if (key.GetValue(appName) != null)
                        {
                            key.DeleteValue(appName);
                            Logger.Info("已禁用开机自启动");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "配置开机自启动失败");
            }
        }
    }
}
