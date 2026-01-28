namespace GameCheatHelper.Core.Models
{
    /// <summary>
    /// 秘籍代码数据模型
    /// </summary>
    public class CheatCode
    {
        /// <summary>
        /// 秘籍唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 所属游戏类型
        /// </summary>
        public GameType Game { get; set; }

        /// <summary>
        /// 秘籍代码（实际输入的文本）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 秘籍描述（说明秘籍效果）
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 秘籍分类（如：资源、地图、单位等）
        /// </summary>
        public string Category { get; set; } = "通用";

        /// <summary>
        /// 是否启用此秘籍
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 绑定的热键ID（可选）
        /// </summary>
        public int? HotKeyId { get; set; }

        public override string ToString()
        {
            return $"{Code} - {Description}";
        }
    }
}
