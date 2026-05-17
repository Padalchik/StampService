namespace StampService.Application.Demo;

public static class DemoDataCatalog
{
    public static IReadOnlyCollection<DemoBrandTemplate> BrandTemplates { get; } =
    [
        new(
            "Кофейная лаборатория",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Кофейные визиты", 6),
                new DemoMetricTemplate("Десерты", 4),
                new DemoMetricTemplate("Завтраки", 5)
            ],
            Products:
            [
                new DemoProductTemplate("Американо", 8),
                new DemoProductTemplate("Капучино", 12),
                new DemoProductTemplate("Круассан", 10)
            ]),
        new(
            "Городская пекарня",
            IsMetricsEnabled: true,
            IsCoinsEnabled: false,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Покупки хлеба", 5),
                new DemoMetricTemplate("Утренние визиты", 3),
                new DemoMetricTemplate("Сладкая выпечка", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Булочка с корицей", 7),
                new DemoProductTemplate("Багет", 9)
            ]),
        new(
            "Барбершоп Север",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Стрижки", 5),
                new DemoMetricTemplate("Уход за бородой", 4),
                new DemoMetricTemplate("Комплексные визиты", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Воск для укладки", 14),
                new DemoProductTemplate("Шампунь", 18),
                new DemoProductTemplate("Скидка на стрижку", 20)
            ]),
        new(
            "Студия маникюра Линия",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Маникюр", 6),
                new DemoMetricTemplate("Педикюр", 5),
                new DemoMetricTemplate("Дизайн ногтей", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Масло для кутикулы", 12),
                new DemoProductTemplate("Крем для рук", 10)
            ]),
        new(
            "Фитнес-клуб Импульс",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Тренировки", 8),
                new DemoMetricTemplate("Групповые занятия", 6),
                new DemoMetricTemplate("Персональные занятия", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Протеиновый батончик", 8),
                new DemoProductTemplate("Гостевой визит", 15),
                new DemoProductTemplate("Фирменная бутылка", 22)
            ]),
        new(
            "Цветочная мастерская Ирис",
            IsMetricsEnabled: false,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Букеты", 5),
                new DemoMetricTemplate("Композиции", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Открытка", 6),
                new DemoProductTemplate("Мини-букет", 20),
                new DemoProductTemplate("Подарочная упаковка", 8)
            ]),
        new(
            "Книжный двор",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Покупки книг", 5),
                new DemoMetricTemplate("Детские книги", 4),
                new DemoMetricTemplate("Предзаказы", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Закладка", 5),
                new DemoProductTemplate("Скидка на книгу", 16),
                new DemoProductTemplate("Подарочный пакет", 7)
            ]),
        new(
            "Пиццерия Круг",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Заказы пиццы", 6),
                new DemoMetricTemplate("Самовывоз", 4),
                new DemoMetricTemplate("Обеды", 5)
            ],
            Products:
            [
                new DemoProductTemplate("Маргарита", 18),
                new DemoProductTemplate("Лимонад", 8),
                new DemoProductTemplate("Соус", 4)
            ]),
        new(
            "Зоомаркет Лапа",
            IsMetricsEnabled: true,
            IsCoinsEnabled: false,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Корм", 5),
                new DemoMetricTemplate("Игрушки", 4),
                new DemoMetricTemplate("Груминг", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Лакомство", 7),
                new DemoProductTemplate("Игрушка", 12)
            ]),
        new(
            "Салон оптики Фокус",
            IsMetricsEnabled: false,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Проверки зрения", 3),
                new DemoMetricTemplate("Покупки линз", 5)
            ],
            Products:
            [
                new DemoProductTemplate("Раствор для линз", 14),
                new DemoProductTemplate("Салфетка для очков", 6),
                new DemoProductTemplate("Футляр", 18)
            ]),
        new(
            "Химчистка Чисто",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Заказы чистки", 5),
                new DemoMetricTemplate("Верхняя одежда", 4),
                new DemoMetricTemplate("Срочные заказы", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Защитный чехол", 9),
                new DemoProductTemplate("Доставка", 12)
            ]),
        new(
            "Автомойка Блеск",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Мойки кузова", 5),
                new DemoMetricTemplate("Комплексные мойки", 4),
                new DemoMetricTemplate("Химчистка салона", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Чернение шин", 8),
                new DemoProductTemplate("Воск", 14),
                new DemoProductTemplate("Комплексная мойка", 25)
            ]),
        new(
            "Косметология Нова",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Процедуры ухода", 5),
                new DemoMetricTemplate("Консультации", 3),
                new DemoMetricTemplate("Курсовые процедуры", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Маска для лица", 12),
                new DemoProductTemplate("Сыворотка", 20),
                new DemoProductTemplate("Скидка на уход", 18)
            ]),
        new(
            "Суши-бар Волна",
            IsMetricsEnabled: false,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Заказы роллов", 5),
                new DemoMetricTemplate("Сеты", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Мисо-суп", 7),
                new DemoProductTemplate("Ролл Калифорния", 16),
                new DemoProductTemplate("Имбирь и васаби", 4)
            ]),
        new(
            "Йога-студия Баланс",
            IsMetricsEnabled: true,
            IsCoinsEnabled: false,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Практики", 7),
                new DemoMetricTemplate("Утренние классы", 5),
                new DemoMetricTemplate("Медитации", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Коврик в аренду", 8),
                new DemoProductTemplate("Чай после практики", 5)
            ]),
        new(
            "Детский центр Росток",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Занятия", 6),
                new DemoMetricTemplate("Мастер-классы", 4),
                new DemoMetricTemplate("Абонементы", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Творческий набор", 12),
                new DemoProductTemplate("Пробное занятие", 20),
                new DemoProductTemplate("Наклейки", 5)
            ]),
        new(
            "Винный бутик Терруар",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Дегустации", 4),
                new DemoMetricTemplate("Покупки бутылок", 6),
                new DemoMetricTemplate("Подборки", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Штопор", 15),
                new DemoProductTemplate("Бокал", 18)
            ]),
        new(
            "Школа английского Речь",
            IsMetricsEnabled: true,
            IsCoinsEnabled: false,
            IsCoinProductRedemptionEnabled: false,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Уроки", 8),
                new DemoMetricTemplate("Разговорные клубы", 5),
                new DemoMetricTemplate("Домашние задания", 6)
            ],
            Products:
            [
                new DemoProductTemplate("Разговорный клуб", 14),
                new DemoProductTemplate("Учебная тетрадь", 8)
            ]),
        new(
            "Фотостудия Кадр",
            IsMetricsEnabled: false,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: false,
            Metrics:
            [
                new DemoMetricTemplate("Съёмки", 4),
                new DemoMetricTemplate("Аренда зала", 3)
            ],
            Products:
            [
                new DemoProductTemplate("Ретушь фото", 12),
                new DemoProductTemplate("Печать снимка", 8),
                new DemoProductTemplate("Дополнительный час", 25)
            ]),
        new(
            "Сырная лавка Молоко",
            IsMetricsEnabled: true,
            IsCoinsEnabled: true,
            IsCoinProductRedemptionEnabled: true,
            IsManualCoinRedemptionEnabled: true,
            Metrics:
            [
                new DemoMetricTemplate("Покупки сыра", 5),
                new DemoMetricTemplate("Дегустации", 3),
                new DemoMetricTemplate("Подарочные наборы", 4)
            ],
            Products:
            [
                new DemoProductTemplate("Сырная тарелка", 18),
                new DemoProductTemplate("Джем", 9),
                new DemoProductTemplate("Подарочная коробка", 12)
            ])
    ];

    public static IReadOnlyCollection<string> CoinIssueComments { get; } =
    [
        "Покупка в магазине",
        "Повторный визит",
        "Бонус за заказ",
        "Акция выходного дня",
        "Подарок постоянному клиенту",
        "Начисление за чек",
        "Участие в акции",
        "Покупка по рекомендации"
    ];

    public static IReadOnlyCollection<string> MetricIssueComments { get; } =
    [
        "Визит в филиал",
        "Покупка по карте",
        "Заказ на кассе",
        "Онлайн-заказ",
        "Повторная покупка",
        "Участие в программе",
        "Плановый визит",
        "Покупка из подборки"
    ];

    public static IReadOnlyCollection<string> MetricRedeemComments { get; } =
    [
        "Получение награды",
        "Списание за подарок",
        "Использование накопления",
        "Обмен на услугу",
        "Списание по акции"
    ];

    public static IReadOnlyCollection<string> CoinBonusComments { get; } =
    [
        "Бонус после покупки",
        "Дополнительное начисление",
        "Персональное предложение",
        "Спасибо за визит",
        "Бонус постоянному клиенту"
    ];
}

public record DemoBrandTemplate(
    string Name,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    IReadOnlyCollection<DemoMetricTemplate> Metrics,
    IReadOnlyCollection<DemoProductTemplate> Products);

public record DemoMetricTemplate(string Name, int RedemptionAmount);

public record DemoProductTemplate(string Name, int Price);
