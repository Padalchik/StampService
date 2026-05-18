import { useEffect, useRef, useState } from 'react';
import { ArrowLeft, BarChart3, ChevronRight, Gift, History, RefreshCw, Ticket, WalletCards } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import {
  getBrandHistory,
  getBrandRewards,
  openUserWallet,
  type UserBrandRewardsResponse,
  type UserBrandWalletHistoryItemResponse,
  type UserBrandWalletHistoryResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletResponse
} from './walletApi';

export function WalletPage() {
  const [wallet, setWallet] = useState<UserWalletResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshingCode, setIsRefreshingCode] = useState(false);
  const [error, setError] = useState('');
  const [selectedBrandId, setSelectedBrandId] = useState<string | null>(null);
  const [brandRewards, setBrandRewards] = useState<UserBrandRewardsResponse | null>(null);
  const [brandHistory, setBrandHistory] = useState<UserBrandWalletHistoryResponse | null>(null);
  const [isBrandLoading, setIsBrandLoading] = useState(false);
  const [brandError, setBrandError] = useState('');
  const hasRequestedInitialWallet = useRef(false);

  useEffect(() => {
    if (hasRequestedInitialWallet.current) {
      return;
    }

    hasRequestedInitialWallet.current = true;
    void loadWallet(false);
  }, []);

  async function loadWallet(forceRefreshCode: boolean) {
    setError('');
    if (forceRefreshCode) {
      setIsRefreshingCode(true);
    } else {
      setIsLoading(true);
    }

    try {
      const response = await openUserWallet(forceRefreshCode);
      setWallet(response);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
      setIsRefreshingCode(false);
    }
  }

  if (isLoading) {
    return (
      <section className="surface-panel">
        <p className="muted-text">Загружаем кошелёк...</p>
      </section>
    );
  }

  if (selectedBrandId) {
    return (
      <BrandDetailsScreen
        rewards={brandRewards}
        history={brandHistory}
        isLoading={isBrandLoading}
        error={brandError}
        onBack={() => {
          setSelectedBrandId(null);
          setBrandRewards(null);
          setBrandHistory(null);
          setBrandError('');
        }}
      />
    );
  }

  return (
    <div className="wallet-page">
      <section className="wallet-code-panel">
        <div className="wallet-code-panel__main">
          <div className="wallet-code-panel__icon" aria-hidden="true">
            <Ticket size={26} />
          </div>
          <div>
            <h2>Код для списания</h2>
            <p>Покажите этот код сотруднику бренда для операции списания.</p>
            {wallet?.customerCode ? (
              <div className="customer-code">Код пользователя: {wallet.customerCode}</div>
            ) : null}
            <div className="redemption-code">{wallet?.redemptionCode.code ?? '----'}</div>
            {wallet?.redemptionCode.expiresAtUtc ? (
              <div className="wallet-code-panel__expires">
                Действует до {formatTime(wallet.redemptionCode.expiresAtUtc)}
              </div>
            ) : null}
          </div>
        </div>
        <button
          className="button-secondary"
          type="button"
          disabled={isRefreshingCode}
          onClick={() => void loadWallet(true)}
        >
          <RefreshCw size={18} />
          Обновить код
        </button>
      </section>

      {error ? <p className="form-status form-status--error">{error}</p> : null}

      <section className="surface-panel">
        <div className="section-heading">
          <WalletCards size={22} />
          <h2>Балансы и доступные награды</h2>
        </div>

        {!wallet || wallet.brands.length === 0 ? (
          <p className="muted-text">У вас пока нет балансов.</p>
        ) : (
          <div className="wallet-brand-list">
            {wallet.brands.map((brand) => (
              <WalletBrandCard
                key={brand.brandId}
                brand={brand}
                isSelected={brand.brandId === selectedBrandId}
                onOpen={() => void loadBrandDetails(brand.brandId)}
              />
            ))}
          </div>
        )}
      </section>

    </div>
  );

  async function loadBrandDetails(brandId: string) {
    setSelectedBrandId(brandId);
    setBrandRewards(null);
    setBrandHistory(null);
    setBrandError('');
    setIsBrandLoading(true);

    try {
      const [rewardsResponse, historyResponse] = await Promise.all([
        getBrandRewards(brandId),
        getBrandHistory(brandId)
      ]);
      setBrandRewards(rewardsResponse);
      setBrandHistory(historyResponse);
    } catch (requestError) {
      setBrandError(getUserMessage(requestError));
    } finally {
      setIsBrandLoading(false);
    }
  }
}

function WalletBrandCard({
  brand,
  isSelected,
  onOpen
}: {
  brand: UserWalletBrandOverviewResponse;
  isSelected: boolean;
  onOpen: () => void;
}) {
  const hasCoinProducts = brand.isCoinsEnabled && brand.availableCoinProducts.length > 0;
  const hasMetrics = brand.isMetricsEnabled && brand.availableMetrics.length > 0;
  const hasRewards = hasCoinProducts || hasMetrics;

  return (
    <article className={`wallet-brand-card ${isSelected ? 'wallet-brand-card--selected' : ''}`}>
      <header className="wallet-brand-card__header">
        <div>
          <h3>{brand.brandName}</h3>
          <div className="wallet-brand-card__toggles">
            {brand.isCoinsEnabled ? <span>Монетки: {brand.coinBalance}</span> : null}
            {brand.isMetricsEnabled ? <span>Метрики включены</span> : null}
          </div>
        </div>
        <button className="button-secondary button-compact" type="button" onClick={onOpen}>
          Подробнее
          <ChevronRight size={17} />
        </button>
      </header>

      {hasCoinProducts ? (
        <div className="reward-block">
          <h4>Доступные товары</h4>
          <ul>
            {brand.availableCoinProducts.map((product) => (
              <li key={product.productId}>
                <span>{product.productName}</span>
                <strong>{product.price} монеток</strong>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {hasMetrics ? (
        <div className="reward-block">
          <h4>Доступные метрики</h4>
          <ul>
            {brand.availableMetrics.map((metric) => (
              <li key={metric.metricDefinitionId}>
                <span>{metric.metricName}</span>
                <strong>
                  {metric.currentBalance}/{metric.requiredAmount}
                </strong>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {!hasRewards ? <p className="muted-text">Доступных наград пока нет.</p> : null}
    </article>
  );
}

function BrandDetailsScreen({
  rewards,
  history,
  isLoading,
  error,
  onBack
}: {
  rewards: UserBrandRewardsResponse | null;
  history: UserBrandWalletHistoryResponse | null;
  isLoading: boolean;
  error: string;
  onBack: () => void;
}) {
  const brandName = rewards?.brandName || history?.brandName || 'Бренд';

  return (
    <div className="brand-detail-page">
      <section className="surface-panel brand-details">
        <div className="brand-details__header">
          <button className="button-secondary button-compact" type="button" onClick={onBack}>
            <ArrowLeft size={17} />
            Назад
          </button>
          <div>
            <h2>{brandName}</h2>
            <p>Доступные награды и история операций.</p>
          </div>
        </div>

        {isLoading ? <p className="muted-text">Загружаем бренд...</p> : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}

        {!isLoading && rewards ? <BrandRewards rewards={rewards} /> : null}
        {!isLoading && history ? <BrandHistory history={history} /> : null}
      </section>
    </div>
  );
}

function BrandRewards({ rewards }: { rewards: UserBrandRewardsResponse }) {
  return (
    <div className="brand-details__grid">
      {rewards.isCoinsEnabled ? (
        <section className="detail-block">
          <div className="detail-block__heading">
            <Gift size={19} />
            <h3>Товары за монетки</h3>
          </div>
          <p className="detail-block__balance">Монетки: {rewards.coinBalance}</p>
          {rewards.isCoinProductRedemptionEnabled && rewards.coinProducts.length > 0 ? (
            <RewardList
              items={rewards.coinProducts.map((product) => ({
                id: product.productId,
                name: product.productName,
                progress: `${product.currentBalance}/${product.price}`,
                status: product.isAvailable ? 'доступно' : `не хватает ${product.missingAmount}`,
                available: product.isAvailable
              }))}
            />
          ) : (
            <p className="muted-text">Пока нет активных товаров.</p>
          )}
        </section>
      ) : null}

      {rewards.isMetricsEnabled ? (
        <section className="detail-block">
          <div className="detail-block__heading">
            <BarChart3 size={19} />
            <h3>Метрики</h3>
          </div>
          {rewards.metrics.length > 0 ? (
            <RewardList
              items={rewards.metrics.map((metric) => ({
                id: metric.metricDefinitionId,
                name: metric.metricName,
                progress: `${metric.currentBalance}/${metric.requiredAmount}`,
                status: metric.isAvailable ? 'доступно' : `не хватает ${metric.missingAmount}`,
                available: metric.isAvailable
              }))}
            />
          ) : (
            <p className="muted-text">Пока нет балансов по метрикам.</p>
          )}
        </section>
      ) : null}

      <p className="brand-details__hint">
        Чтобы получить награду, покажите код для списания сотруднику.
      </p>
    </div>
  );
}

function RewardList({
  items
}: {
  items: Array<{ id: string; name: string; progress: string; status: string; available: boolean }>;
}) {
  return (
    <ul className="detail-list">
      {items.map((item) => (
        <li key={item.id}>
          <span className={item.available ? 'reward-marker reward-marker--available' : 'reward-marker'} />
          <span>{item.name}</span>
          <strong>{item.progress}</strong>
          <em>{item.status}</em>
        </li>
      ))}
    </ul>
  );
}

function BrandHistory({ history }: { history: UserBrandWalletHistoryResponse }) {
  const coinItems = history.items.filter((item) => item.sourceType === 'Coin');
  const metricItems = history.items.filter((item) => item.sourceType === 'Metric');

  return (
    <section className="detail-block detail-block--wide">
      <div className="detail-block__heading">
        <History size={19} />
        <h3>Последние операции</h3>
      </div>

      {history.items.length === 0 ? (
        <p className="muted-text">Истории операций пока нет.</p>
      ) : (
        <div className="history-sections">
          {history.isCoinsEnabled ? <HistorySection title="Монеты" items={coinItems} /> : null}
          {history.isMetricsEnabled ? <HistorySection title="Метрики" items={metricItems} /> : null}
        </div>
      )}
    </section>
  );
}

function HistorySection({
  title,
  items
}: {
  title: string;
  items: UserBrandWalletHistoryItemResponse[];
}) {
  return (
    <div className="history-section">
      <h4>{title}</h4>
      {items.length === 0 ? (
        <p className="muted-text">Операций пока нет.</p>
      ) : (
        <ul className="history-list">
          {items
            .slice()
            .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
            .map((item) => (
              <li key={`${item.sourceType}-${item.sourceName}-${item.createdAt}-${item.amount}`}>
                <span className={item.transactionType === 'Issue' ? 'history-sign history-sign--issue' : 'history-sign'} />
                <div>
                  <strong>
                    {item.transactionType === 'Issue' ? '+' : '-'}
                    {item.amount} {item.sourceName}
                  </strong>
                  <p>
                    {formatDateTime(item.createdAt)}
                    {item.comment && !isAutoComment(item.comment) ? ` - ${item.comment}` : ''}
                  </p>
                </div>
              </li>
            ))}
        </ul>
      )}
    </div>
  );
}

function formatTime(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  }).format(new Date(value));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

function isAutoComment(value: string): boolean {
  return ['Issue metric', 'Redeem metric', 'Issue coins', 'Redeem coins'].includes(value);
}

function getUserMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    return error.message;
  }

  return 'Не удалось загрузить кошелёк.';
}
