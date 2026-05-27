import { useEffect, useRef, useState } from 'react';
import { ArrowLeft, BarChart3, Gift, History, RefreshCw } from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import { formatRuDateTime } from '../format/dateTime';
import {
  getBrandDetails,
  openUserWallet,
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandHistoryGroupResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletBrandRewardSectionResponse,
  type UserWalletResponse
} from './walletApi';

export function WalletPage({ homeNavigationKey }: { homeNavigationKey: number }) {
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

  useEffect(() => {
    setSelectedBrandId(null);
    setBrandDetails(null);
    setBrandError('');
    setIsBrandLoading(false);
  }, [homeNavigationKey]);

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
      <div className="wallet-layout">
        <section className="wallet-code-card wallet-sticky-code" aria-label="Код для списания">
          <div className="wallet-code-card__content">
            <span className="wallet-code-card__label">Код для списания</span>
            <div className="wallet-code-card__row">
              <div className="redemption-code wallet-code-card__code">{wallet?.redemptionCode.code ?? '----'}</div>
              <button
                className="wallet-code-card__refresh"
                type="button"
                aria-label="Обновить код"
                disabled={isRefreshingCode}
                onClick={() => void loadWallet(true)}
              >
                <RefreshCw size={20} aria-hidden="true" />
              </button>
            </div>
          </div>

          {error ? <p className="form-status form-status--error">{error}</p> : null}
        </section>

        <section className="wallet-brands-panel">
          <div className="section-heading section-heading--wallet">
            <h2>Мои бренды</h2>
            {wallet ? <span>{formatBrandCount(wallet.brands.length)}</span> : null}
          </div>

          {!wallet || wallet.brands.length === 0 ? (
            <p className="muted-text">У вас пока нет брендов.</p>
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
  const rewards = getVisibleRewards(brand);
  const visibleRewards = rewards.slice(0, 3);
  const hiddenRewardCount = rewards.length - visibleRewards.length;
  const metaText = getBrandMetaText(brand, rewards.length);

  return (
    <article className={`wallet-brand-card ${isSelected ? 'wallet-brand-card--selected' : ''}`}>
      <header className="wallet-brand-card__top">
        <div className="wallet-brand-card__title">
          <h3>{brand.brandName}</h3>
          <p className="wallet-brand-card__meta">{metaText}</p>
        </div>
        <button className="wallet-brand-card__open" type="button" onClick={onOpen}>
          Открыть
        </button>
      </header>

      {visibleRewards.length > 0 ? (
        <div className="wallet-reward-chips" aria-label="Доступные награды">
          {visibleRewards.map((reward) => (
            <span className="wallet-reward-chip" key={reward.key}>
              {reward.name}
            </span>
          ))}
          {hiddenRewardCount > 0 ? (
            <span className="wallet-reward-chip wallet-reward-chip--more">+{hiddenRewardCount} ещё</span>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}

function getVisibleRewards(brand: UserWalletBrandOverviewResponse): Array<{ key: string; name: string }> {
  const coinProductRewards = brand.isCoinsEnabled
    ? brand.availableCoinProducts
        .filter((product) => product.isAvailable)
        .map((product) => ({
          key: `coin-${product.productId}`,
          name: product.productName
        }))
    : [];

  const metricRewards = brand.isMetricsEnabled
    ? brand.availableMetrics
        .filter((metric) => metric.isAvailable)
        .map((metric) => ({
          key: `metric-${metric.metricDefinitionId}`,
          name: metric.metricName
        }))
    : [];

  return [...coinProductRewards, ...metricRewards];
}

function getBrandMetaText(brand: UserWalletBrandOverviewResponse, rewardCount: number): string {
  if (brand.isCoinsEnabled) {
    return formatCoinCount(brand.coinBalance);
  }

  if (rewardCount > 0) {
    return formatRewardCount(rewardCount);
  }

  return 'Пока нет доступных наград';
}

function formatBrandCount(count: number): string {
  return `${count} ${getRuPlural(count, 'бренд', 'бренда', 'брендов')}`;
}

function formatCoinCount(count: number): string {
  return `${count} ${getRuPlural(count, 'монета', 'монеты', 'монет')}`;
}

function formatRewardCount(count: number): string {
  return `${count} ${getRuPlural(count, 'доступная награда', 'доступные награды', 'доступных наград')}`;
}

function getRuPlural(count: number, one: string, few: string, many: string): string {
  const absoluteCount = Math.abs(count);
  const lastTwoDigits = absoluteCount % 100;
  const lastDigit = absoluteCount % 10;

  if (lastTwoDigits >= 11 && lastTwoDigits <= 14) {
    return many;
  }

  if (lastDigit === 1) {
    return one;
  }

  if (lastDigit >= 2 && lastDigit <= 4) {
    return few;
  }

  return many;
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
                  {formatRuDateTime(item.createdAt)}
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

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось загрузить кошелёк.');
}
