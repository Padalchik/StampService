namespace StampService.Domain.Access;

public enum PermissionCode
{
    /// <summary>
    /// Полное управление брендом (создание/удаление, настройки, управление сотрудниками)
    /// </summary>
    BrandManage = 1,

    /// <summary>
    /// Создание и редактирование метрик лояльности
    /// </summary>
    MetricCreate = 2,

    /// <summary>
    /// Управление сотрудниками (добавление/удаление из команды)
    /// </summary>
    StaffManage = 3,

    /// <summary>
    /// Выдача штампов/баллов клиентам
    /// </summary>
    StampIssue = 4,

    /// <summary>
    /// Просмотр балансов клиентов
    /// </summary>
    BalanceView = 5
}
