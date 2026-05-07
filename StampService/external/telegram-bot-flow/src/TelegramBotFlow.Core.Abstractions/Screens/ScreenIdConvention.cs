using System.Text;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Соглашение о преобразовании типа экрана в строковый идентификатор.
/// Используется как в <see cref="ScreenView"/> (кнопки навигации), так и в ScreenRegistry (регистрация).
/// Правила: удаляется суффикс «Screen», результат переводится в snake_case.
/// Пример: <c>MainMenuScreen → main_menu</c>.
/// </summary>
public static class ScreenIdConvention
{
    public static string GetIdFromType(Type screenType)
    {
        string name = screenType.Name;

        if (name.EndsWith("Screen", StringComparison.Ordinal))
            name = name[..^"Screen".Length];

        return ToSnakeCase(name);
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]))
                sb.Append('_');
            sb.Append(char.ToLower(input[i]));
        }

        return sb.ToString();
    }
}