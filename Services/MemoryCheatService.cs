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
        private Timer? _resourceTimer;
        private Timer? _buildSpeedTimer;
        private int _targetProcessId;
        private bool _isSupplyCapRemoved;
        private bool _isResourceBoostActive;
        private bool _isBuildSpeedBoostActive;
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

        // ============================================================
        // 星际争霸1 资源相关内存地址
        // 水晶矿(Minerals): 0x0057F0F0 (Player 0 起始, 每个玩家+4)
        // 瓦斯(Gas):        0x0057F120 (Player 0 起始, 每个玩家+4)
        // ============================================================
        private static readonly IntPtr SC_MINERALS_BASE = new IntPtr(0x0057F0F0);
        private static readonly IntPtr SC_GAS_BASE = new IntPtr(0x0057F120);

        // ============================================================
        // 星际争霸1 建造速度相关内存地址
        // 每个玩家的建造速度修改器: 0x00584140 (Player 0 起始, 每个玩家+1字节)
        // 值设为1=正常, 值设为0=极速建造
        // ============================================================
        private static readonly IntPtr SC_BUILD_SPEED_BASE = new IntPtr(0x006509C0);
        private const int PLAYER_BUILD_SPEED_ENTRY_SIZE = 1;

        // 内部值常量
        private const int SUPPLY_200_INTERNAL = 400;    // 200人口对应内部值400
        private const int SUPPLY_MAX_INTERNAL = 1600;   // 设置为800人口上限（内部值1600）
        private const int PLAYER_ENTRY_SIZE = 4;        // 每个玩家条目4字节
        private const int DEFAULT_ADD_MINERALS = 10000;  // 默认加矿量
        private const int DEFAULT_ADD_GAS = 10000;       // 默认加气量

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
        /// 资源加成是否已激活
        /// </summary>
        public bool IsResourceBoostActive
        {
            get => _isResourceBoostActive;
            private set
            {
                if (_isResourceBoostActive != value)
                {
                    _isResourceBoostActive = value;
                    ResourceBoostStatusChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 资源加成状态变化事件
        /// </summary>
        public event EventHandler<bool>? ResourceBoostStatusChanged;

        /// <summary>
        /// 建造加速是否已激活
        /// </summary>
        public bool IsBuildSpeedBoostActive
        {
            get => _isBuildSpeedBoostActive;
            private set
            {
                if (_isBuildSpeedBoostActive != value)
                {
                    _isBuildSpeedBoostActive = value;
                    BuildSpeedBoostStatusChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 建造加速状态变化事件
        /// </summary>
        public event EventHandler<bool>? BuildSpeedBoostStatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MemoryCheatService()
        {
            _memoryEditor = new MemoryEditor();
            Logger.Info("内存秘籍服务初始化");
        }

        /// <summary>
        /// 扫描并查找当前玩家的索引（用于多人模式调试）
        /// 通过检查哪个玩家槽位的资源值在合理范围内来判断
        /// </summary>
        public int ScanForPlayerIndex(int processId)
        {
            try
            {
                Logger.Info("🔍 开始扫描玩家索引...");

                if (!_memoryEditor.IsAttached)
                {
                    if (!_memoryEditor.Attach(processId))
                    {
                        Logger.Error("无法附加到星际争霸进程");
                        return -1;
                    }
                }

                // 扫描所有8个可能的玩家槽位
                for (int i = 0; i < 8; i++)
                {
                    int playerOffset = i * PLAYER_ENTRY_SIZE;
                    IntPtr mineralsAddr = IntPtr.Add(SC_MINERALS_BASE, playerOffset);
                    IntPtr gasAddr = IntPtr.Add(SC_GAS_BASE, playerOffset);

                    _memoryEditor.ReadInt32(mineralsAddr, out int minerals);
                    _memoryEditor.ReadInt32(gasAddr, out int gas);

                    Logger.Info($"玩家 {i}: 矿物={minerals}, 瓦斯={gas} (地址: 0x{mineralsAddr.ToInt64():X})");

                    // 如果资源值在合理范围内（0-100000），可能是当前玩家
                    if (minerals >= 0 && minerals < 100000 && gas >= 0 && gas < 100000)
                    {
                        Logger.Info($"✅ 可能的玩家索引: {i} (矿物={minerals}, 瓦斯={gas})");
                    }
                }

                Logger.Info("扫描完成，请查看上面的日志判断哪个是你的玩家索引");
                return -1; // 返回-1表示需要手动判断
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "扫描玩家索引时发生错误");
                return -1;
            }
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
        /// 给自己加资源（水晶矿+瓦斯）
        /// </summary>
        /// <param name="processId">星际争霸进程ID</param>
        /// <param name="playerIndex">玩家索引（默认0=玩家1）</param>
        /// <param name="addMinerals">增加的水晶矿量</param>
        /// <param name="addGas">增加的瓦斯量</param>
        /// <returns>是否成功</returns>
        public bool AddStarCraftResources(int processId, int playerIndex = 0, int addMinerals = DEFAULT_ADD_MINERALS, int addGas = DEFAULT_ADD_GAS)
        {
            try
            {
                Logger.Info($"给玩家{playerIndex + 1}加资源, PID: {processId}, 矿: +{addMinerals}, 气: +{addGas}");

                if (!_memoryEditor.IsAttached)
                {
                    if (!_memoryEditor.Attach(processId))
                    {
                        Logger.Error("无法附加到星际争霸进程，请确保以管理员权限运行");
                        return false;
                    }
                }

                int playerOffset = playerIndex * PLAYER_ENTRY_SIZE;
                IntPtr mineralsAddr = IntPtr.Add(SC_MINERALS_BASE, playerOffset);
                IntPtr gasAddr = IntPtr.Add(SC_GAS_BASE, playerOffset);

                Logger.Info($"🔍 矿物地址: 0x{mineralsAddr.ToInt64():X}, 气体地址: 0x{gasAddr.ToInt64():X}");

                // 读取当前资源
                _memoryEditor.ReadInt32(mineralsAddr, out int currentMinerals);
                _memoryEditor.ReadInt32(gasAddr, out int currentGas);
                Logger.Info($"当前资源 - 水晶矿: {currentMinerals}, 瓦斯: {currentGas}");

                // 如果读取到的值异常（<0 或 >1000000），说明地址可能不对
                if (currentMinerals < 0 || currentMinerals > 1000000 || currentGas < 0 || currentGas > 1000000)
                {
                    Logger.Warn($"⚠️ 读取到异常资源值，可能内存地址不正确");
                }

                // 写入新的资源值（当前值 + 增加量）
                bool result1 = _memoryEditor.WriteInt32(mineralsAddr, currentMinerals + addMinerals);
                bool result2 = _memoryEditor.WriteInt32(gasAddr, currentGas + addGas);

                // 验证写入
                _memoryEditor.ReadInt32(mineralsAddr, out int verifyMinerals);
                _memoryEditor.ReadInt32(gasAddr, out int verifyGas);
                Logger.Info($"🔍 写入后验证 - 水晶矿: {verifyMinerals}, 瓦斯: {verifyGas}");

                if (verifyMinerals != (currentMinerals + addMinerals) || verifyGas != (currentGas + addGas))
                {
                    Logger.Warn($"⚠️ 写入验证失败！预期 {currentMinerals + addMinerals}/{currentGas + addGas}，实际 {verifyMinerals}/{verifyGas}");
                }

                if (result1 && result2)
                {
                    Logger.Info($"✅ 资源已增加 - 水晶矿: {currentMinerals} → {currentMinerals + addMinerals}, 瓦斯: {currentGas} → {currentGas + addGas}");
                }

                return result1 && result2;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加资源时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 启动持续加资源定时器（每5秒自动加资源）
        /// </summary>
        public bool StartResourceBoost(int processId, int playerIndex = 0, int addMinerals = DEFAULT_ADD_MINERALS, int addGas = DEFAULT_ADD_GAS)
        {
            try
            {
                if (!_memoryEditor.IsAttached)
                {
                    if (!_memoryEditor.Attach(processId))
                    {
                        Logger.Error("无法附加到星际争霸进程");
                        return false;
                    }
                }

                // 先立即加一次
                AddStarCraftResources(processId, playerIndex, addMinerals, addGas);

                StopResourceBoost();

                _resourceTimer = new Timer(5000); // 每5秒加一次
                _resourceTimer.Elapsed += (s, e) =>
                {
                    try
                    {
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            if (process.HasExited)
                            {
                                StopResourceBoost();
                                return;
                            }
                        }
                        catch
                        {
                            StopResourceBoost();
                            return;
                        }

                        if (!_memoryEditor.IsAttached)
                        {
                            _memoryEditor.Attach(processId);
                        }

                        AddStarCraftResources(processId, playerIndex, addMinerals, addGas);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "持续加资源时发生错误");
                    }
                };
                _resourceTimer.AutoReset = true;
                _resourceTimer.Start();

                IsResourceBoostActive = true;
                Logger.Info($"✅ 持续加资源已启动（每5秒 +{addMinerals}矿 +{addGas}气）");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "启动持续加资源失败");
                return false;
            }
        }

        /// <summary>
        /// 停止持续加资源
        /// </summary>
        public void StopResourceBoost()
        {
            if (_resourceTimer != null)
            {
                _resourceTimer.Stop();
                _resourceTimer.Dispose();
                _resourceTimer = null;
                IsResourceBoostActive = false;
                Logger.Info("持续加资源已停止");
            }
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
        /// 启动建造加速（给自己）
        /// </summary>
        public bool StartBuildSpeedBoost(int processId, int playerIndex = 0)
        {
            try
            {
                Logger.Info($"开始启动建造加速, 玩家: {playerIndex + 1}");

                if (!_memoryEditor.IsAttached)
                {
                    if (!_memoryEditor.Attach(processId))
                    {
                        Logger.Error("无法附加到星际争霸进程");
                        return false;
                    }
                }

                // 立即设置一次
                IntPtr buildSpeedAddr = IntPtr.Add(SC_BUILD_SPEED_BASE, playerIndex * PLAYER_BUILD_SPEED_ENTRY_SIZE);
                Logger.Info($"🔍 建造加速地址: 0x{buildSpeedAddr.ToInt64():X}");
                
                // 先读取当前值
                byte[] currentValue = new byte[1];
                _memoryEditor.ReadBytes(buildSpeedAddr, currentValue, 1);
                Logger.Info($"当前建造速度值: {currentValue[0]}");
                
                byte[] speedValue = new byte[] { 0 }; // 0=极速建造
                _memoryEditor.WriteBytes(buildSpeedAddr, speedValue, 1);
                
                // 验证写入
                byte[] verifyValue = new byte[1];
                _memoryEditor.ReadBytes(buildSpeedAddr, verifyValue, 1);
                Logger.Info($"🔍 写入后验证值: {verifyValue[0]}");
                
                if (verifyValue[0] != 0)
                {
                    Logger.Warn($"⚠️ 建造加速写入验证失败！预期 0，实际 {verifyValue[0]}");
                }

                StopBuildSpeedBoost();

                _buildSpeedTimer = new Timer(1000); // 每1秒维持一次
                _buildSpeedTimer.Elapsed += (s, e) =>
                {
                    try
                    {
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            if (process.HasExited)
                            {
                                StopBuildSpeedBoost();
                                return;
                            }
                        }
                        catch
                        {
                            StopBuildSpeedBoost();
                            return;
                        }

                        if (!_memoryEditor.IsAttached)
                        {
                            _memoryEditor.Attach(processId);
                        }

                        // 持续维持建造加速
                        IntPtr addr = IntPtr.Add(SC_BUILD_SPEED_BASE, playerIndex * PLAYER_BUILD_SPEED_ENTRY_SIZE);
                        byte[] value = new byte[] { 0 };
                        _memoryEditor.WriteBytes(addr, value, 1);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "维持建造加速时发生错误");
                    }
                };
                _buildSpeedTimer.AutoReset = true;
                _buildSpeedTimer.Start();

                IsBuildSpeedBoostActive = true;
                Logger.Info("✅ 建造加速已启动");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "启动建造加速失败");
                return false;
            }
        }

        /// <summary>
        /// 停止建造加速
        /// </summary>
        public void StopBuildSpeedBoost()
        {
            if (_buildSpeedTimer != null)
            {
                _buildSpeedTimer.Stop();
                _buildSpeedTimer.Dispose();
                _buildSpeedTimer = null;
                IsBuildSpeedBoostActive = false;
                Logger.Info("建造加速已停止");
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
                StopResourceBoost();
                StopBuildSpeedBoost();
                _memoryEditor?.Dispose();
                _disposed = true;
                Logger.Info("内存秘籍服务已释放");
            }
        }
    }
}
