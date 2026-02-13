using System;
using System.Diagnostics;
using GameCheatHelper.Utilities;
using NLog;

namespace GameCheatHelper.Core
{
    /// <summary>
    /// 进程内存编辑器
    /// 提供读取和写入游戏进程内存的通用能力
    /// </summary>
    public class MemoryEditor : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private IntPtr _processHandle = IntPtr.Zero;
        private int _processId;
        private bool _disposed;

        /// <summary>
        /// 是否已成功打开进程
        /// </summary>
        public bool IsAttached => _processHandle != IntPtr.Zero;

        /// <summary>
        /// 当前附加的进程ID
        /// </summary>
        public int ProcessId => _processId;

        /// <summary>
        /// 附加到指定进程
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>是否成功附加</returns>
        public bool Attach(int processId)
        {
            try
            {
                // 先关闭之前的句柄
                Detach();

                _processHandle = Win32API.OpenProcess(
                    Win32API.PROCESS_VM_READ | Win32API.PROCESS_VM_WRITE | Win32API.PROCESS_VM_OPERATION | Win32API.PROCESS_QUERY_INFORMATION,
                    false,
                    processId);

                if (_processHandle == IntPtr.Zero)
                {
                    var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    Logger.Error($"打开进程失败, PID: {processId}, 错误码: {error}");
                    return false;
                }

                _processId = processId;
                Logger.Info($"成功附加到进程, PID: {processId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"附加进程异常, PID: {processId}");
                return false;
            }
        }

        /// <summary>
        /// 从进程分离
        /// </summary>
        public void Detach()
        {
            if (_processHandle != IntPtr.Zero)
            {
                Win32API.CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
                _processId = 0;
                Logger.Info("已从进程分离");
            }
        }

        /// <summary>
        /// 读取4字节整数
        /// </summary>
        /// <param name="address">内存地址</param>
        /// <param name="value">读取到的值</param>
        /// <returns>是否成功</returns>
        public bool ReadInt32(IntPtr address, out int value)
        {
            value = 0;
            if (!IsAttached) return false;

            byte[] buffer = new byte[4];
            bool success = Win32API.ReadProcessMemory(_processHandle, address, buffer, 4, out int bytesRead);

            if (success && bytesRead == 4)
            {
                value = BitConverter.ToInt32(buffer, 0);
                return true;
            }

            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logger.Warn($"读取内存失败, 地址: 0x{address.ToInt64():X}, 错误码: {error}");
            return false;
        }

        /// <summary>
        /// 写入4字节整数
        /// </summary>
        /// <param name="address">内存地址</param>
        /// <param name="value">要写入的值</param>
        /// <returns>是否成功</returns>
        public bool WriteInt32(IntPtr address, int value)
        {
            if (!IsAttached) return false;

            byte[] buffer = BitConverter.GetBytes(value);
            bool success = Win32API.WriteProcessMemory(_processHandle, address, buffer, 4, out int bytesWritten);

            if (success && bytesWritten == 4)
            {
                Debug.WriteLine($"写入内存成功, 地址: 0x{address.ToInt64():X}, 值: {value}");
                return true;
            }

            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logger.Warn($"写入内存失败, 地址: 0x{address.ToInt64():X}, 错误码: {error}");
            return false;
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        public bool ReadBytes(IntPtr address, byte[] buffer, int size)
        {
            if (!IsAttached) return false;

            return Win32API.ReadProcessMemory(_processHandle, address, buffer, size, out int bytesRead) && bytesRead == size;
        }

        /// <summary>
        /// 写入字节数组
        /// </summary>
        public bool WriteBytes(IntPtr address, byte[] buffer, int size)
        {
            if (!IsAttached) return false;

            return Win32API.WriteProcessMemory(_processHandle, address, buffer, size, out int bytesWritten) && bytesWritten == size;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Detach();
                _disposed = true;
            }
        }
    }
}
