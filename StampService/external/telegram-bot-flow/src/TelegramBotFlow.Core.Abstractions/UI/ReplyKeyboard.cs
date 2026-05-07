using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.UI;

public sealed class ReplyKeyboard
{
    private readonly List<List<KeyboardButton>> _rows = [];
    private List<KeyboardButton> _currentRow = [];
    private bool _resizeKeyboard = true;
    private bool _oneTimeKeyboard;

    public ReplyKeyboard Button(string text)
    {
        _currentRow.Add(new KeyboardButton(text));
        return this;
    }

    public ReplyKeyboard RequestContact(string text)
    {
        _currentRow.Add(KeyboardButton.WithRequestContact(text));
        return this;
    }

    public ReplyKeyboard RequestLocation(string text)
    {
        _currentRow.Add(KeyboardButton.WithRequestLocation(text));
        return this;
    }

    public ReplyKeyboard Row()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
            _currentRow = [];
        }

        return this;
    }

    public ReplyKeyboard Resize(bool resize = true)
    {
        _resizeKeyboard = resize;
        return this;
    }

    public ReplyKeyboard OneTime(bool oneTime = true)
    {
        _oneTimeKeyboard = oneTime;
        return this;
    }

    public ReplyKeyboardMarkup Build()
    {
        if (_currentRow.Count > 0)
            _rows.Add(_currentRow);

        return new ReplyKeyboardMarkup(_rows)
        {
            ResizeKeyboard = _resizeKeyboard,
            OneTimeKeyboard = _oneTimeKeyboard
        };
    }

    public static ReplyKeyboardRemove Remove() => new();
}