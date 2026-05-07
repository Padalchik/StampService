using System.Text.Json;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Прикладные данные пользовательской сессии: key-value хранилище.
/// Владелец: прикладной код экранов и визардов.
/// Фреймворк не записывает сюда ничего, кроме очистки.
/// </summary>
public sealed class SessionData
{
    private readonly Dictionary<string, string> _data = [];

    public void Set(string key, string value) => _data[key] = value;

    /// <summary>
    /// Сохраняет типизированное значение, сериализуя его в JSON.
    /// </summary>
    public void Set<T>(string key, T value) => _data[key] = JsonSerializer.Serialize(value);

    public string? GetString(string key) => _data.GetValueOrDefault(key);

    /// <summary>
    /// Возвращает типизированное значение, десериализуя его из JSON.
    /// Возвращает <see langword="default"/> если ключ отсутствует или десериализация не удалась.
    /// </summary>
    public T? Get<T>(string key) =>
        _data.TryGetValue(key, out string? json) ? JsonSerializer.Deserialize<T>(json) : default;

    public int? GetInt(string key) =>
        _data.TryGetValue(key, out string? value) && int.TryParse(value, out int result) ? result : null;

    public long? GetLong(string key) =>
        _data.TryGetValue(key, out string? value) && long.TryParse(value, out long result) ? result : null;

    /// <summary>
    /// Возвращает <see langword="true"/> если ключ установлен в "true",
    /// <see langword="false"/> если ключ установлен в другое значение,
    /// <see langword="null"/> если ключ отсутствует.
    /// </summary>
    public bool? GetBool(string key) =>
        _data.TryGetValue(key, out string? value) ? value is "true" : null;

    public bool Has(string key) => _data.ContainsKey(key);

    public void Remove(string key) => _data.Remove(key);

    public void Clear() => _data.Clear();

    internal IReadOnlyDictionary<string, string> GetAll() => _data;

    internal void Populate(IReadOnlyDictionary<string, string> data)
    {
        foreach ((string key, string value) in data)
            _data[key] = value;
    }
}