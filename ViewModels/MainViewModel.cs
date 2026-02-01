using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly HotKeyBindingService _hotKeyBindingService;

        private string _gameStatus;
        private string _statusMessage;
        private GameInfo? _currentGame;
        private ObservableCollection<CheatCodeViewModel> _cheatCodes;
        private Dictionary<string, string> _cheatHotKeyMap;
        private KeyValuePair<GameType, string> _selectedGameType;
        private GameType _manuallySelectedGameType;

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
        /// 游戏类型字典（用于下拉框）
        /// </summary>
        public Dictionary<GameType, string> GameTypes { get; } = new Dictionary<GameType, string>
        {
            { GameType.Warcraft3, "魔兽争霸3" },
            { GameType.StarCraft, "星际争霸1" }
        };

        /// <summary>
        /// 当前选择的游戏类型
        /// </summary>
        public KeyValuePair<GameType, string> SelectedGameType
        {
            get => _selectedGameType;
            set
            {
                if (SetProperty(ref _selectedGameType, value))
                {
                    OnGameTypeChanged(value.Key);
                }
            }
        }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 添加秘籍命令
        /// </summary>
        public ICommand AddCheatCommand { get; }

        /// <summary>
        /// 编辑秘籍命令
        /// </summary>
        public ICommand EditCheatCommand { get; }

        /// <summary>
        /// 删除秘籍命令
        /// </summary>
        public ICommand DeleteCheatCommand { get; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand { get; }

        private CheatCodeViewModel? _selectedCheat;

        /// <summary>
        /// 选中的秘籍
        /// </summary>
        public CheatCodeViewModel? SelectedCheat
        {
            get => _selectedCheat;
            set => SetProperty(ref _selectedCheat, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel(IntPtr windowHandle)
        {
            _gameStatus = "未检测到游戏";
            _statusMessage = "就绪";
            _cheatCodes = new ObservableCollection<CheatCodeViewModel>();
            _cheatHotKeyMap = new Dictionary<string, string>();

            // 初始化服务
            _configService = new ConfigService();
            _configService.LoadConfig();

            _cheatCodeService = new CheatCodeService();
            _cheatCodeService.LoadDefaultCheats();

            _hotKeyBindingService = new HotKeyBindingService();
            _hotKeyBindingService.LoadDefaultHotKeyBindings();

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
            AddCheatCommand = new RelayCommand(AddCheat);
            EditCheatCommand = new RelayCommand(EditCheat, () => SelectedCheat != null);
            DeleteCheatCommand = new RelayCommand(DeleteCheat, () => SelectedCheat != null);
            SearchCommand = new RelayCommand<string>(Search);

            // 默认选择魔兽争霸3
            _manuallySelectedGameType = GameType.Warcraft3;
            _selectedGameType = GameTypes.First(x => x.Key == GameType.Warcraft3);

            // 启动时默认加载魔兽争霸3秘籍
            LoadCheatsForGame(GameType.Warcraft3);

            // 启动游戏检测（辅助功能）
            _gameDetectionService.Start();

            Logger.Info("MainViewModel 初始化完成");
            StatusMessage = "已加载魔兽争霸3秘籍，等待检测游戏...";
        }

        /// <summary>
        /// 游戏检测到事件
        /// </summary>
        private void OnGameDetected(object? sender, GameInfo gameInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _currentGame = gameInfo;
                GameStatus = $"{gameInfo.DisplayName} (已检测)";

                // 自动切换到检测到的游戏类型
                _manuallySelectedGameType = gameInfo.GameType;
                _selectedGameType = GameTypes.First(x => x.Key == gameInfo.GameType);
                OnPropertyChanged(nameof(SelectedGameType));

                StatusMessage = $"检测到 {gameInfo.DisplayName}，正在注册热键...";

                // 注册该游戏的热键
                RegisterHotKeysForGame(gameInfo.GameType);

                // 加载该游戏的秘籍
                LoadCheatsForGame(gameInfo.GameType);

                StatusMessage = $"检测到 {gameInfo.DisplayName}，秘籍功能已激活";
            });
        }

        /// <summary>
        /// 游戏丢失事件
        /// </summary>
        private void OnGameLost(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 注销所有热键
                _hotKeyManager?.UnregisterAllHotKeys();

                _currentGame = null;
                GameStatus = "未检测到游戏";
                StatusMessage = "游戏已关闭，可继续管理秘籍";
                _cheatHotKeyMap.Clear();

                // 保持显示当前选择的游戏秘籍
                // 不再清空秘籍列表
            });
        }

        /// <summary>
        /// 游戏类型改变事件
        /// </summary>
        private void OnGameTypeChanged(GameType gameType)
        {
            _manuallySelectedGameType = gameType;

            // 如果游戏正在运行且类型不同，注销当前热键
            if (_currentGame != null && _currentGame.GameType != gameType)
            {
                _hotKeyManager?.UnregisterAllHotKeys();
                _cheatHotKeyMap.Clear();
            }

            // 加载选择的游戏秘籍
            LoadCheatsForGame(gameType);

            // 如果游戏正在运行且类型匹配，重新注册热键
            if (_currentGame != null && _currentGame.GameType == gameType)
            {
                RegisterHotKeysForGame(gameType);
            }

            StatusMessage = $"已切换到 {GetGameName(gameType)}";
            Logger.Info($"用户手动选择游戏: {gameType}");
        }

        /// <summary>
        /// 为游戏注册热键
        /// </summary>
        private void RegisterHotKeysForGame(GameType gameType)
        {
            if (_hotKeyManager == null) return;

            // 获取该游戏的热键绑定
            var bindings = _hotKeyBindingService.GetBindingsByGameType(gameType, _cheatCodeService);

            foreach (var binding in bindings)
            {
                var success = _hotKeyManager.RegisterHotKey(binding.HotKey);
                if (success)
                {
                    _cheatHotKeyMap[binding.CheatCodeId] = binding.HotKey.DisplayText;
                    Logger.Info($"注册热键: {binding.HotKey.DisplayText} -> {binding.CheatCodeId}");
                }
                else
                {
                    Logger.Warn($"热键注册失败: {binding.HotKey.DisplayText}");
                }
            }
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
                var hotKey = _cheatHotKeyMap.ContainsKey(cheat.Id) ? _cheatHotKeyMap[cheat.Id] : "未绑定";
                CheatCodes.Add(new CheatCodeViewModel(cheat, hotKey));
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

            // 刷新当前选择的游戏秘籍列表
            LoadCheatsForGame(_manuallySelectedGameType);

            StatusMessage = "刷新完成";
        }

        /// <summary>
        /// 添加秘籍
        /// </summary>
        private void AddCheat()
        {
            var viewModel = new CheatEditViewModel();
            var dialog = new Views.CheatEditDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var newCheat = viewModel.GetCheatCode();

                // 检查热键冲突
                if (viewModel.CurrentHotKey != null)
                {
                    var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
                        viewModel.CurrentHotKey,
                        null  // 新建秘籍，不需要排除
                    );

                    if (conflictingCheatId != null)
                    {
                        // 找到冲突的秘籍
                        var conflictingCheat = _cheatCodeService.GetCheatById(conflictingCheatId);
                        var conflictMsg = conflictingCheat != null
                            ? $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被秘籍 '{conflictingCheat.Description}' 使用。\n\n是否移除原有绑定并使用新的绑定？"
                            : $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被其他秘籍使用。\n\n是否移除原有绑定并使用新的绑定？";

                        var result = MessageBox.Show(
                            conflictMsg,
                            "热键冲突",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );

                        if (result != MessageBoxResult.Yes)
                        {
                            // 用户选择不覆盖，清除热键
                            viewModel.CurrentHotKey = null;
                            StatusMessage = "已取消热键绑定";
                        }
                    }
                }

                if (_cheatCodeService.AddCheat(newCheat))
                {
                    // 保存热键绑定
                    if (viewModel.CurrentHotKey != null)
                    {
                        var description = $"{GetGameName(newCheat.Game)}: {newCheat.Description}";
                        _hotKeyBindingService.AddOrUpdateHotKeyBinding(newCheat.Id, viewModel.CurrentHotKey, description);

                        // 如果当前游戏正在运行且匹配，注册热键
                        if (_currentGame != null && newCheat.Game == _currentGame.GameType && _hotKeyManager != null)
                        {
                            // 重新注册所有热键
                            _hotKeyManager.UnregisterAllHotKeys();
                            RegisterHotKeysForGame(_currentGame.GameType);
                        }

                        StatusMessage = $"秘籍 '{newCheat.Code}' 已添加，热键 '{viewModel.CurrentHotKey.DisplayText}' 已绑定";
                    }
                    else
                    {
                        StatusMessage = $"秘籍 '{newCheat.Code}' 已添加";
                    }

                    // 如果是当前选择的游戏，刷新列表
                    if (newCheat.Game == _manuallySelectedGameType)
                    {
                        LoadCheatsForGame(_manuallySelectedGameType);
                    }
                }
                else
                {
                    StatusMessage = "添加秘籍失败";
                }
            }
        }

        /// <summary>
        /// 编辑秘籍
        /// </summary>
        private void EditCheat()
        {
            if (SelectedCheat == null)
            {
                StatusMessage = "请先选择要编辑的秘籍";
                return;
            }

            var cheat = _cheatCodeService.GetCheatById(SelectedCheat.Code);
            if (cheat == null)
            {
                // 尝试通过Code查找
                cheat = _cheatCodeService.CheatCodes.FirstOrDefault(c => c.Code == SelectedCheat.Code);
            }

            if (cheat == null)
            {
                StatusMessage = "找不到该秘籍";
                return;
            }

            var viewModel = new CheatEditViewModel(cheat);

            // 加载现有热键绑定
            var existingBinding = _hotKeyBindingService.GetBindingByCheatCodeId(cheat.Id);
            if (existingBinding != null)
            {
                viewModel.CurrentHotKey = existingBinding.HotKey;
            }

            var dialog = new Views.CheatEditDialog(viewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var updatedCheat = viewModel.GetCheatCode();

                // 检查热键冲突（排除当前秘籍自己）
                if (viewModel.CurrentHotKey != null)
                {
                    var conflictingCheatId = _hotKeyBindingService.CheckHotKeyOccupied(
                        viewModel.CurrentHotKey,
                        updatedCheat.Id  // 排除当前秘籍
                    );

                    if (conflictingCheatId != null)
                    {
                        // 找到冲突的秘籍
                        var conflictingCheat = _cheatCodeService.GetCheatById(conflictingCheatId);
                        var conflictMsg = conflictingCheat != null
                            ? $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被秘籍 '{conflictingCheat.Description}' 使用。\n\n是否移除原有绑定并使用新的绑定？"
                            : $"热键 '{viewModel.CurrentHotKey.DisplayText}' 已被其他秘籍使用。\n\n是否移除原有绑定并使用新的绑定？";

                        var result = MessageBox.Show(
                            conflictMsg,
                            "热键冲突",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );

                        if (result != MessageBoxResult.Yes)
                        {
                            // 用户选择不覆盖，恢复原有热键
                            viewModel.CurrentHotKey = existingBinding?.HotKey;
                            StatusMessage = "已取消热键更改";
                        }
                    }
                }

                if (_cheatCodeService.UpdateCheat(updatedCheat))
                {
                    // 更新热键绑定
                    var description = $"{GetGameName(updatedCheat.Game)}: {updatedCheat.Description}";
                    _hotKeyBindingService.AddOrUpdateHotKeyBinding(updatedCheat.Id, viewModel.CurrentHotKey, description);

                    // 如果当前游戏正在运行且匹配，需要重新注册热键
                    if (_currentGame != null && updatedCheat.Game == _currentGame.GameType)
                    {
                        // 注销所有热键并重新注册
                        _hotKeyManager?.UnregisterAllHotKeys();
                        RegisterHotKeysForGame(_currentGame.GameType);
                    }

                    if (viewModel.CurrentHotKey != null)
                    {
                        StatusMessage = $"秘籍 '{updatedCheat.Code}' 已更新，热键 '{viewModel.CurrentHotKey.DisplayText}' 已绑定";
                    }
                    else
                    {
                        StatusMessage = $"秘籍 '{updatedCheat.Code}' 已更新";
                    }

                    // 刷新当前选择的游戏秘籍列表
                    LoadCheatsForGame(_manuallySelectedGameType);
                }
                else
                {
                    StatusMessage = "更新秘籍失败";
                }
            }
        }

        /// <summary>
        /// 获取游戏名称
        /// </summary>
        private string GetGameName(GameType gameType)
        {
            return gameType switch
            {
                GameType.Warcraft3 => "魔兽争霸3",
                GameType.StarCraft => "星际争霸1",
                _ => "未知游戏"
            };
        }

        /// <summary>
        /// 删除秘籍
        /// </summary>
        private void DeleteCheat()
        {
            if (SelectedCheat == null)
            {
                StatusMessage = "请先选择要删除的秘籍";
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"确定要删除秘籍 '{SelectedCheat.Code}' 吗？",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var cheat = _cheatCodeService.CheatCodes.FirstOrDefault(c => c.Code == SelectedCheat.Code);
                if (cheat != null && _cheatCodeService.RemoveCheat(cheat.Id))
                {
                    StatusMessage = $"秘籍 '{SelectedCheat.Code}' 已删除";

                    // 刷新当前选择的游戏秘籍列表
                    LoadCheatsForGame(_manuallySelectedGameType);
                }
                else
                {
                    StatusMessage = "删除秘籍失败";
                }
            }
        }

        /// <summary>
        /// 搜索秘籍
        /// </summary>
        private void Search(string? keyword)
        {
            // 使用手动选择的游戏类型进行搜索
            var results = _cheatCodeService.SearchCheats(keyword ?? string.Empty)
                .Where(c => c.Game == _manuallySelectedGameType)
                .ToList();

            CheatCodes.Clear();
            foreach (var cheat in results)
            {
                var hotKey = _cheatHotKeyMap.ContainsKey(cheat.Id) ? _cheatHotKeyMap[cheat.Id] : "未绑定";
                CheatCodes.Add(new CheatCodeViewModel(cheat, hotKey));
            }

            StatusMessage = string.IsNullOrWhiteSpace(keyword)
                ? $"显示所有秘籍 ({results.Count} 个)"
                : $"搜索到 {results.Count} 个秘籍";
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

        /// <summary>
        /// 获取配置服务（供设置窗口使用）
        /// </summary>
        public ConfigService GetConfigService()
        {
            return _configService;
        }
    }

    /// <summary>
    /// 秘籍代码 ViewModel（用于显示）
    /// </summary>
    public class CheatCodeViewModel : ViewModelBase
    {
        private readonly CheatCode _cheat;
        private readonly string _hotKeyText;

        public string Code => _cheat.Code;
        public string Description => _cheat.Description;
        public string Category => _cheat.Category;
        public string HotKeyText => _hotKeyText;

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

        public CheatCodeViewModel(CheatCode cheat, string hotKeyText = "未绑定")
        {
            _cheat = cheat;
            _hotKeyText = hotKeyText;
        }
    }
}
