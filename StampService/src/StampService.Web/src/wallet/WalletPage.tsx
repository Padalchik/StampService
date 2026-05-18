import { useEffect, useRef, useState } from 'react';
import { ArrowLeft, BarChart3, ChevronRight, Gift, History, RefreshCw, Ticket, WalletCards } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import {
  getBrandDetails,
  openUserWallet,
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandHistoryGroupResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletBrandRewardSectionResponse,
  type UserWalletResponse
} from './walletApi';

export function WalletPage() {
  const [wallet, setWallet] = useState<UserWalletResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshingCode, setIsRefreshingCode] = useState(false);
  const [error, setError] = useState('');
  const [selectedBrandId, setSelectedBrandId] = useState<string | null>(null);
  const [brandDetails, setBrandDetails] = useState<UserWalletBrandDetailsResponse | null>(null);
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
        details={brandDetails}
        isLoading={isBrandLoading}
        error={brandError}
        onBack={() => {
          setSelectedBrandId(null);
          setBrandDetails(null);
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
    setBrandDetails(null);
    setBrandError('');
    setIsBrandLoading(true);

    try {
      const response = await getBrandDetails(brandId);
      setBrandDetails(response);
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
  details,
  isLoading,
  error,
  onBack
}: {
  details: UserWalletBrandDetailsResponse | null;
  isLoading: boolean;
  error: string;
  onBack: () => void;
}) {
  const brandName = details?.brandName || 'Бренд';

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

        {!isLoading && details ? <BrandRewards details={details} /> : null}
        {!isLoading && details ? <BrandHistory history={details.history} /> : null}
      </section>
    </div>
  );
}

function BrandRewards({ details }: { details: UserWalletBrandDetailsResponse }) {
  return (
    <div className="brand-details__grid">
      {details.rewardSections.map((section) => (
        <RewardSection key={section.kind} section={section} />
      ))}

      <p className="brand-details__hint">{details.hintText}</p>
    </div>
  );
}

function RewardSection({ section }: { section: UserWalletBrandRewardSectionResponse }) {
  const Icon = section.kind === 'Metrics' ? BarChart3 : Gift;

  return (
    <section className="detail-block">
      <div className="detail-block__heading">
        <Icon size={19} />
        <h3>{section.title}</h3>
      </div>
      {section.balanceText ? <p className="detail-block__balance">{section.balanceText}</p> : null}
      {section.items.length > 0 ? (
        <ul className="detail-list">
          {section.items.map((item) => (
            <li key={item.itemId}>
              <span className={item.isAvailable ? 'reward-marker reward-marker--available' : 'reward-marker'} />
              <span>{item.name}</span>
              <strong>{item.progressText}</strong>
              <em>{item.statusText}</em>
            </li>
          ))}
        </ul>
      ) : (
        <p className="muted-text">{section.emptyText}</p>
      )}
    </section>
  );
}

function BrandHistory({ history }: { history: UserWalletBrandDetailsResponse['history'] }) {
  const hasItems = history.groups.some((group) => group.items.length > 0);

  return (
    <section className="detail-block detail-block--wide">
      <div className="detail-block__heading">
        <History size={19} />
        <h3>{history.title}</h3>
      </div>

      {!hasItems ? (
        <p className="muted-text">{history.emptyText}</p>
      ) : (
        <div className="history-sections">
          {history.groups.map((group) => (
            <HistorySection key={group.kind} group={group} />
          ))}
        </div>
      )}
    </section>
  );
}

function HistorySection({ group }: { group: UserWalletBrandHistoryGroupResponse }) {
  return (
    <div className="history-section">
      <h4>{group.title}</h4>
      {group.items.length === 0 ? (
        <p className="muted-text">{group.emptyText}</p>
      ) : (
        <ul className="history-list">
          {group.items.map((item) => (
            <li key={`${item.sourceType}-${item.sourceName}-${item.createdAt}-${item.amount}`}>
              <span className={item.transactionType === 'Issue' ? 'history-sign history-sign--issue' : 'history-sign'} />
              <div>
                <strong>{item.amountText}</strong>
                <p>
                  {formatDateTime(item.createdAt)}
                  {item.hasVisibleComment && item.comment ? ` - ${item.comment}` : ''}
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

function getUserMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    return error.message;
  }

  return 'Не удалось загрузить кошелёк.';
}
