namespace TelegramBotFlow.Core.Endpoints;

/// <summary>
/// Маркер именованной точки взаимодействия. Связывает View с Handler
/// без строковых идентификаторов. Используется для кнопок (Button&lt;T&gt;, MapAction&lt;T&gt;)
/// и для ввода (AwaitInput&lt;T&gt;, MapInput&lt;T&gt;).
/// </summary>
public interface IBotAction;