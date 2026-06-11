import { useEffect, useMemo, useState } from 'react';
import { X } from 'lucide-react';
import { formatRuDateTime } from '../format/dateTime';
import {
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandHistoryItemDetailsResponse,
  type UserWalletBrandRewardItemResponse,
  type UserWalletBrandRewardSectionResponse
} from './walletApi';

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

export function WalletBrandDetailsBlock({
  details,
  ariaLabel = 'Разделы бренда'
}: {
  details: UserWalletBrandDetailsResponse;
  ariaLabel?: string;
}) {
  const [activeTab, setActiveTab] = useState<BrandDetailsTab>('metrics');
  const [isProductsExpanded, setIsProductsExpanded] = useState(false);
  const [isMetricsExpanded, setIsMetricsExpanded] = useState(false);
  const [selectedHistorySource, setSelectedHistorySource] = useState<HistorySource | null>(null);
  const productsSection = findWalletBrandRewardSection(details.rewardSections, 'products');
  const metricsSection = findWalletBrandRewardSection(details.rewardSections, 'metrics');
  const products = productsSection ? sortRewardItems(productsSection.items) : [];
  const metrics = metricsSection ? sortRewardItems(metricsSection.items) : [];
  const historySources = getHistorySources(details.history.groups.flatMap((group) => group.items));
  const availableTabs = useMemo(
    () => getAvailableBrandDetailsTabs(details, metricsSection),
    [details, metricsSection]
  );
  const selectedTab = availableTabs.some((tab) => tab.id === activeTab) ? activeTab : availableTabs[0].id;

  useEffect(() => {
    if (availableTabs.some((tab) => tab.id === activeTab)) {
      return;
    }

    setActiveTab(availableTabs[0].id);
  }, [activeTab, availableTabs]);

  return (
    <>
      <section className="brand-details-tabs-card">
        <div className="brand-details-tabs" role="tablist" aria-label={ariaLabel}>
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

      {selectedHistorySource ? (
        <HistoryBottomSheet source={selectedHistorySource} onClose={() => setSelectedHistorySource(null)} />
      ) : null}
    </>
  );
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

export function findWalletBrandRewardSection(
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
    const leftRank = getRewardSortRank(left);
    const rightRank = getRewardSortRank(right);

    if (leftRank !== rightRank) {
      return leftRank - rightRank;
    }

    if (leftRank === 1 && rightRank === 1) {
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

function getRewardSortRank(item: UserWalletBrandRewardItemResponse): number {
  if (item.isAvailable) {
    return 0;
  }

  const progress = getProgress(item.progressText);
  if (progress && progress.value > 0) {
    return 1;
  }

  return 2;
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

function formatCoinCount(count: number): string {
  return `${count} ${getRuPlural(count, 'монета', 'монеты', 'монет')}`;
}

function formatOperationCount(count: number): string {
  return `${count} ${getRuPlural(count, 'операция', 'операции', 'операций')}`;
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
