using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using GameCheatHelper.Core.Models;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 游戏检测服务
    /// 定时检测游戏进程，通知游戏状态变化
    /// </summary>
    public class GameDetectionService : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Timer _detectionTimer;
        private GameInfo? _currentGame;

        /// <summary>
        /// 游戏检测到事件
        /// </summary>
        public event EventHandler<GameInfo>? GameDetected;

        /// <summary>
        /// 游戏丢失事件
        /// </summary>
        public event EventHandler? GameLost;

        /// <summary>
        /// 当前检测到的游戏
        /// </summary>
        public GameInfo? CurrentGame => _currentGame;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="detectionIntervalMs">检测间隔（毫秒），默认2000ms</param>
        public GameDetectionService(int detectionIntervalMs = 2000)
        {
            _detectionTimer = new Timer(detectionIntervalMs);
            _detectionTimer.Elapsed += OnDetectionTimerElapsed;
            Logger.Info($"游戏检测服务初始化，检测间隔: {detectionIntervalMs}ms");
        }

        /// <summary>
        /// 启动检测
        /// </summary>
        public void Start()
        {
            _detectionTimer.Start();
            Logger.Info("游戏检测服务已启动");
        }

        /// <summary>
        /// 停止检测
        /// </summary>
        public void Stop()
        {
            _detectionTimer.Stop();
            Logger.Info("游戏检测服务已停止");
        }

        /// <summary>
        /// 定时器触发事件
        /// </summary>
        private void OnDetectionTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            var detectedGame = DetectGame();

            if (detectedGame != null && _currentGame == null)
            {
                // 检测到新游戏
                _currentGame = detectedGame;
                Logger.Info($"检测到游戏: {_currentGame.DisplayName} (PID: {_currentGame.ProcessId})");
                GameDetected?.Invoke(this, _currentGame);
            }
            else if (detectedGame == null && _currentGame != null)
            {
                // 游戏已关闭
                Logger.Info($"游戏已关闭: {_currentGame.DisplayName}");
                _currentGame = null;
                GameLost?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 检测游戏进程
        /// </summary>
        private GameInfo? DetectGame()
        {
            try
            {
                // 检测魔兽争霸3
                var war3Process = Process.GetProcesses()
                    .FirstOrDefault(p =>
                    {
                        try
                        {
                            var name = p.ProcessName.ToLower();
                            return name.Contains("war3") ||
                                   name.Contains("warcraft") ||
                                   name == "frozen throne";
                        }
                        catch
                        {
                            return false;
                        }
                    });

                if (war3Process != null)
                {
                    return new GameInfo
                    {
                        GameType = GameType.Warcraft3,
                        ProcessId = war3Process.Id,
                        ProcessName = war3Process.ProcessName,
                        WindowHandle = war3Process.MainWindowHandle
                    };
                }

                // 检测星际争霸1
                var scProcess = Process.GetProcesses()
                    .FirstOrDefault(p =>
                    {
                        try
                        {
                            var name = p.ProcessName.ToLower();
                            return name.Contains("starcraft") && !name.Contains("starcraft2");
                        }
                        catch
                        {
                            return false;
                        }
                    });

                if (scProcess != null)
                {
                    return new GameInfo
                    {
                        GameType = GameType.StarCraft,
                        ProcessId = scProcess.Id,
                        ProcessName = scProcess.ProcessName,
                        WindowHandle = scProcess.MainWindowHandle
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "游戏检测过程中发生错误");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _detectionTimer?.Stop();
            _detectionTimer?.Dispose();
            Logger.Info("游戏检测服务已释放");
        }
    }
}
