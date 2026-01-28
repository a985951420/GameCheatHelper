using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GameCheatHelper.Core;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Services;
using GameCheatHelper.Utilities;
using NLog;

namespace GameCheatHelper.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly GameDetectionService _gameDetectionService;
        private readonly HotKeyManager? _hotKeyManager;
        private readonly CheatCodeService _cheatCodeService;
        private readonly ConfigService _configService;
        private readonly CheatExecutor _cheatExecutor;

        private string _gameStatus;
        private string _statusMessage;
        private GameInfo? _currentGame;
        private ObservableCollection<CheatCodeViewModel> _cheatCodes;

        /// <summary>
        /// 游戏状态文本
        /// </summary>
        public string GameStatus
        {
            get => _gameStatus;
            set => SetProperty(ref _gameStatus, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 秘籍代码列表
        /// </summary>
        public ObservableCollection<CheatCodeViewModel> CheatCodes
        {
            get => _cheatCodes;
            set => SetProperty(ref _cheatCodes, value);
        }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel(IntPtr windowHandle)
        {
            _gameStatus = "未检测到游戏";
            _statusMessage = "就绪";
            _cheatCodes = new ObservableCollection<CheatCodeViewModel>();

            // 初始化服务
            _configService = new ConfigService();
            _configService.LoadConfig();

            _cheatCodeService = new CheatCodeService();
            _cheatCodeService.LoadDefaultCheats();

            _gameDetectionService = new GameDetectionService(_configService.Config.Settings.DetectionInterval);
            _gameDetectionService.GameDetected += OnGameDetected;
            _gameDetectionService.GameLost += OnGameLost;

            if (windowHandle != IntPtr.Zero)
            {
                _hotKeyManager = new HotKeyManager(windowHandle);
                _hotKeyManager.HotKeyPressed += OnHotKeyPressed;
            }

            _cheatExecutor = new CheatExecutor(_configService.Config.Settings.InputDelay);

            // 命令
            RefreshCommand = new RelayCommand(Refresh);

            // 启动游戏检测
            _gameDetectionService.Start();

            Logger.Info("MainViewModel 初始化完成");
            StatusMessage = "应用程序已启动，等待检测游戏...";
        }

        /// <summary>
        /// 游戏检测到事件
        /// </summary>
        private void OnGameDetected(object? sender, GameInfo gameInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _currentGame = gameInfo;
                GameStatus = gameInfo.DisplayName;
                StatusMessage = $"检测到 {gameInfo.DisplayName}，秘籍功能已激活";

                // 加载该游戏的秘籍
                LoadCheatsForGame(gameInfo.GameType);
            });
        }

        /// <summary>
        /// 游戏丢失事件
        /// </summary>
        private void OnGameLost(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _currentGame = null;
                GameStatus = "未检测到游戏";
                StatusMessage = "游戏已关闭";
                CheatCodes.Clear();
            });
        }

        /// <summary>
        /// 热键按下事件
        /// </summary>
        private async void OnHotKeyPressed(object? sender, HotKey hotKey)
        {
            try
            {
                if (_currentGame == null)
                {
                    Logger.Warn("游戏未运行，忽略热键");
                    return;
                }

                if (string.IsNullOrEmpty(hotKey.CheatCodeId))
                {
                    Logger.Warn("热键未绑定秘籍");
                    return;
                }

                var cheat = _cheatCodeService.GetCheatById(hotKey.CheatCodeId);
                if (cheat == null)
                {
                    Logger.Warn($"秘籍不存在: {hotKey.CheatCodeId}");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"正在执行秘籍: {cheat.Code}...";
                });

                var success = await _cheatExecutor.ExecuteCheatAsync(cheat, _currentGame.WindowHandle);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = success
                        ? $"秘籍执行成功: {cheat.Description}"
                        : "秘籍执行失败";
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "热键处理失败");
            }
        }

        /// <summary>
        /// 加载游戏秘籍
        /// </summary>
        private void LoadCheatsForGame(GameType gameType)
        {
            CheatCodes.Clear();

            var cheats = _cheatCodeService.GetCheatsByGame(gameType);
            foreach (var cheat in cheats)
            {
                CheatCodes.Add(new CheatCodeViewModel(cheat));
            }

            Logger.Info($"加载了 {cheats.Count} 个秘籍");
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void Refresh()
        {
            StatusMessage = "刷新中...";
            _cheatCodeService.LoadDefaultCheats();

            if (_currentGame != null)
            {
                LoadCheatsForGame(_currentGame.GameType);
            }

            StatusMessage = "刷新完成";
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _gameDetectionService?.Dispose();
            _hotKeyManager?.Dispose();
            Logger.Info("MainViewModel 已释放");
        }
    }

    /// <summary>
    /// 秘籍代码 ViewModel（用于显示）
    /// </summary>
    public class CheatCodeViewModel : ViewModelBase
    {
        private readonly CheatCode _cheat;

        public string Code => _cheat.Code;
        public string Description => _cheat.Description;
        public string Category => _cheat.Category;

        public bool Enabled
        {
            get => _cheat.Enabled;
            set
            {
                if (_cheat.Enabled != value)
                {
                    _cheat.Enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HotKeyText => "未绑定"; // TODO: 从热键配置获取

        public CheatCodeViewModel(CheatCode cheat)
        {
            _cheat = cheat;
        }
    }
}
