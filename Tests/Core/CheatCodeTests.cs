using GameCheatHelper.Core.Models;
using Xunit;

namespace GameCheatHelper.Tests.Core
{
    /// <summary>
    /// 秘籍数据模型单元测试
    /// </summary>
    public class CheatCodeTests
    {
        [Fact]
        public void CheatCode_Creation_ShouldSetProperties()
        {
            // Arrange & Act
            var cheat = new CheatCode
            {
                Id = "test-001",
                Code = "greedisgood",
                Description = "增加金币和木材",
                Game = GameType.Warcraft3,
                Category = "资源",
                Enabled = true
            };

            // Assert
            Assert.Equal("test-001", cheat.Id);
            Assert.Equal("greedisgood", cheat.Code);
            Assert.Equal("增加金币和木材", cheat.Description);
            Assert.Equal(GameType.Warcraft3, cheat.Game);
            Assert.Equal("资源", cheat.Category);
            Assert.True(cheat.Enabled);
        }

        [Theory]
        [InlineData("", "描述", false)]
        [InlineData("code", "", false)]
        [InlineData("code", "描述", true)]
        public void CheatCode_Validation_ShouldWork(string code, string description, bool expectedValid)
        {
            // Arrange
            var cheat = new CheatCode
            {
                Code = code,
                Description = description,
                Game = GameType.Warcraft3
            };

            // Act
            bool isValid = !string.IsNullOrWhiteSpace(cheat.Code) &&
                          !string.IsNullOrWhiteSpace(cheat.Description);

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public void CheatCode_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var cheat = new CheatCode();

            // Assert
            Assert.True(cheat.Enabled); // 秘籍默认应该是启用状态
            Assert.Equal("通用", cheat.Category); // 默认分类应该是'通用'
        }
    }
}
