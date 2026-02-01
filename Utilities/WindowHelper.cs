using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GameCheatHelper.Utilities
{
    /// <summary>
    /// 窗口辅助类 - 提供全屏检测和增强的窗口激活功能
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// 检测窗口是否处于全屏独占模式
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>true 表示全屏，false 表示窗口化</returns>
        public static bool IsFullScreen(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            try
            {
                // 检测窗口是否最小化
                if (Win32API.IsIconic(windowHandle))
                    return false;

                // 获取窗口矩形区域
                if (!Win32API.GetWindowRect(windowHandle, out var windowRect))
                    return false;

                // 获取窗口所在的显示器
                var monitor = Win32API.MonitorFromWindow(windowHandle, Win32API.MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero)
                    return false;

                // 获取显示器信息
                var monitorInfo = new Win32API.MONITORINFO();
                monitorInfo.Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32API.MONITORINFO));

                if (!Win32API.GetMonitorInfo(monitor, ref monitorInfo))
                    return false;

                // 比较窗口大小和显示器大小
                var isFullWidth = windowRect.Width == monitorInfo.Monitor.Width;
                var isFullHeight = windowRect.Height == monitorInfo.Monitor.Height;
                var isAtTopLeft = windowRect.Left == monitorInfo.Monitor.Left &&
                                  windowRect.Top == monitorInfo.Monitor.Top;

                // 全屏条件：窗口覆盖整个显示器且位置在左上角
                return isFullWidth && isFullHeight && isAtTopLeft;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测全屏状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取窗口状态描述（用于日志）
        /// </summary>
        public static string GetWindowStateDescription(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return "无效窗口句柄";

            var isMinimized = Win32API.IsIconic(windowHandle);
            var isFullScreen = IsFullScreen(windowHandle);
            var isForeground = Win32API.GetForegroundWindow() == windowHandle;

            return $"最小化:{isMinimized}, 全屏:{isFullScreen}, 前台:{isForeground}";
        }

        /// <summary>
        /// 增强的窗口激活方法 - 支持全屏和最小化窗口
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>true 表示激活成功</returns>
        public static async Task<bool> ActivateWindowAsync(IntPtr windowHandle, int maxRetries = 3)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"窗口激活尝试 {attempt}/{maxRetries}");
                    Debug.WriteLine($"当前窗口状态: {GetWindowStateDescription(windowHandle)}");

                    // 1. 如果窗口最小化，先恢复
                    if (Win32API.IsIconic(windowHandle))
                    {
                        Debug.WriteLine("检测到窗口最小化，正在恢复...");
                        Win32API.ShowWindow(windowHandle, Win32API.SW_RESTORE);
                        await Task.Delay(200); // 等待窗口恢复动画
                    }

                    // 2. 检测是否全屏
                    bool isFullScreen = IsFullScreen(windowHandle);
                    Debug.WriteLine($"窗口全屏状态: {isFullScreen}");

                    // 3. 使用线程附加技术增强激活（特别对全屏窗口有效）
                    bool activationSuccess = false;

                    // 获取目标窗口的线程ID
                    uint targetThreadId = Win32API.GetWindowThreadProcessId(windowHandle, out _);
                    uint currentThreadId = Win32API.GetCurrentThreadId();

                    // 如果不是同一个线程，尝试附加
                    bool threadsAttached = false;
                    if (targetThreadId != currentThreadId && targetThreadId != 0)
                    {
                        threadsAttached = Win32API.AttachThreadInput(currentThreadId, targetThreadId, true);
                        Debug.WriteLine($"线程附加结果: {threadsAttached}");
                    }

                    try
                    {
                        // 尝试激活窗口
                        activationSuccess = Win32API.SetForegroundWindow(windowHandle);
                        Debug.WriteLine($"SetForegroundWindow 结果: {activationSuccess}");
                    }
                    finally
                    {
                        // 解除线程附加
                        if (threadsAttached)
                        {
                            Win32API.AttachThreadInput(currentThreadId, targetThreadId, false);
                        }
                    }

                    // 4. 等待窗口切换（全屏模式需要更长时间）
                    int waitTime = isFullScreen ? 500 : 200;
                    await Task.Delay(waitTime);

                    // 5. 验证激活是否成功
                    bool isForeground = Win32API.GetForegroundWindow() == windowHandle;
                    Debug.WriteLine($"激活验证结果: {isForeground}");

                    if (isForeground)
                    {
                        Debug.WriteLine($"窗口激活成功 (尝试 {attempt}/{maxRetries})");
                        return true;
                    }

                    // 如果未成功且是全屏，尝试显示窗口
                    if (!isForeground && isFullScreen)
                    {
                        Debug.WriteLine("全屏窗口激活失败，尝试 ShowWindow...");
                        Win32API.ShowWindow(windowHandle, Win32API.SW_SHOW);
                        await Task.Delay(300);

                        if (Win32API.GetForegroundWindow() == windowHandle)
                        {
                            Debug.WriteLine("ShowWindow 成功激活窗口");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"窗口激活异常 (尝试 {attempt}/{maxRetries}): {ex.Message}");
                }

                // 在重试前等待
                if (attempt < maxRetries)
                {
                    await Task.Delay(300);
                }
            }

            Debug.WriteLine($"窗口激活失败，已用尽所有 {maxRetries} 次尝试");
            return false;
        }

        /// <summary>
        /// 快速检查窗口是否有效且可激活
        /// </summary>
        public static bool IsWindowValid(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            try
            {
                // 尝试获取窗口矩形，如果失败说明窗口无效
                return Win32API.GetWindowRect(windowHandle, out _);
            }
            catch
            {
                return false;
            }
        }
    }
}
