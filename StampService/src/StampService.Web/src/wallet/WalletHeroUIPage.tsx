import { Alert, Button, Card, Chip } from '@heroui/react';
import { useEffect, useRef, useState } from 'react';
import { ArrowLeft, BarChart3, ChevronRight, Gift, History, RefreshCw, Ticket, WalletCards } from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import { formatRuDateTime, formatRuTime } from '../format/dateTime';
import {
  getBrandDetails,
  openUserWallet,
  type UserWalletBrandDetailsResponse,
  type UserWalletBrandHistoryGroupResponse,
  type UserWalletBrandOverviewResponse,
  type UserWalletBrandRewardSectionResponse,
  type UserWalletResponse
} from './walletApi';

export function WalletHeroUIPage() {
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

  if (isLoading) {
    return (
      <section className="surface-panel">
        <p className="muted-text">Загружаем кошелек...</p>
      </section>
    );
  }

  if (selectedBrandId) {
    return (
      <BrandDetailsHeroUIScreen
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
    <div className="wallet-page heroui-experiment heroui-wallet-page">
      <Card className="heroui-wallet-code-card">
        <Card.Header className="heroui-card-header heroui-wallet-code-card__header">
          <div className="wallet-code-panel__icon" aria-hidden="true">
            <Ticket size={26} />
          </div>
          <div>
            <Card.Title>Код для списания</Card.Title>
            <Card.Description>Покажите этот код сотруднику бренда для операции списания.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          <div className="heroui-redemption-code">{wallet?.redemptionCode.code ?? '----'}</div>
          {wallet?.redemptionCode.expiresAtUtc ? (
            <p className="heroui-wallet-code-card__expires">
              Действует до {formatRuTime(wallet.redemptionCode.expiresAtUtc)}
            </p>
          ) : null}
        </Card.Content>

        <Card.Footer>
          <Button
            type="button"
            className="heroui-button-outline"
            variant="outline"
            isDisabled={isRefreshingCode}
            onPress={() => void loadWallet(true)}
          >
            <RefreshCw size={18} />
            Обновить код
          </Button>
        </Card.Footer>
      </Card>

      {error ? (
        <Alert status="danger">
          <Alert.Content>
            <Alert.Title>Не удалось загрузить кошелек</Alert.Title>
            <Alert.Description>{error}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      <Card className="heroui-work-card">
        <Card.Header className="heroui-card-header">
          <WalletCards size={22} />
          <div>
            <Card.Title>Балансы и доступные награды</Card.Title>
            <Card.Description>HeroUI-копия списка брендов из раздела кошелька.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          {!wallet || wallet.brands.length === 0 ? (
            <p className="muted-text">У вас пока нет балансов.</p>
          ) : (
            <div className="heroui-wallet-brand-list">
              {wallet.brands.map((brand) => (
                <WalletBrandHeroUICard
                  key={brand.brandId}
                  brand={brand}
                  onOpen={() => void loadBrandDetails(brand.brandId)}
                />
              ))}
            </div>
          )}
        </Card.Content>
      </Card>
    </div>
  );
}

function WalletBrandHeroUICard({
  brand,
  onOpen
}: {
  brand: UserWalletBrandOverviewResponse;
  onOpen: () => void;
}) {
  const hasCoinProducts = brand.isCoinsEnabled && brand.availableCoinProducts.length > 0;
  const hasMetrics = brand.isMetricsEnabled && brand.availableMetrics.length > 0;
  const hasRewards = hasCoinProducts || hasMetrics;

  return (
    <Card className="heroui-wallet-brand-card">
      <Card.Header className="heroui-wallet-brand-card__header">
        <div>
          <Card.Title>{brand.brandName}</Card.Title>
          <div className="heroui-chip-row">
            {brand.isCoinsEnabled ? <Chip color="accent" variant="soft">Монетки: {brand.coinBalance}</Chip> : null}
            {brand.isMetricsEnabled ? <Chip color="default" variant="soft">Метрики включены</Chip> : null}
          </div>
        </div>
        <Button
          type="button"
          className="heroui-button-outline"
          variant="outline"
          size="sm"
          onPress={onOpen}
        >
          Подробнее
          <ChevronRight size={17} />
        </Button>
      </Card.Header>

      <Card.Content>
        <div className="heroui-wallet-reward-grid">
          {hasCoinProducts ? (
            <WalletRewardPreview
              title="Доступные товары"
              items={brand.availableCoinProducts.map((product) => ({
                id: product.productId,
                name: product.productName,
                value: `${product.price} монеток`,
                available: product.isAvailable
              }))}
            />
          ) : null}

          {hasMetrics ? (
            <WalletRewardPreview
              title="Доступные метрики"
              items={brand.availableMetrics.map((metric) => ({
                id: metric.metricDefinitionId,
                name: metric.metricName,
                value: `${metric.currentBalance}/${metric.requiredAmount}`,
                available: metric.isAvailable
              }))}
            />
          ) : null}
        </div>

        {!hasRewards ? <p className="muted-text">Доступных наград пока нет.</p> : null}
      </Card.Content>
    </Card>
  );
}

function WalletRewardPreview({
  title,
  items
}: {
  title: string;
  items: Array<{ id: string; name: string; value: string; available: boolean }>;
}) {
  return (
    <section className="heroui-wallet-preview">
      <h4>{title}</h4>
      <ul>
        {items.map((item) => (
          <li key={item.id}>
            <span>{item.name}</span>
            <Chip color={item.available ? 'success' : 'warning'} size="sm" variant="soft">
              {item.value}
            </Chip>
          </li>
        ))}
      </ul>
    </section>
  );
}

function BrandDetailsHeroUIScreen({
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
    <div className="brand-detail-page heroui-experiment heroui-wallet-page">
      <Card className="heroui-work-card">
        <Card.Header className="heroui-card-header heroui-brand-detail-header">
          <Button
            type="button"
            className="heroui-button-outline"
            variant="outline"
            size="sm"
            onPress={onBack}
          >
            <ArrowLeft size={17} />
            Назад
          </Button>
          <div>
            <Card.Title>{brandName}</Card.Title>
            <Card.Description>Доступные награды и история операций.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          {isLoading ? <p className="muted-text">Загружаем бренд...</p> : null}
          {error ? (
            <Alert status="danger">
              <Alert.Content>
                <Alert.Title>Не удалось загрузить бренд</Alert.Title>
                <Alert.Description>{error}</Alert.Description>
              </Alert.Content>
            </Alert>
          ) : null}

          {!isLoading && details ? <BrandRewardsHeroUI details={details} /> : null}
          {!isLoading && details ? <BrandHistoryHeroUI history={details.history} /> : null}
        </Card.Content>
      </Card>
    </div>
  );
}

function BrandRewardsHeroUI({ details }: { details: UserWalletBrandDetailsResponse }) {
  return (
    <div className="heroui-brand-details-grid">
      {details.rewardSections.map((section) => (
        <RewardSectionHeroUI key={section.kind} section={section} />
      ))}

      <Alert status="default">
        <Alert.Content>
          <Alert.Title>Подсказка</Alert.Title>
          <Alert.Description>{details.hintText}</Alert.Description>
        </Alert.Content>
      </Alert>
    </div>
  );
}

function RewardSectionHeroUI({ section }: { section: UserWalletBrandRewardSectionResponse }) {
  const Icon = section.kind === 'Metrics' ? BarChart3 : Gift;

  return (
    <Card className="heroui-detail-card">
      <Card.Header className="heroui-card-header">
        <Icon size={19} />
        <div>
          <Card.Title>{section.title}</Card.Title>
          {section.balanceText ? <Card.Description>{section.balanceText}</Card.Description> : null}
        </div>
      </Card.Header>

      <Card.Content>
        {section.items.length > 0 ? (
          <ul className="heroui-detail-list">
            {section.items.map((item) => (
              <li key={item.itemId}>
                <div>
                  <strong>{item.name}</strong>
                  <span>{item.progressText}</span>
                </div>
                <Chip color={item.isAvailable ? 'success' : 'warning'} size="sm" variant="soft">
                  {item.statusText}
                </Chip>
              </li>
            ))}
          </ul>
        ) : (
          <p className="muted-text">{section.emptyText}</p>
        )}
      </Card.Content>
    </Card>
  );
}

function BrandHistoryHeroUI({ history }: { history: UserWalletBrandDetailsResponse['history'] }) {
  const hasItems = history.groups.some((group) => group.items.length > 0);

  return (
    <Card className="heroui-detail-card heroui-history-card">
      <Card.Header className="heroui-card-header">
        <History size={19} />
        <div>
          <Card.Title>{history.title}</Card.Title>
        </div>
      </Card.Header>

      <Card.Content>
        {!hasItems ? (
          <p className="muted-text">{history.emptyText}</p>
        ) : (
          <div className="heroui-history-grid">
            {history.groups.map((group) => (
              <HistorySectionHeroUI key={group.kind} group={group} />
            ))}
          </div>
        )}
      </Card.Content>
    </Card>
  );
}

function HistorySectionHeroUI({ group }: { group: UserWalletBrandHistoryGroupResponse }) {
  return (
    <section className="heroui-history-section">
      <h4>{group.title}</h4>
      {group.items.length === 0 ? (
        <p className="muted-text">{group.emptyText}</p>
      ) : (
        <ul>
          {group.items.map((item) => (
            <li key={`${item.sourceType}-${item.sourceName}-${item.createdAt}-${item.amount}`}>
              <Chip color={item.transactionType === 'Issue' ? 'success' : 'danger'} size="sm" variant="soft">
                {item.amountText}
              </Chip>
              <div>
                <strong>{item.sourceName}</strong>
                <p>
                  {formatRuDateTime(item.createdAt)}
                  {item.hasVisibleComment && item.comment ? ` - ${item.comment}` : ''}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось загрузить кошелек.');
}
