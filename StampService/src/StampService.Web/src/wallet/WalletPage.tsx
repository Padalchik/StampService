import { useEffect, useRef, useState } from 'react';
import { getApiErrorMessage } from '../api/errorMessages';
import { BrandWalletPage } from './BrandWalletPage';
import { RedemptionCodeCard } from './RedemptionCodeCard';
import {
  getCurrentRedemptionCode,
  getBrandDetails,
  openUserWallet,
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletResponse
} from './walletApi';

const redemptionCodeRefreshIntervalMs = 5000;

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
  const hasLoadedWallet = useRef(false);
  const isCodeRefreshInFlight = useRef(false);

  useEffect(() => {
    if (hasRequestedInitialWallet.current) {
      return;
    }

    hasRequestedInitialWallet.current = true;
    void loadWallet(false);
  }, []);

  useEffect(() => {
    function refreshIfVisible() {
      if (document.visibilityState === 'visible') {
        void refreshRedemptionCode({ silent: true });
      }
    }

    const intervalId = window.setInterval(refreshIfVisible, redemptionCodeRefreshIntervalMs);

    document.addEventListener('visibilitychange', refreshIfVisible);
    window.addEventListener('focus', refreshIfVisible);

    return () => {
      window.clearInterval(intervalId);
      document.removeEventListener('visibilitychange', refreshIfVisible);
      window.removeEventListener('focus', refreshIfVisible);
    };
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
      hasLoadedWallet.current = true;
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
      setIsRefreshingCode(false);
    }
  }

  async function refreshRedemptionCode({
    forceRefreshCode = false,
    silent = false
  }: {
    forceRefreshCode?: boolean;
    silent?: boolean;
  } = {}) {
    if (!hasLoadedWallet.current) {
      return;
    }

    if (isCodeRefreshInFlight.current) {
      return;
    }

    isCodeRefreshInFlight.current = true;
    if (!silent) {
      setError('');
      setIsRefreshingCode(true);
    }

    try {
      const redemptionCode = await getCurrentRedemptionCode(forceRefreshCode);
      setWallet((currentWallet) =>
        currentWallet
          ? {
              ...currentWallet,
              redemptionCode
            }
          : currentWallet
      );
    } catch (requestError) {
      if (!silent) {
        setError(getUserMessage(requestError));
      }
    } finally {
      isCodeRefreshInFlight.current = false;
      if (!silent) {
        setIsRefreshingCode(false);
      }
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
      <BrandWalletPage
        details={brandDetails}
        isLoading={isBrandLoading}
        error={brandError}
        redemptionCode={wallet?.redemptionCode.code}
        isRefreshingCode={isRefreshingCode}
        codeRefreshError={error}
        onRefreshCode={() => void refreshRedemptionCode({ forceRefreshCode: true })}
      />
    );
  }

  return (
    <div className="wallet-page">
      <div className="wallet-layout">
        <RedemptionCodeCard
          code={wallet?.redemptionCode.code}
          isRefreshing={isRefreshingCode}
          error={error}
          className="wallet-sticky-code"
          onRefresh={() => void refreshRedemptionCode({ forceRefreshCode: true })}
        />

        <section className="wallet-brands-panel">
          <div className="section-heading section-heading--wallet">
            <h2>Мои награды</h2>
          </div>

          {!wallet || wallet.brands.length === 0 ? (
            <p className="muted-text">У вас пока что нет наград.</p>
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
    <button
      className={`wallet-brand-card ${isSelected ? 'wallet-brand-card--selected' : ''}`}
      type="button"
      onClick={onOpen}
      aria-label={`Открыть кошелёк бренда ${brand.brandName}`}
    >
      <span className="wallet-brand-card__top">
        <span className="wallet-brand-card__title">
          <span className="wallet-brand-card__name">{brand.brandName}</span>
          <span className="wallet-brand-card__meta">{metaText}</span>
        </span>
      </span>

      {visibleRewards.length > 0 ? (
        <span className="wallet-reward-chips" aria-label="Доступные награды">
          {visibleRewards.map((reward) => (
            <span className="wallet-reward-chip" key={reward.key}>
              {reward.name}
            </span>
          ))}
          {hiddenRewardCount > 0 ? (
            <span className="wallet-reward-chip wallet-reward-chip--more">+{hiddenRewardCount} ещё</span>
          ) : null}
        </span>
      ) : null}
    </button>
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

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось загрузить кошелёк.');
}
