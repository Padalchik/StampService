import { useEffect, useState, type ReactNode } from 'react';
import {
  ArrowLeft,
  BadgePlus,
  Coins,
  Gift,
  RefreshCw,
  Search,
  Store,
  TicketMinus
} from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import {
  getBrandWorkspace,
  getCoinProductPurchaseOptions,
  getIssueMetricOptions,
  getMyBrands,
  getRedeemMetricOptions,
  issueCoinsByPhone,
  issueMetricByPhone,
  purchaseCoinProduct,
  redeemCoins,
  redeemMetric,
  type BrandWorkspaceResponse,
  type CoinOperationResponse,
  type CoinProductPurchaseOptionsResponse,
  type IssueMetricResponse,
  type MetricResponse,
  type MyBrandResponse,
  type RedeemMetricOptionsResponse,
  type RedeemMetricResponse
} from './brandWorkspaceApi';
import { formatRuPhoneInput, isRuPhoneInputComplete } from '../validation/phoneNumber';
import { RuPhoneInput } from '../components/RuPhoneInput';

type OperationResult =
  | { kind: 'metric'; title: string; response: IssueMetricResponse | RedeemMetricResponse }
  | { kind: 'coins'; title: string; response: CoinOperationResponse };

export function BrandWorkspacePage() {
  const [brands, setBrands] = useState<MyBrandResponse[]>([]);
  const [workspace, setWorkspace] = useState<BrandWorkspaceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isWorkspaceLoading, setIsWorkspaceLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    void loadBrands();
  }, []);

  async function loadBrands() {
    setIsLoading(true);
    setError('');

    try {
      const response = await getMyBrands();
      setBrands(response.brands);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function openWorkspace(brandId: string) {
    setIsWorkspaceLoading(true);
    setError('');

    try {
      const response = await getBrandWorkspace(brandId);
      setWorkspace(response);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsWorkspaceLoading(false);
    }
  }

  if (workspace) {
    return (
      <BrandWorkspace
        workspace={workspace}
        onBack={() => {
          setWorkspace(null);
          setError('');
        }}
      />
    );
  }

  return (
    <div className="brand-workspace-page">
      <section className="surface-panel">
        <div className="section-heading section-heading--split">
          <div>
            <div className="section-heading__title">
              <Store size={22} />
              <h2>Рабочие бренды</h2>
            </div>
            <p>Выберите бренд для операций с клиентами.</p>
          </div>
          <button className="button-secondary button-compact" type="button" onClick={() => void loadBrands()}>
            <RefreshCw size={17} />
            Обновить
          </button>
        </div>

        {isLoading ? <p className="muted-text">Загружаем бренды...</p> : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}

        {!isLoading && brands.length === 0 ? (
          <p className="muted-text">У вас пока нет рабочих брендов.</p>
        ) : null}

        <div className="brand-list">
          {brands.map((brand) => (
            <article className="brand-list-item" key={brand.brandId}>
              <div>
                <h3>{brand.brandName}</h3>
                <p>Роль: {brand.roleSystemName}</p>
              </div>
              <button
                className="button-secondary"
                type="button"
                disabled={isWorkspaceLoading}
                onClick={() => void openWorkspace(brand.brandId)}
              >
                Открыть
              </button>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}

function BrandWorkspace({
  workspace,
  onBack
}: {
  workspace: BrandWorkspaceResponse;
  onBack: () => void;
}) {
  const [metrics, setMetrics] = useState<MetricResponse[]>([]);
  const [metricsError, setMetricsError] = useState('');

  useEffect(() => {
    if (!workspace.isMetricsEnabled || !workspace.canIssue) {
      setMetrics([]);
      return;
    }

    void loadIssueMetrics();
  }, [workspace.brandId, workspace.canIssue, workspace.isMetricsEnabled]);

  async function loadIssueMetrics() {
    setMetricsError('');

    try {
      const response = await getIssueMetricOptions(workspace.brandId);
      setMetrics(response);
    } catch (requestError) {
      setMetricsError(getUserMessage(requestError));
    }
  }

  const hasClientActions = workspace.canIssue || workspace.canRedeem;

  return (
    <div className="brand-workspace-page">
      <section className="surface-panel brand-workspace-header">
        <button className="button-secondary button-compact" type="button" onClick={onBack}>
          <ArrowLeft size={17} />
          Назад
        </button>
        <div>
          <h2>{workspace.brandName}</h2>
          <p>Роль: {workspace.roleSystemName}</p>
          <div className="workspace-flags">
            {workspace.isMetricsEnabled ? <span>Метрики включены</span> : null}
            {workspace.isCoinsEnabled ? <span>Монетки включены</span> : null}
            {workspace.isCoinProductRedemptionEnabled ? <span>Товары за монетки</span> : null}
            {workspace.isManualCoinRedemptionEnabled ? <span>Ручное списание монеток</span> : null}
          </div>
        </div>
      </section>

      {!hasClientActions ? (
        <section className="surface-panel">
          <p className="muted-text">Для этого бренда нет доступных клиентских операций.</p>
        </section>
      ) : null}

      <div className="operation-grid">
        {workspace.isMetricsEnabled && workspace.canIssue ? (
          <IssueMetricPanel metrics={metrics} metricsError={metricsError} onReloadMetrics={loadIssueMetrics} />
        ) : null}

        {workspace.isMetricsEnabled && workspace.canRedeem ? (
          <RedeemMetricPanel brandId={workspace.brandId} />
        ) : null}

        {workspace.isCoinsEnabled && workspace.canIssue ? (
          <IssueCoinsPanel brandId={workspace.brandId} />
        ) : null}

        {workspace.isCoinsEnabled && workspace.canRedeem && workspace.isManualCoinRedemptionEnabled ? (
          <RedeemCoinsPanel brandId={workspace.brandId} />
        ) : null}

        {workspace.isCoinsEnabled && workspace.canRedeem && workspace.isCoinProductRedemptionEnabled ? (
          <PurchaseCoinProductPanel brandId={workspace.brandId} />
        ) : null}
      </div>
    </div>
  );
}

function IssueMetricPanel({
  metrics,
  metricsError,
  onReloadMetrics
}: {
  metrics: MetricResponse[];
  metricsError: string;
  onReloadMetrics: () => Promise<void>;
}) {
  const [metricId, setMetricId] = useState('');
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [amount, setAmount] = useState('');
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<OperationResult | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!metricId && metrics.length > 0) {
      setMetricId(metrics[0].id);
    }
  }, [metricId, metrics]);

  async function submit() {
    const parsedAmount = Number(amount);
    if (!metricId || !isRuPhoneInputComplete(phoneNumber) || !Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Выберите метрику, укажите телефон клиента и положительное количество.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await issueMetricByPhone(metricId, {
        phoneNumber: phoneNumber.trim(),
        amount: parsedAmount,
        comment: comment.trim() || undefined
      });
      setResult({ kind: 'metric', title: 'Метрика выдана', response });
      setAmount('');
      setComment('');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <OperationPanel icon={<BadgePlus size={20} />} title="Выдать метрику">
      {metricsError ? <p className="form-status form-status--error">{metricsError}</p> : null}
      <div className="work-form">
        <label>
          Метрика
          <select value={metricId} onChange={(event) => setMetricId(event.target.value)}>
            {metrics.map((metric) => (
              <option key={metric.id} value={metric.id}>
                {metric.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Телефон клиента
          <RuPhoneInput
            value={phoneNumber}
            onValueChange={setPhoneNumber}
          />
        </label>
        <label>
          Количество
          <input value={amount} inputMode="numeric" onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label>
          Комментарий
          <input value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <div className="form-actions">
          <button type="button" disabled={isSubmitting || metrics.length === 0} onClick={() => void submit()}>
            Выдать
          </button>
          <button className="button-secondary" type="button" onClick={() => void onReloadMetrics()}>
            Обновить
          </button>
        </div>
      </div>
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function RedeemMetricPanel({ brandId }: { brandId: string }) {
  const [redemptionCode, setRedemptionCode] = useState('');
  const [options, setOptions] = useState<RedeemMetricOptionsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmittingId, setIsSubmittingId] = useState('');
  const [error, setError] = useState('');
  const [result, setResult] = useState<OperationResult | null>(null);

  async function loadOptions() {
    if (!redemptionCode.trim()) {
      setError('Введите код списания клиента.');
      return;
    }

    setIsLoading(true);
    setError('');
    setResult(null);

    try {
      const response = await getRedeemMetricOptions(brandId, redemptionCode.trim());
      setOptions(response);
    } catch (requestError) {
      setOptions(null);
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function submit(metricDefinitionId: string) {
    setIsSubmittingId(metricDefinitionId);
    setError('');
    setResult(null);

    try {
      const response = await redeemMetric(metricDefinitionId, {
        redemptionCode: redemptionCode.trim(),
        comment: 'Redeem metric'
      });
      setResult({ kind: 'metric', title: 'Метрика списана', response });
      setOptions(null);
      setRedemptionCode('');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmittingId('');
    }
  }

  return (
    <OperationPanel icon={<TicketMinus size={20} />} title="Списать метрику">
      <div className="work-form">
        <label>
          Код списания клиента
          <input value={redemptionCode} inputMode="numeric" onChange={(event) => setRedemptionCode(event.target.value)} />
        </label>
        <button className="button-secondary" type="button" disabled={isLoading} onClick={() => void loadOptions()}>
          <Search size={17} />
          Показать метрики
        </button>
      </div>

      {options ? (
        <div className="operation-options">
          <p className="operation-options__customer">Клиент: {options.customerName}</p>
          {options.metrics.length === 0 ? <p className="muted-text">Доступных метрик нет.</p> : null}
          {options.metrics.map((metric) => (
            <div className="operation-option" key={metric.metricDefinitionId}>
              <div>
                <strong>{metric.metricName}</strong>
                <p>
                  Баланс {metric.currentBalance} из {metric.requiredAmount}
                </p>
              </div>
              <button
                type="button"
                disabled={!metric.canRedeem || isSubmittingId === metric.metricDefinitionId}
                onClick={() => void submit(metric.metricDefinitionId)}
              >
                Списать
              </button>
            </div>
          ))}
        </div>
      ) : null}
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function IssueCoinsPanel({ brandId }: { brandId: string }) {
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [amount, setAmount] = useState('');
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<OperationResult | null>(null);

  async function submit() {
    const parsedAmount = Number(amount);
    if (!isRuPhoneInputComplete(phoneNumber) || !Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Укажите телефон клиента и положительное количество монеток.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await issueCoinsByPhone(brandId, {
        phoneNumber: phoneNumber.trim(),
        amount: parsedAmount,
        comment: comment.trim() || undefined
      });
      setResult({ kind: 'coins', title: 'Монетки начислены', response });
      setAmount('');
      setComment('');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <OperationPanel icon={<Coins size={20} />} title="Начислить монетки">
      <div className="work-form">
        <label>
          Телефон клиента
          <RuPhoneInput
            value={phoneNumber}
            onValueChange={setPhoneNumber}
          />
        </label>
        <label>
          Количество
          <input value={amount} inputMode="numeric" onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label>
          Комментарий
          <input value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submit()}>
          Начислить
        </button>
      </div>
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function RedeemCoinsPanel({ brandId }: { brandId: string }) {
  const [redemptionCode, setRedemptionCode] = useState('');
  const [amount, setAmount] = useState('');
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<OperationResult | null>(null);

  async function submit() {
    const parsedAmount = Number(amount);
    if (!redemptionCode.trim() || !Number.isInteger(parsedAmount) || parsedAmount <= 0 || !comment.trim()) {
      setError('Укажите код списания клиента, положительное количество и назначение списания.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await redeemCoins(brandId, {
        redemptionCode: redemptionCode.trim(),
        amount: parsedAmount,
        comment: comment.trim()
      });
      setResult({ kind: 'coins', title: 'Монетки списаны', response });
      setRedemptionCode('');
      setAmount('');
      setComment('');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <OperationPanel icon={<TicketMinus size={20} />} title="Списать монетки">
      <div className="work-form">
        <label>
          Код списания клиента
          <input value={redemptionCode} inputMode="numeric" onChange={(event) => setRedemptionCode(event.target.value)} />
        </label>
        <label>
          Количество
          <input value={amount} inputMode="numeric" onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label>
          Назначение списания
          <input value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submit()}>
          Списать
        </button>
      </div>
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function PurchaseCoinProductPanel({ brandId }: { brandId: string }) {
  const [redemptionCode, setRedemptionCode] = useState('');
  const [options, setOptions] = useState<CoinProductPurchaseOptionsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmittingId, setIsSubmittingId] = useState('');
  const [error, setError] = useState('');
  const [result, setResult] = useState<OperationResult | null>(null);

  async function loadOptions() {
    if (!redemptionCode.trim()) {
      setError('Введите код списания клиента.');
      return;
    }

    setIsLoading(true);
    setError('');
    setResult(null);

    try {
      const response = await getCoinProductPurchaseOptions(brandId, redemptionCode.trim());
      setOptions(response);
    } catch (requestError) {
      setOptions(null);
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function submit(productId: string) {
    setIsSubmittingId(productId);
    setError('');
    setResult(null);

    try {
      const response = await purchaseCoinProduct(brandId, productId, redemptionCode.trim());
      setResult({ kind: 'coins', title: 'Товар списан за монетки', response });
      setOptions(null);
      setRedemptionCode('');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmittingId('');
    }
  }

  return (
    <OperationPanel icon={<Gift size={20} />} title="Выдать товар за монетки">
      <div className="work-form">
        <label>
          Код списания клиента
          <input value={redemptionCode} inputMode="numeric" onChange={(event) => setRedemptionCode(event.target.value)} />
        </label>
        <button className="button-secondary" type="button" disabled={isLoading} onClick={() => void loadOptions()}>
          <Search size={17} />
          Показать товары
        </button>
      </div>

      {options ? (
        <div className="operation-options">
          <p className="operation-options__customer">Клиент: {options.customerName}</p>
          {options.products.length === 0 ? <p className="muted-text">Доступных товаров нет.</p> : null}
          {options.products.map((product) => (
            <div className="operation-option" key={product.productId}>
              <div>
                <strong>{product.productName}</strong>
                <p>
                  Цена {product.price}, баланс {product.currentBalance}
                </p>
              </div>
              <button
                type="button"
                disabled={!product.canPurchase || isSubmittingId === product.productId}
                onClick={() => void submit(product.productId)}
              >
                Выдать
              </button>
            </div>
          ))}
        </div>
      ) : null}
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function OperationPanel({
  icon,
  title,
  children
}: {
  icon: ReactNode;
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="operation-panel">
      <div className="operation-panel__heading">
        {icon}
        <h3>{title}</h3>
      </div>
      {children}
    </section>
  );
}

function OperationFeedback({ error, result }: { error: string; result: OperationResult | null }) {
  if (error) {
    return <p className="form-status form-status--error">{error}</p>;
  }

  if (!result) {
    return null;
  }

  if (result.kind === 'coins') {
    return (
      <div className="operation-result">
        <strong>{result.title}</strong>
        <span>Клиент: {result.response.userName}</span>
        <span>Количество: {result.response.amount}</span>
        <span>Баланс: {result.response.balanceValue}</span>
      </div>
    );
  }

  return (
    <div className="operation-result">
      <strong>{result.title}</strong>
      <span>Количество: {result.response.amount}</span>
      <span>Баланс: {result.response.balanceValue}</span>
    </div>
  );
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
