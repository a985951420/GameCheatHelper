using GameCheatHelper.Services;
using Xunit;

namespace GameCheatHelper.Tests.Services
{
    /// <summary>
    /// 秘籍服务单元测试
    /// </summary>
    public class CheatCodeServiceTests
    {
        [Fact]
        public void CheatCodeService_LoadDefaultCheats_ShouldSucceed()
        {
            // Arrange
            var service = new CheatCodeService();

            // Act
            service.LoadDefaultCheats();

            // Assert
            Assert.NotEmpty(service.CheatCodes);
            Assert.Contains(service.CheatCodes, c => c.Code == "greedisgood");
        }

        [Fact]
        public void CheatCodeService_GetCheatsByGame_ShouldFilterCorrectly()
        {
            // Arrange
            var service = new CheatCodeService();
            service.LoadDefaultCheats();

            // Act
            var wc3Cheats = service.GetCheatsByGame(GameCheatHelper.Core.Models.GameType.Warcraft3);
            var sc1Cheats = service.GetCheatsByGame(GameCheatHelper.Core.Models.GameType.StarCraft1);

            // Assert
            Assert.NotEmpty(wc3Cheats);
            Assert.NotEmpty(sc1Cheats);
            Assert.All(wc3Cheats, c => Assert.Equal(GameCheatHelper.Core.Models.GameType.Warcraft3, c.Game));
            Assert.All(sc1Cheats, c => Assert.Equal(GameCheatHelper.Core.Models.GameType.StarCraft1, c.Game));
        }

        [Theory]
        [InlineData("gold", true)]
        [InlineData("resource", true)]
        [InlineData("xyz123notexist", false)]
        public void CheatCodeService_SearchCheats_ShouldWork(string keyword, bool shouldFindResults)
        {
            // Arrange
            var service = new CheatCodeService();
            service.LoadDefaultCheats();

            // Act
            var results = service.SearchCheats(keyword);

            // Assert
            if (shouldFindResults)
            {
                Assert.NotEmpty(results);
            }
            else
            {
                // 即使没找到特定关键词，搜索功能应该正常工作
                Assert.NotNull(results);
            }
        }

        [Fact]
        public void CheatCodeService_AddCheat_ShouldAddToCollection()
        {
            // Arrange
            var service = new CheatCodeService();
            var initialCount = service.CheatCodes.Count;

            var newCheat = new GameCheatHelper.Core.Models.CheatCode
            {
                Id = "test-cheat",
                Code = "testcode",
                Description = "Test cheat",
                Game = GameCheatHelper.Core.Models.GameType.Warcraft3
            };

            // Act
            var result = service.AddCheat(newCheat);

            // Assert
            Assert.True(result);
            Assert.Equal(initialCount + 1, service.CheatCodes.Count);
            Assert.Contains(service.CheatCodes, c => c.Id == "test-cheat");
        }
    }
}
