using StampService.Application.Users;

namespace StampService.ApplicationTests.Users;

public class CuteUserDisplayNameGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnUnknownAnimalName()
    {
        var generator = new CuteUserDisplayNameGenerator();

        for (var i = 0; i < 100; i++)
        {
            var name = generator.Generate();

            Assert.Matches(@"^Неопознанн(ая|ый) .+", name);
        }
    }
}
