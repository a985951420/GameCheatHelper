using System;

namespace GameCheatHelper.Core.Models
{
    /// <summary>
    /// 游戏类型枚举
    /// </summary>
    public enum GameType
    {
        /// <summary>
        /// 未知游戏
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 魔兽争霸3
        /// </summary>
        Warcraft3 = 1,

        /// <summary>
        /// 星际争霸1
        /// </summary>
        StarCraft = 2
    }

    /// <summary>
    /// 游戏信息
    /// </summary>
    public class GameInfo
    {
        /// <summary>
        /// 游戏类型
        /// </summary>
        public GameType GameType { get; set; }

        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr WindowHandle { get; set; }

        /// <summary>
        /// 游戏是否正在运行
        /// </summary>
        public bool IsRunning => WindowHandle != IntPtr.Zero;

        /// <summary>
        /// 获取游戏显示名称
        /// </summary>
        public string DisplayName => GameType switch
        {
            GameType.Warcraft3 => "魔兽争霸3",
            GameType.StarCraft => "星际争霸1",
            _ => "未知游戏"
        };
    }
}
