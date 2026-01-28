using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 全局热键管理器
    /// 负责注册、注销和监听全局热键
    /// </summary>
    public class HotKeyManager : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, HotKey> _registeredHotKeys;
        private int _nextHotKeyId = 1;
        private HwndSource? _hwndSource;

        /// <summary>
        /// 热键按下事件
        /// </summary>
        public event EventHandler<HotKey>? HotKeyPressed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public HotKeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _registeredHotKeys = new Dictionary<int, HotKey>();

            // 添加消息钩子
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
                Logger.Info("热键管理器初始化成功");
            }
            else
            {
                Logger.Warn("无法创建 HwndSource，热键功能可能无法正常工作");
            }
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="hotKey">要注册的热键</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterHotKey(HotKey hotKey)
        {
            try
            {
                // 检查热键冲突
                if (IsHotKeyConflict(hotKey))
                {
                    Logger.Warn($"热键 {hotKey.DisplayText} 已被注册");
                    return false;
                }

                var id = _nextHotKeyId++;
                var vkCode = KeyInterop.VirtualKeyFromKey(hotKey.Key);

                var success = Win32API.RegisterHotKey(
                    _windowHandle,
                    id,
                    hotKey.Modifiers | Win32API.MOD_NOREPEAT,
                    (uint)vkCode
                );

                if (success)
                {
                    hotKey.Id = id;
                    _registeredHotKeys[id] = hotKey;
                    Logger.Info($"热键注册成功: {hotKey.DisplayText} (ID: {id})");
                    return true;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Error($"热键注册失败: {hotKey.DisplayText}, 错误代码: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"注册热键时发生异常: {hotKey.DisplayText}");
                return false;
            }
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="hotKey">要注销的热键</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterHotKey(HotKey hotKey)
        {
            try
            {
                if (hotKey.Id == 0 || !_registeredHotKeys.ContainsKey(hotKey.Id))
                {
                    Logger.Warn($"热键未注册: {hotKey.DisplayText}");
                    return false;
                }

                var success = Win32API.UnregisterHotKey(_windowHandle, hotKey.Id);

                if (success)
                {
                    _registeredHotKeys.Remove(hotKey.Id);
                    Logger.Info($"热键注销成功: {hotKey.DisplayText} (ID: {hotKey.Id})");
                    return true;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Error($"热键注销失败: {hotKey.DisplayText}, 错误代码: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"注销热键时发生异常: {hotKey.DisplayText}");
                return false;
            }
        }

        /// <summary>
        /// 注销所有热键
        /// </summary>
        public void UnregisterAllHotKeys()
        {
            var hotKeys = new List<HotKey>(_registeredHotKeys.Values);
            foreach (var hotKey in hotKeys)
            {
                UnregisterHotKey(hotKey);
            }
            Logger.Info("所有热键已注销");
        }

        /// <summary>
        /// 检查热键是否冲突
        /// </summary>
        private bool IsHotKeyConflict(HotKey newHotKey)
        {
            foreach (var hotKey in _registeredHotKeys.Values)
            {
                if (hotKey.IsSameAs(newHotKey))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32API.WM_HOTKEY)
            {
                var id = wParam.ToInt32();

                if (_registeredHotKeys.TryGetValue(id, out var hotKey))
                {
                    Logger.Debug($"热键触发: {hotKey.DisplayText}");
                    HotKeyPressed?.Invoke(this, hotKey);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            UnregisterAllHotKeys();
            _hwndSource?.RemoveHook(WndProc);
            Logger.Info("热键管理器已释放");
        }
    }
}
