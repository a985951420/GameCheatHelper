using GameCheatHelper.Core.Models;
using Xunit;

namespace GameCheatHelper.Tests.Core
{
    /// <summary>
    /// 热键模型单元测试
    /// </summary>
    public class HotKeyTests
    {
        [Fact]
        public void HotKey_DisplayText_ShouldFormatCorrectly()
        {
            // Arrange
            var hotKey = new HotKey
            {
                Key = System.Windows.Forms.Keys.F1,
                Modifiers = HotKeyModifiers.None
            };

            // Act
            var displayText = hotKey.DisplayText;

            // Assert
            Assert.Equal("F1", displayText);
        }

        [Fact]
        public void HotKey_WithModifiers_DisplayText_ShouldFormatCorrectly()
        {
            // Arrange
            var hotKey = new HotKey
            {
                Key = System.Windows.Forms.Keys.F1,
                Modifiers = HotKeyModifiers.Control | HotKeyModifiers.Alt
            };

            // Act
            var displayText = hotKey.DisplayText;

            // Assert
            Assert.Contains("Ctrl", displayText);
            Assert.Contains("Alt", displayText);
            Assert.Contains("F1", displayText);
        }

        [Fact]
        public void HotKey_Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var hotKey1 = new HotKey
            {
                Key = System.Windows.Forms.Keys.F1,
                Modifiers = HotKeyModifiers.Control
            };

            var hotKey2 = new HotKey
            {
                Key = System.Windows.Forms.Keys.F1,
                Modifiers = HotKeyModifiers.Control
            };

            var hotKey3 = new HotKey
            {
                Key = System.Windows.Forms.Keys.F2,
                Modifiers = HotKeyModifiers.Control
            };

            // Act & Assert
            Assert.Equal(hotKey1.Key, hotKey2.Key);
            Assert.Equal(hotKey1.Modifiers, hotKey2.Modifiers);
            Assert.NotEqual(hotKey1.Key, hotKey3.Key);
        }
    }
}
