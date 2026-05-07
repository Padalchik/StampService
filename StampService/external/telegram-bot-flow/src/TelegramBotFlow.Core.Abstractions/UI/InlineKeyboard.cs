using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.UI;

public sealed class InlineKeyboard
{
    private readonly List<List<InlineKeyboardButton>> _rows = [];
    private List<InlineKeyboardButton> _currentRow = [];

    public InlineKeyboard Button(string text, string callbackData)
    {
        _currentRow.Add(InlineKeyboardButton.WithCallbackData(text, callbackData));
        return this;
    }

    public InlineKeyboard Url(string text, string url)
    {
        _currentRow.Add(InlineKeyboardButton.WithUrl(text, url));
        return this;
    }

    public InlineKeyboard Row()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
            _currentRow = [];
        }

        return this;
    }

    public bool HasButtons => _rows.Count > 0 || _currentRow.Count > 0;

    public InlineKeyboardMarkup Build()
    {
        List<List<InlineKeyboardButton>> rows = [.. _rows];
        if (_currentRow.Count > 0)
            rows.Add(_currentRow);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup SingleButton(string text, string callbackData) =>
        new InlineKeyboard().Button(text, callbackData).Build();

    public static InlineKeyboardMarkup SingleUrl(string text, string url) =>
        new InlineKeyboard().Url(text, url).Build();
}