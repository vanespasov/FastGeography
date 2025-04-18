namespace FastGeography.Tests
{
    using FastGeography.Shared;

    public class GameTests
    {
        [Fact]
        public void SetCssClass_ShouldReturnCorrectClass()
        {
            // Arrange
            var game = new Game();

            // Act & Assert
            Assert.Equal("table-light", game.SetCssClass(0));
            Assert.Equal("table-success", game.SetCssClass(10));
            Assert.Equal("table-danger", game.SetCssClass(-5));
        }
    }
}