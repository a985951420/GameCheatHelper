using GameCheatHelper.Core.Models;

namespace GameCheatHelper.ViewModels
{
    /// <summary>
    /// 热键冲突检测结果
    /// </summary>
    public class HotKeyConflictResult
    {
        /// <summary>
        /// 是否有冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 冲突的秘籍ID
        /// </summary>
        public string? ConflictingCheatCodeId { get; set; }

        /// <summary>
        /// 冲突的秘籍描述
        /// </summary>
        public string? ConflictingDescription { get; set; }

        /// <summary>
        /// 冲突的热键显示文本
        /// </summary>
        public string? HotKeyDisplayText { get; set; }
    }
}
