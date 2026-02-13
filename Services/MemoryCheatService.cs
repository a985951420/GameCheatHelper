using System;
using System.Diagnostics;
using System.Timers;
using GameCheatHelper.Core;
using GameCheatHelper.Core.Models;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 内存秘籍服务
    /// 通过修改游戏进程内存实现特殊功能（如解除人口上限）
    /// </summary>
    public class MemoryCheatService : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly MemoryEditor _memoryEditor;
        private Timer? _supplyTimer;
        private int _targetProcessId;
        private bool _isSupplyCapRemoved;
        private bool _disposed;

        #region 星际争霸1 (Brood War 1.16.1) 内存地址

        // ============================================================
        // 星际争霸1 人口/补给相关内存地址
        // 适用版本: StarCraft: Brood War v1.16.1
        // 说明: 游戏内部补给值是实际值的2倍（200人口 = 内部值400）
        // 每个数组有12个玩家位置，每个占4字节
        // ============================================================

        // 补给提供量（Provided） - 即当前补给建筑提供的总补给
        // Zerg 补给提供: 0x00582174 (Player 0 起始, 每个玩家+4)
        private static readonly IntPtr SC_SUPPLY_PROVIDED_ZERG = new IntPtr(0x00582174);
        // Terran 补给提供: 0x00582234
        private static readonly IntPtr SC_SUPPLY_PROVIDED_TERRAN = new IntPtr(0x00582234);
        // Protoss 补给提供: 0x005822F4
        private static readonly IntPtr SC_SUPPLY_PROVIDED_PROTOSS = new IntPtr(0x005822F4);

        // 补给上限 (Max) - 用于限制最大补给
        // Zerg 补给上限: 0x005821A4
        private static readonly IntPtr SC_SUPPLY_MAX_ZERG = new IntPtr(0x005821A4);
        // Terran 补给上限: 0x00582264
        private static readonly IntPtr SC_SUPPLY_MAX_TERRAN = new IntPtr(0x00582264);
        // Protoss 补给上限: 0x00582324
        private static readonly IntPtr SC_SUPPLY_MAX_PROTOSS = new IntPtr(0x00582324);

        // 内部值常量
        private const int SUPPLY_200_INTERNAL = 400;    // 200人口对应内部值400
        private const int SUPPLY_MAX_INTERNAL = 1600;   // 设置为800人口上限（内部值1600）
        private const int PLAYER_ENTRY_SIZE = 4;        // 每个玩家条目4字节

        #endregion

        /// <summary>
        /// 人口上限是否已解除
        /// </summary>
        public bool IsSupplyCapRemoved
        {
            get => _isSupplyCapRemoved;
            private set
            {
                if (_isSupplyCapRemoved != value)
                {
                    _isSupplyCapRemoved = value;
                    SupplyCapStatusChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 人口上限状态变化事件
        /// </summary>
        public event EventHandler<bool>? SupplyCapStatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MemoryCheatService()
        {
            _memoryEditor = new MemoryEditor();
            Logger.Info("内存秘籍服务初始化");
        }

        /// <summary>
        /// 解除星际争霸1人口上限
        /// </summary>
        /// <param name="processId">星际争霸进程ID</param>
        /// <param name="playerIndex">玩家索引（默认0=玩家1）</param>
        /// <returns>是否成功</returns>
        public bool RemoveStarCraftSupplyCap(int processId, int playerIndex = 0)
        {
            try
            {
                Logger.Info($"开始解除星际争霸1人口上限, PID: {processId}, 玩家索引: {playerIndex}");

                // 附加到进程
                if (!_memoryEditor.Attach(processId))
                {
                    Logger.Error("无法附加到星际争霸进程，请确保以管理员权限运行");
                    return false;
                }

                _targetProcessId = processId;

                // 计算当前玩家的地址偏移
                int playerOffset = playerIndex * PLAYER_ENTRY_SIZE;

                // 修改所有三个种族的补给上限和提供量（因为不确定玩家使用哪个种族）
                bool success = true;

                // 修改 Terran 补给
                success &= SetSupplyValues(SC_SUPPLY_PROVIDED_TERRAN, SC_SUPPLY_MAX_TERRAN, playerOffset, "Terran");

                // 修改 Zerg 补给
                success &= SetSupplyValues(SC_SUPPLY_PROVIDED_ZERG, SC_SUPPLY_MAX_ZERG, playerOffset, "Zerg");

                // 修改 Protoss 补给
                success &= SetSupplyValues(SC_SUPPLY_PROVIDED_PROTOSS, SC_SUPPLY_MAX_PROTOSS, playerOffset, "Protoss");

                if (success)
                {
                    IsSupplyCapRemoved = true;
                    StartSupplyMaintainer(processId, playerIndex);
                    Logger.Info("✅ 星际争霸1人口上限已成功解除");
                }
                else
                {
                    Logger.Warn("⚠️ 部分补给修改失败，可能版本不匹配");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "解除人口上限时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 设置指定种族的补给值
        /// </summary>
        private bool SetSupplyValues(IntPtr providedBase, IntPtr maxBase, int playerOffset, string raceName)
        {
            try
            {
                IntPtr providedAddr = IntPtr.Add(providedBase, playerOffset);
                IntPtr maxAddr = IntPtr.Add(maxBase, playerOffset);

                // 先读取当前值用于日志
                _memoryEditor.ReadInt32(providedAddr, out int currentProvided);
                _memoryEditor.ReadInt32(maxAddr, out int currentMax);
                Logger.Info($"{raceName} - 当前补给提供: {currentProvided / 2}, 当前补给上限: {currentMax / 2}");

                // 写入新的补给提供量和上限
                bool result1 = _memoryEditor.WriteInt32(providedAddr, SUPPLY_MAX_INTERNAL);
                bool result2 = _memoryEditor.WriteInt32(maxAddr, SUPPLY_MAX_INTERNAL);

                if (result1 && result2)
                {
                    Logger.Info($"{raceName} - 补给已设置为: {SUPPLY_MAX_INTERNAL / 2}");
                }

                return result1 && result2;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"设置{raceName}补给值失败");
                return false;
            }
        }

        /// <summary>
        /// 启动补给维持定时器
        /// 持续写入补给值，防止游戏重新计算覆盖
        /// </summary>
        private void StartSupplyMaintainer(int processId, int playerIndex)
        {
            StopSupplyMaintainer();

            _supplyTimer = new Timer(2000); // 每2秒维持一次
            _supplyTimer.Elapsed += (s, e) =>
            {
                try
                {
                    // 检查进程是否仍然存在
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        if (process.HasExited)
                        {
                            StopSupplyMaintainer();
                            IsSupplyCapRemoved = false;
                            return;
                        }
                    }
                    catch
                    {
                        StopSupplyMaintainer();
                        IsSupplyCapRemoved = false;
                        return;
                    }

                    // 重新附加（以防句柄失效）
                    if (!_memoryEditor.IsAttached)
                    {
                        _memoryEditor.Attach(processId);
                    }

                    int playerOffset = playerIndex * PLAYER_ENTRY_SIZE;

                    // 持续维持补给值
                    SetSupplyValues(SC_SUPPLY_PROVIDED_TERRAN, SC_SUPPLY_MAX_TERRAN, playerOffset, "Terran");
                    SetSupplyValues(SC_SUPPLY_PROVIDED_ZERG, SC_SUPPLY_MAX_ZERG, playerOffset, "Zerg");
                    SetSupplyValues(SC_SUPPLY_PROVIDED_PROTOSS, SC_SUPPLY_MAX_PROTOSS, playerOffset, "Protoss");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "维持补给值时发生错误");
                }
            };
            _supplyTimer.AutoReset = true;
            _supplyTimer.Start();

            Logger.Info("补给维持定时器已启动（每2秒刷新）");
        }

        /// <summary>
        /// 停止补给维持定时器
        /// </summary>
        private void StopSupplyMaintainer()
        {
            if (_supplyTimer != null)
            {
                _supplyTimer.Stop();
                _supplyTimer.Dispose();
                _supplyTimer = null;
                Logger.Info("补给维持定时器已停止");
            }
        }

        /// <summary>
        /// 恢复人口上限（恢复为默认200）
        /// </summary>
        public bool RestoreStarCraftSupplyCap(int processId, int playerIndex = 0)
        {
            try
            {
                StopSupplyMaintainer();

                if (!_memoryEditor.IsAttached)
                {
                    if (!_memoryEditor.Attach(processId))
                    {
                        Logger.Error("无法附加到进程");
                        return false;
                    }
                }

                int playerOffset = playerIndex * PLAYER_ENTRY_SIZE;

                // 恢复为默认200人口上限（内部值400）
                _memoryEditor.WriteInt32(IntPtr.Add(SC_SUPPLY_MAX_TERRAN, playerOffset), SUPPLY_200_INTERNAL);
                _memoryEditor.WriteInt32(IntPtr.Add(SC_SUPPLY_MAX_ZERG, playerOffset), SUPPLY_200_INTERNAL);
                _memoryEditor.WriteInt32(IntPtr.Add(SC_SUPPLY_MAX_PROTOSS, playerOffset), SUPPLY_200_INTERNAL);

                IsSupplyCapRemoved = false;
                _memoryEditor.Detach();

                Logger.Info("✅ 星际争霸1人口上限已恢复为200");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "恢复人口上限时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                StopSupplyMaintainer();
                _memoryEditor?.Dispose();
                _disposed = true;
                Logger.Info("内存秘籍服务已释放");
            }
        }
    }
}
