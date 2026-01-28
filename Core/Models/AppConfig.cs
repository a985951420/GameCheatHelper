using System.Collections.Generic;

namespace GameCheatHelper.Core.Models
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 配置文件版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 用户设置
        /// </summary>
        public Settings Settings { get; set; } = new Settings();

        /// <summary>
        /// 秘籍代码列表
        /// </summary>
        public List<CheatCode> CheatCodes { get; set; } = new List<CheatCode>();

        /// <summary>
        /// 热键配置列表
        /// </summary>
        public List<HotKey> HotKeys { get; set; } = new List<HotKey>();
    }

    /// <summary>
    /// 用户设置
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// 键盘输入延迟（毫秒）
        /// </summary>
        public int InputDelay { get; set; } = 10;

        /// <summary>
        /// 开机自动启动
        /// </summary>
        public bool StartWithWindows { get; set; } = false;

        /// <summary>
        /// 启动时最小化到托盘
        /// </summary>
        public bool StartMinimized { get; set; } = false;

        /// <summary>
        /// 最小化到系统托盘
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// 关闭窗口时最小化到托盘
        /// </summary>
        public bool CloseToTray { get; set; } = false;

        /// <summary>
        /// 显示通知
        /// </summary>
        public bool ShowNotifications { get; set; } = true;

        /// <summary>
        /// 语言代码（如：zh-CN, en-US）
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// 游戏检测间隔（毫秒）
        /// </summary>
        public int DetectionInterval { get; set; } = 2000;
    }
}
