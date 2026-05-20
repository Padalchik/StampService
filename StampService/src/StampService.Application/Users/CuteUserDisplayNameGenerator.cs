using System.Security.Cryptography;

namespace StampService.Application.Users;

public class CuteUserDisplayNameGenerator : IUserDisplayNameGenerator
{
    private static readonly UnknownAnimalName[] Names =
    [
        new("Неопознанная", "альпака"),
        new("Неопознанная", "белка"),
        new("Неопознанная", "выдра"),
        new("Неопознанная", "капибара"),
        new("Неопознанная", "коала"),
        new("Неопознанный", "кролик"),
        new("Неопознанный", "лемур"),
        new("Неопознанная", "лиса"),
        new("Неопознанная", "морская свинка"),
        new("Неопознанная", "панда"),
        new("Неопознанный", "пингвин"),
        new("Неопознанный", "сурикат"),
        new("Неопознанный", "тюлень"),
        new("Неопознанный", "фенек"),
        new("Неопознанный", "хомяк"),
        new("Неопознанная", "шиншилла")
    ];

    public string Generate()
    {
        var index = RandomNumberGenerator.GetInt32(Names.Length);
        var name = Names[index];

        return $"{name.Adjective} {name.Animal}";
    }

    private sealed record UnknownAnimalName(string Adjective, string Animal);
}
