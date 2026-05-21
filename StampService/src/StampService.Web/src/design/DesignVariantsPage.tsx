import { CheckCircle2, ChevronRight, Coins, Gift, LayoutDashboard, RefreshCw, Search, Settings2, Stamp, Store, TicketCheck, UserRound } from 'lucide-react';
import { useMemo, useState, type ReactNode } from 'react';

type VariantId = 'console' | 'client' | 'counter' | 'owner';

type DesignVariant = {
  id: VariantId;
  title: string;
  subtitle: string;
  tone: string;
};

const variants: DesignVariant[] = [
  {
    id: 'console',
    title: 'Операционная консоль',
    subtitle: 'Плотный рабочий экран для сотрудников: меньше воздуха, больше данных и быстрых действий.',
    tone: 'Строгий, табличный, B2B'
  },
  {
    id: 'client',
    title: 'Лёгкий кабинет',
    subtitle: 'Спокойный личный кабинет клиента с крупным кодом списания и понятными наградами.',
    tone: 'Чистый, доверительный, клиентский'
  },
  {
    id: 'counter',
    title: 'Кассовый режим',
    subtitle: 'Экран для точки продаж: крупные формы, быстрый поиск клиента, минимум отвлечений.',
    tone: 'Практичный, быстрый, фронт-офис'
  },
  {
    id: 'owner',
    title: 'Панель владельца',
    subtitle: 'Сводка по бренду, настройкам и доступным операциям без декоративной подачи.',
    tone: 'Управленческий, обзорный, спокойный'
  }
];

const recentOperations = [
  { label: 'Начислены монетки', meta: '+120, Анна П.', time: '12:40' },
  { label: 'Списана метрика', meta: 'Кофе 5/5, Иван К.', time: '12:18' },
  { label: 'Выдан товар', meta: 'Десерт за 300', time: '11:52' }
];

export function DesignVariantsPage() {
  const [activeVariantId, setActiveVariantId] = useState<VariantId>('console');
  const activeVariant = useMemo(
    () => variants.find((variant) => variant.id === activeVariantId) ?? variants[0],
    [activeVariantId]
  );

  return (
    <main className="design-preview">
      <section className="design-preview__intro">
        <div>
          <p className="design-preview__eyebrow">StampService Web UI</p>
          <h1>Варианты визуального направления</h1>
          <p>
            Это отдельная страница предпросмотра без API-запросов. Все варианты показывают один и тот же домен:
            кошелёк клиента, рабочие операции бренда и управленческую сводку.
          </p>
        </div>
        <a className="button-link button-link--light" href="/">
          Вернуться в приложение
        </a>
      </section>

      <section className="design-preview__selector" aria-label="Варианты страниц">
        {variants.map((variant) => (
          <button
            className={`design-preview__tab ${activeVariantId === variant.id ? 'design-preview__tab--active' : ''}`}
            key={variant.id}
            type="button"
            onClick={() => setActiveVariantId(variant.id)}
          >
            <strong>{variant.title}</strong>
            <span>{variant.tone}</span>
          </button>
        ))}
      </section>

      <section className={`design-stage design-stage--${activeVariant.id}`}>
        <div className="design-stage__header">
          <div>
            <h2>{activeVariant.title}</h2>
            <p>{activeVariant.subtitle}</p>
          </div>
          <span>{activeVariant.tone}</span>
        </div>

        {activeVariant.id === 'console' ? <ConsoleVariant /> : null}
        {activeVariant.id === 'client' ? <ClientVariant /> : null}
        {activeVariant.id === 'counter' ? <CounterVariant /> : null}
        {activeVariant.id === 'owner' ? <OwnerVariant /> : null}
      </section>
    </main>
  );
}

function ConsoleVariant() {
  return (
    <div className="variant-shell variant-shell--console">
      <aside className="variant-rail">
        <div className="variant-logo">SS</div>
        <button type="button" aria-label="Рабочий стол">
          <LayoutDashboard size={18} />
        </button>
        <button type="button" aria-label="Бренды">
          <Store size={18} />
        </button>
        <button type="button" aria-label="Настройки">
          <Settings2 size={18} />
        </button>
      </aside>

      <div className="variant-workspace">
        <header className="variant-topbar">
          <div>
            <h3>Рабочие бренды</h3>
            <span>Кофейня «Север» · роль OWNER</span>
          </div>
          <button type="button" className="button-secondary button-compact">
            <RefreshCw size={16} />
            Обновить
          </button>
        </header>

        <div className="console-grid">
          <section className="console-main">
            <div className="toolbar-row">
              <button type="button">Начислить</button>
              <button type="button" className="button-secondary">
                Списать
              </button>
              <button type="button" className="button-secondary">
                Выдать товар
              </button>
            </div>

            <div className="console-form">
              <label>
                Телефон клиента
                <input value="+7 (999) 123-45-67" readOnly />
              </label>
              <label>
                Операция
                <select defaultValue="coins" disabled>
                  <option value="coins">Начислить монетки</option>
                </select>
              </label>
              <label>
                Количество
                <input value="120" readOnly />
              </label>
              <button type="button">
                <Coins size={17} />
                Выполнить
              </button>
            </div>
          </section>

          <section className="console-side">
            <MetricLine label="Монетки" value="1 240" />
            <MetricLine label="Визиты" value="4 из 5" />
            <MetricLine label="Доступно наград" value="2" />
          </section>
        </div>

        <OperationTable />
      </div>
    </div>
  );
}

function ClientVariant() {
  return (
    <div className="variant-shell variant-shell--client">
      <header className="client-header">
        <div>
          <span>Мой кошелёк</span>
          <h3>Анна Петрова</h3>
        </div>
        <button type="button" className="button-secondary button-compact">
          <UserRound size={16} />
          Профиль
        </button>
      </header>

      <section className="client-code">
        <div>
          <p>Код для списания</p>
          <strong>4821</strong>
          <span>Действует до 13:05</span>
        </div>
        <TicketCheck size={48} />
      </section>

      <div className="client-grid">
        <RewardCard icon={<Stamp size={20} />} title="Кофе в подарок" text="Доступно после одного визита" value="4/5" />
        <RewardCard icon={<Gift size={20} />} title="Десерт" text="Можно получить сейчас" value="Доступно" />
        <RewardCard icon={<Coins size={20} />} title="Монетки" text="Баланс в кофейне «Север»" value="1 240" />
      </div>
    </div>
  );
}

function CounterVariant() {
  return (
    <div className="variant-shell variant-shell--counter">
      <header className="counter-header">
        <div>
          <h3>Кассовый режим</h3>
          <span>Быстрые операции с клиентом</span>
        </div>
        <span className="counter-status">Смена открыта</span>
      </header>

      <section className="counter-search">
        <Search size={22} />
        <input value="+7 (999) 123-45-67" readOnly />
        <button type="button">Найти</button>
      </section>

      <div className="counter-actions">
        <ActionTile icon={<Coins size={24} />} title="Начислить монетки" text="120 монеток за покупку" />
        <ActionTile icon={<Stamp size={24} />} title="Поставить визит" text="Метрика «Кофе»" />
        <ActionTile icon={<Gift size={24} />} title="Выдать награду" text="Доступные товары" />
      </div>

      <section className="counter-client">
        <div>
          <span>Клиент</span>
          <strong>Анна Петрова</strong>
        </div>
        <div>
          <span>Код списания</span>
          <strong>4821</strong>
        </div>
        <div>
          <span>Баланс</span>
          <strong>1 240</strong>
        </div>
      </section>
    </div>
  );
}

function OwnerVariant() {
  return (
    <div className="variant-shell variant-shell--owner">
      <header className="owner-header">
        <div>
          <span>Панель владельца</span>
          <h3>Кофейня «Север»</h3>
        </div>
        <button type="button">
          <Store size={17} />
          Управлять брендом
        </button>
      </header>

      <div className="owner-stats">
        <MetricLine label="Операций сегодня" value="86" />
        <MetricLine label="Выдано монеток" value="12 480" />
        <MetricLine label="Списаний" value="19" />
        <MetricLine label="Сотрудников" value="4" />
      </div>

      <div className="owner-layout">
        <section className="owner-settings">
          <h4>Включённые сценарии</h4>
          {['Метрики лояльности', 'Монетки', 'Товары за монетки', 'Ручное списание'].map((item) => (
            <div className="owner-setting-row" key={item}>
              <CheckCircle2 size={18} />
              <span>{item}</span>
            </div>
          ))}
        </section>
        <section className="owner-feed">
          <h4>Последние операции</h4>
          {recentOperations.map((operation) => (
            <div className="owner-feed-row" key={operation.label}>
              <div>
                <strong>{operation.label}</strong>
                <span>{operation.meta}</span>
              </div>
              <em>{operation.time}</em>
            </div>
          ))}
        </section>
      </div>
    </div>
  );
}

function MetricLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric-line">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function OperationTable() {
  return (
    <section className="operation-table">
      <div className="operation-table__head">
        <span>Операция</span>
        <span>Детали</span>
        <span>Время</span>
      </div>
      {recentOperations.map((operation) => (
        <div className="operation-table__row" key={operation.label}>
          <strong>{operation.label}</strong>
          <span>{operation.meta}</span>
          <em>{operation.time}</em>
        </div>
      ))}
    </section>
  );
}

function RewardCard({ icon, title, text, value }: { icon: ReactNode; title: string; text: string; value: string }) {
  return (
    <article className="reward-card-preview">
      <div className="reward-card-preview__icon">{icon}</div>
      <div>
        <h4>{title}</h4>
        <p>{text}</p>
      </div>
      <strong>{value}</strong>
    </article>
  );
}

function ActionTile({ icon, title, text }: { icon: ReactNode; title: string; text: string }) {
  return (
    <button className="action-tile" type="button">
      {icon}
      <span>
        <strong>{title}</strong>
        <em>{text}</em>
      </span>
      <ChevronRight size={18} />
    </button>
  );
}
