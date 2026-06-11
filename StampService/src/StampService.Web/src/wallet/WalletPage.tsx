import { useEffect, useMemo, useRef, useState } from 'react';
import { ArrowLeft, RefreshCw, X } from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import { formatRuDateTime } from '../format/dateTime';
import {
  getBrandDetails,
  openUserWallet,
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandHistoryItemDetailsResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletBrandRewardItemResponse,
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
  const [activeTab, setActiveTab] = useState<BrandDetailsTab>('metrics');
  const [isProductsExpanded, setIsProductsExpanded] = useState(false);
  const [isMetricsExpanded, setIsMetricsExpanded] = useState(false);
  const [selectedHistorySource, setSelectedHistorySource] = useState<HistorySource | null>(null);
  const brandName = details?.brandName || 'Бренд';
  const productsSection = details ? findRewardSection(details.rewardSections, 'products') : null;
  const metricsSection = details ? findRewardSection(details.rewardSections, 'metrics') : null;
  const products = productsSection ? sortRewardItems(productsSection.items) : [];
  const metrics = metricsSection ? sortRewardItems(metricsSection.items) : [];
  const historySources = details ? getHistorySources(details.history.groups.flatMap((group) => group.items)) : [];
  const metaText = details ? getBrandDetailsMetaText(details, productsSection, metricsSection) : 'Загрузка данных';
  const availableTabs = useMemo(
    () => getAvailableBrandDetailsTabs(details, metricsSection),
    [details, metricsSection]
  );
  const selectedTab = availableTabs.some((tab) => tab.id === activeTab) ? activeTab : availableTabs[0].id;

  useEffect(() => {
    if (!details || availableTabs.some((tab) => tab.id === activeTab)) {
      return;
    }

    setActiveTab(availableTabs[0].id);
  }, [activeTab, availableTabs, details]);

  return (
    <div className="brand-detail-page">
      <div className="brand-details-topline">
        <button className="button-secondary button-compact" type="button" onClick={onBack}>
          <ArrowLeft size={17} />
          Назад
        </button>
      </div>

      <section className="brand-details-hero">
        <h2>{brandName}</h2>
        <p>{metaText}</p>
      </section>

      {isLoading ? (
        <section className="brand-details-content">
          <p className="muted-text">Загружаем бренд...</p>
        </section>
      ) : null}

      {error ? (
        <section className="brand-details-content">
          <p className="form-status form-status--error">{error}</p>
        </section>
      ) : null}

      {!isLoading && details ? (
        <>
          <section className="brand-details-tabs-card">
            <div className="brand-details-tabs" role="tablist" aria-label="Разделы бренда">
              {availableTabs.map((tab) => (
                <button
                  className={selectedTab === tab.id ? 'brand-details-tabs__item brand-details-tabs__item--active' : 'brand-details-tabs__item'}
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={selectedTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </section>

          <section className="brand-details-content">
            {selectedTab === 'coins' ? (
              <CoinsTab
                balance={details.coinBalance}
                products={details.isCoinProductRedemptionEnabled && productsSection ? products : null}
                productsEmptyText={productsSection?.emptyText || 'Пока нет активных товаров.'}
                isProductsExpanded={isProductsExpanded}
                onToggleProductsExpanded={() => setIsProductsExpanded((value) => !value)}
              />
            ) : null}

            {selectedTab === 'metrics' ? (
              <RewardTab
                emptyText="Пока нет штампов"
                items={metrics}
                isExpanded={isMetricsExpanded}
                onToggleExpanded={() => setIsMetricsExpanded((value) => !value)}
              />
            ) : null}

            {selectedTab === 'history' ? (
              <HistoryTab
                emptyText={details.history.emptyText || 'Пока нет операций'}
                sources={historySources}
                onSelectSource={setSelectedHistorySource}
              />
            ) : null}
          </section>
        </>
      ) : null}

      {selectedHistorySource ? (
        <HistoryBottomSheet source={selectedHistorySource} onClose={() => setSelectedHistorySource(null)} />
      ) : null}
    </div>
  );
}

type BrandDetailsTab = 'metrics' | 'coins' | 'history';

type BrandDetailsTabItem = {
  id: BrandDetailsTab;
  label: string;
};

function getAvailableBrandDetailsTabs(
  details: UserWalletBrandDetailsResponse | null,
  metricsSection: UserWalletBrandRewardSectionResponse | null
): BrandDetailsTabItem[] {
  const tabs: BrandDetailsTabItem[] = [];

  if (details?.isMetricsEnabled && metricsSection) {
    tabs.push({ id: 'metrics', label: 'Штампы' });
  }

  if (details?.isCoinsEnabled) {
    tabs.push({ id: 'coins', label: 'Монетки' });
  }

  tabs.push({ id: 'history', label: 'История' });

  return tabs;
}

function CoinsTab({
  balance,
  products,
  productsEmptyText,
  isProductsExpanded,
  onToggleProductsExpanded
}: {
  balance: number;
  products: UserWalletBrandRewardItemResponse[] | null;
  productsEmptyText: string;
  isProductsExpanded: boolean;
  onToggleProductsExpanded: () => void;
}) {
  return (
    <div className="brand-coins-tab">
      <article className="brand-coin-balance-card">
        <span>Баланс</span>
        <strong>{formatCoinCount(balance)}</strong>
      </article>

      {products ? (
        <RewardTab
          emptyText={productsEmptyText}
          items={products}
          isExpanded={isProductsExpanded}
          onToggleExpanded={onToggleProductsExpanded}
        />
      ) : null}
    </div>
  );
}

type RewardTabProps = {
  emptyText: string;
  items: UserWalletBrandRewardItemResponse[];
  isExpanded: boolean;
  onToggleExpanded: () => void;
};

function RewardTab({ emptyText, items, isExpanded, onToggleExpanded }: RewardTabProps) {
  const visibleLimit = 3;
  const visibleItems = isExpanded ? items : items.slice(0, visibleLimit);
  const hiddenCount = items.length - visibleItems.length;

  if (items.length === 0) {
    return <p className="brand-details-empty">{emptyText}</p>;
  }

  return (
    <div className="brand-reward-list">
      {visibleItems.map((item) => (
        <RewardCard key={item.itemId} item={item} />
      ))}

      {hiddenCount > 0 || isExpanded ? (
        <button className="button-secondary brand-details-more" type="button" onClick={onToggleExpanded}>
          {isExpanded ? 'Свернуть' : hiddenCount <= visibleLimit ? `Показать ещё ${hiddenCount}` : 'Показать все'}
        </button>
      ) : null}
    </div>
  );
}

function RewardCard({ item }: { item: UserWalletBrandRewardItemResponse }) {
  const progress = getProgress(item.progressText);
  const showProgress = !item.isAvailable && progress !== null;
  const progressText = getRewardProgressText(item.progressText);

  return (
    <article className={item.isAvailable ? 'brand-reward-card brand-reward-card--available' : 'brand-reward-card'}>
      <div className="brand-reward-card__top">
        <h3>{item.name}</h3>
        <span className={item.isAvailable ? 'status-pill status-pill--ok' : 'status-pill status-pill--warning'}>
          {getRewardStatusText(item)}
        </span>
      </div>

      {progressText ? <p className="brand-reward-card__progress-text">{progressText}</p> : null}

      {showProgress ? (
        <progress className="brand-reward-progress" value={progress.value} max={progress.max}>
          {progressText}
        </progress>
      ) : null}
    </article>
  );
}

type HistorySource = {
  key: string;
  title: string;
  items: UserWalletBrandHistoryItemDetailsResponse[];
};

function HistoryTab({
  emptyText,
  sources,
  onSelectSource
}: {
  emptyText: string;
  sources: HistorySource[];
  onSelectSource: (source: HistorySource) => void;
}) {
  if (sources.length === 0) {
    return <p className="brand-details-empty">{emptyText}</p>;
  }

  return (
    <div className="brand-history-sources">
      {sources.map((source) => (
        <button className="brand-history-source" key={source.key} type="button" onClick={() => onSelectSource(source)}>
          <span>{source.title}</span>
          <strong>{formatOperationCount(source.items.length)}</strong>
        </button>
      ))}
    </div>
  );
}

function HistoryBottomSheet({ source, onClose }: { source: HistorySource; onClose: () => void }) {
  return (
    <div className="brand-history-sheet" role="dialog" aria-modal="true" aria-labelledby="brand-history-sheet-title">
      <button className="brand-history-sheet__backdrop" type="button" aria-label="Закрыть историю" onClick={onClose} />
      <section className="brand-history-sheet__panel">
        <div className="brand-history-sheet__handle" aria-hidden="true" />
        <header className="brand-history-sheet__header">
          <h3 id="brand-history-sheet-title">{source.title}</h3>
          <button className="button-secondary button-compact brand-history-sheet__close" type="button" onClick={onClose}>
            <X size={16} />
            Закрыть
          </button>
        </header>

        {source.items.length === 0 ? (
          <p className="muted-text">По этому элементу пока нет операций</p>
        ) : (
          <ul className="brand-history-list">
            {source.items.map((item) => (
              <li key={`${source.key}-${item.createdAt}-${item.amount}-${item.amountText}`}>
                <strong>{item.amountText}</strong>
                <span>{formatRuDateTime(item.createdAt)}</span>
                {item.hasVisibleComment && item.comment ? <p>{item.comment}</p> : null}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function findRewardSection(
  sections: UserWalletBrandRewardSectionResponse[],
  target: 'products' | 'metrics'
): UserWalletBrandRewardSectionResponse | null {
  const exactKind = target === 'products' ? 'CoinProducts' : 'Metrics';
  const exactMatch = sections.find((section) => section.kind === exactKind);

  if (exactMatch) {
    return exactMatch;
  }

  return (
    sections.find((section) => {
      const kind = section.kind.toLocaleLowerCase('ru-RU');
      const title = section.title.toLocaleLowerCase('ru-RU');

      if (target === 'products') {
        return kind.includes('coin') || title.includes('товар') || title.includes('монет');
      }

      return kind.includes('metric') || title.includes('штамп');
    }) ?? null
  );
}

function sortRewardItems(items: UserWalletBrandRewardItemResponse[]): UserWalletBrandRewardItemResponse[] {
  return [...items].sort((left, right) => {
    if (left.isAvailable !== right.isAvailable) {
      return Number(right.isAvailable) - Number(left.isAvailable);
    }

    if (!left.isAvailable && !right.isAvailable) {
      const leftProgress = getProgress(left.progressText);
      const rightProgress = getProgress(right.progressText);

      if (leftProgress && rightProgress) {
        const leftMissing = leftProgress.max - leftProgress.value;
        const rightMissing = rightProgress.max - rightProgress.value;

        if (leftMissing !== rightMissing) {
          return leftMissing - rightMissing;
        }
      }
    }

    return 0;
  });
}

function getRewardStatusText(item: UserWalletBrandRewardItemResponse): string {
  if (item.isAvailable) {
    return 'доступно';
  }

  if (item.statusText.toLocaleLowerCase('ru-RU').includes('не хватает')) {
    return item.statusText;
  }

  return item.statusText || 'недоступно';
}

function getProgress(progressText: string): { value: number; max: number } | null {
  const match = progressText.match(/^\s*(\d+)\s*(?:\/|из)\s*(\d+)/i);

  if (!match) {
    return null;
  }

  const value = Number(match[1]);
  const max = Number(match[2]);

  if (!Number.isFinite(value) || !Number.isFinite(max) || max <= 0) {
    return null;
  }

  return { value: Math.min(value, max), max };
}

function getRewardProgressText(progressText: string): string {
  const match = progressText.match(/^\s*(\d+)\s*\/\s*(\d+)\s*$/);

  if (!match) {
    return progressText;
  }

  return `${match[1]} из ${match[2]}`;
}

function getHistorySources(items: UserWalletBrandHistoryItemDetailsResponse[]): HistorySource[] {
  const sources = new Map<string, HistorySource>();

  for (const item of items) {
    const key = `${item.sourceType}::${item.sourceName}`;
    const title = item.sourceType === 'Coin' ? 'Монетки' : item.sourceName || 'История';
    const source = sources.get(key);

    if (source) {
      source.items.push(item);
    } else {
      sources.set(key, {
        key,
        title,
        items: [item]
      });
    }
  }

  return Array.from(sources.values());
}

function getBrandDetailsMetaText(
  details: UserWalletBrandDetailsResponse,
  productsSection: UserWalletBrandRewardSectionResponse | null,
  metricsSection: UserWalletBrandRewardSectionResponse | null
): string {
  const availableCount = details.rewardSections.reduce(
    (count, section) => count + section.items.filter((item) => item.isAvailable).length,
    0
  );
  const balanceText = details.isCoinsEnabled
    ? formatCoinCount(details.coinBalance)
    : productsSection?.balanceText || metricsSection?.balanceText || '';
  const rewardText = availableCount > 0 ? formatRewardCount(availableCount) : 'пока нет доступных наград';

  return [balanceText, rewardText].filter(Boolean).join(' · ');
}

function formatOperationCount(count: number): string {
  return `${count} ${getRuPlural(count, 'операция', 'операции', 'операций')}`;
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось загрузить кошелёк.');
}
