import { ArrowLeft } from 'lucide-react';
import { RedemptionCodeCard } from './RedemptionCodeCard';
import { WalletBrandDetailsBlock, findWalletBrandRewardSection } from './WalletBrandDetailsBlock';
import {
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandRewardSectionResponse
} from './walletApi';

export function BrandWalletPage({
  details,
  isLoading,
  error,
  redemptionCode,
  isRefreshingCode,
  codeRefreshError,
  onRefreshCode,
  onBack
}: {
  details: UserWalletBrandDetailsResponse | null;
  isLoading: boolean;
  error: string;
  redemptionCode?: string;
  isRefreshingCode: boolean;
  codeRefreshError: string;
  onRefreshCode: () => void;
  onBack: () => void;
}) {
  const brandName = details?.brandName || 'Бренд';
  const productsSection = details ? findWalletBrandRewardSection(details.rewardSections, 'products') : null;
  const metricsSection = details ? findWalletBrandRewardSection(details.rewardSections, 'metrics') : null;
  const metaText = details ? getBrandDetailsMetaText(details, productsSection, metricsSection) : 'Загрузка данных';

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

      <RedemptionCodeCard
        code={redemptionCode}
        isRefreshing={isRefreshingCode}
        error={codeRefreshError}
        onRefresh={onRefreshCode}
      />

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
        <WalletBrandDetailsBlock details={details} ariaLabel="Разделы бренда" />
      ) : null}
    </div>
  );
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
