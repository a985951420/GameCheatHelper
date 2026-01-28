using System.Windows.Input;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core.Models
{
    /// <summary>
    /// 热键数据模型
    /// </summary>
    public class HotKey
    {
        /// <summary>
        /// 热键唯一标识符
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 按键
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// 修饰键（Ctrl, Alt, Shift, Win）
        /// </summary>
        public uint Modifiers { get; set; }

        /// <summary>
        /// 关联的秘籍代码ID
        /// </summary>
        public string? CheatCodeId { get; set; }

        /// <summary>
        /// 获取热键的显示文本
        /// </summary>
        public string DisplayText
        {
            get
            {
                var text = string.Empty;

                if ((Modifiers & Win32API.MOD_CONTROL) != 0)
                    text += "Ctrl+";

                if ((Modifiers & Win32API.MOD_ALT) != 0)
                    text += "Alt+";

                if ((Modifiers & Win32API.MOD_SHIFT) != 0)
                    text += "Shift+";

                if ((Modifiers & Win32API.MOD_WIN) != 0)
                    text += "Win+";

                text += Key.ToString();

                return text;
            }
        }

        /// <summary>
        /// 检查两个热键是否相同
        /// </summary>
        public bool IsSameAs(HotKey other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
