import { useEffect, useState, type ReactNode } from 'react';
import {
  ArrowLeft,
  BadgePlus,
  BarChart3,
  Coins,
  Edit3,
  Gift,
  Plus,
  RefreshCw,
  Save,
  Search,
  Settings,
  Store,
  TicketMinus,
  Trash2,
  UserPlus,
  Users,
  X
} from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import {
  addBrandStaffByPhone,
  createCoinProduct,
  createMetric,
  deleteCoinProduct,
  getBrandStaff,
  getBrandWorkspace,
  getCoinProductPurchaseOptions,
  getIssueMetricOptions,
  getManageCoinProducts,
  getManageMetrics,
  getMyBrands,
  getRedeemMetricOptions,
  issueCoinsByPhone,
  issueMetricByPhone,
  purchaseCoinProduct,
  redeemCoins,
  redeemMetric,
  removeBrandStaff,
  updateBrandRewardSettings,
  updateCoinProduct,
  updateMetric,
  type AddBrandStaffByPhoneResponse,
  type BrandStaffResponse,
  type BrandWorkspaceResponse,
  type CoinOperationResponse,
  type CoinProductResponse,
  type CoinProductPurchaseOptionsResponse,
  type IssueMetricResponse,
  type MetricResponse,
  type MyBrandResponse,
  type RedeemMetricOptionsResponse,
  type RedeemMetricResponse,
  type UpdateBrandResponse
} from './brandWorkspaceApi';
import { formatRuPhoneInput, isRuPhoneInputComplete } from '../validation/phoneNumber';
import { RuPhoneInput } from '../components/RuPhoneInput';
import { formatRuDateTime } from '../format/dateTime';

type OperationResult =
  | { kind: 'metric'; title: string; response: IssueMetricResponse | RedeemMetricResponse }
  | { kind: 'coins'; title: string; response: CoinOperationResponse };

type StaffOperationResult =
  | { kind: 'add'; response: AddBrandStaffByPhoneResponse }
  | { kind: 'remove'; response: { userName: string; phoneNumber?: string | null } };

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
        onWorkspaceUpdated={(updatedWorkspace) => setWorkspace(updatedWorkspace)}
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
  onWorkspaceUpdated,
  onBack
}: {
  workspace: BrandWorkspaceResponse;
  onWorkspaceUpdated: (workspace: BrandWorkspaceResponse) => void;
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
  const showMetricManagement = workspace.canManageMetrics && workspace.isMetricsEnabled;
  const showCoinProductManagement =
    workspace.canManageMetrics && workspace.isCoinsEnabled && workspace.isCoinProductRedemptionEnabled;
  const showStaffManagement = workspace.canManageStaff;
  const showBrandSettings = workspace.canManageBrand;

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

      {showMetricManagement ? (
        <MetricManagementPanel
          brandId={workspace.brandId}
          onMetricsChanged={workspace.canIssue ? loadIssueMetrics : undefined}
        />
      ) : null}

      {showCoinProductManagement ? (
        <CoinProductManagementPanel brandId={workspace.brandId} />
      ) : null}

      {showStaffManagement ? (
        <StaffManagementPanel brandId={workspace.brandId} />
      ) : null}

      {showBrandSettings ? (
        <BrandSettingsPanel workspace={workspace} onWorkspaceUpdated={onWorkspaceUpdated} />
      ) : null}
    </div>
  );
}

function MetricManagementPanel({
  brandId,
  onMetricsChanged
}: {
  brandId: string;
  onMetricsChanged?: () => Promise<void>;
}) {
  const [metrics, setMetrics] = useState<MetricResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [newName, setNewName] = useState('');
  const [newRedemptionAmount, setNewRedemptionAmount] = useState('');
  const [editingMetricId, setEditingMetricId] = useState('');
  const [editName, setEditName] = useState('');
  const [editRedemptionAmount, setEditRedemptionAmount] = useState('');

  useEffect(() => {
    void loadMetrics();
  }, [brandId]);

  async function loadMetrics() {
    setIsLoading(true);
    setError('');

    try {
      const response = await getManageMetrics(brandId);
      setMetrics(response);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function refreshAllMetrics() {
    await loadMetrics();
    if (onMetricsChanged) {
      await onMetricsChanged();
    }
  }

  async function submitCreate() {
    const parsedAmount = Number(newRedemptionAmount);
    if (!newName.trim() || !Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Укажите название метрики и положительное количество для списания.');
      setStatus('');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      await createMetric(brandId, {
        name: newName.trim(),
        redemptionAmount: parsedAmount
      });
      setNewName('');
      setNewRedemptionAmount('');
      setStatus('Метрика создана.');
      await refreshAllMetrics();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  function startEdit(metric: MetricResponse) {
    setEditingMetricId(metric.id);
    setEditName(metric.name);
    setEditRedemptionAmount(String(metric.redemptionAmount));
    setError('');
    setStatus('');
  }

  function cancelEdit() {
    setEditingMetricId('');
    setEditName('');
    setEditRedemptionAmount('');
  }

  async function submitUpdate(metricId: string) {
    const parsedAmount = Number(editRedemptionAmount);
    if (!editName.trim() || !Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Укажите название метрики и положительное количество для списания.');
      setStatus('');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      await updateMetric(metricId, {
        name: editName.trim(),
        redemptionAmount: parsedAmount
      });
      cancelEdit();
      setStatus('Метрика обновлена.');
      await refreshAllMetrics();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="surface-panel metric-management">
      <div className="section-heading section-heading--split">
        <div>
          <div className="section-heading__title">
            <BarChart3 size={22} />
            <h2>Управление метриками</h2>
          </div>
          <p>Создание и редактирование метрик бренда.</p>
        </div>
        <button className="button-secondary button-compact" type="button" onClick={() => void refreshAllMetrics()}>
          <RefreshCw size={17} />
          Обновить
        </button>
      </div>

      <div className="metric-create-form">
        <label>
          Название
          <input value={newName} onChange={(event) => setNewName(event.target.value)} />
        </label>
        <label>
          Списание
          <input
            value={newRedemptionAmount}
            inputMode="numeric"
            onChange={(event) => setNewRedemptionAmount(event.target.value)}
          />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submitCreate()}>
          <Plus size={17} />
          Создать
        </button>
      </div>

      {isLoading ? <p className="muted-text">Загружаем метрики...</p> : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}

      {!isLoading && metrics.length === 0 ? (
        <p className="muted-text">Метрик пока нет.</p>
      ) : null}

      <div className="metric-management-list">
        {metrics.map((metric) => {
          const isEditing = editingMetricId === metric.id;

          return (
            <article className="metric-management-row" key={metric.id}>
              {isEditing ? (
                <div className="metric-edit-form">
                  <label>
                    Название
                    <input value={editName} onChange={(event) => setEditName(event.target.value)} />
                  </label>
                  <label>
                    Списание
                    <input
                      value={editRedemptionAmount}
                      inputMode="numeric"
                      onChange={(event) => setEditRedemptionAmount(event.target.value)}
                    />
                  </label>
                </div>
              ) : (
                <div className="metric-management-row__summary">
                  <div>
                    <h3>{metric.name}</h3>
                    <p>Списание: {metric.redemptionAmount}</p>
                  </div>
                  <span className={`status-pill ${metric.isActive ? 'status-pill--ok' : ''}`}>
                    {metric.isActive ? 'Активна' : 'Выключена'}
                  </span>
                </div>
              )}

              <div className="metric-management-row__actions">
                {isEditing ? (
                  <>
                    <button type="button" disabled={isSubmitting} onClick={() => void submitUpdate(metric.id)}>
                      <Save size={17} />
                      Сохранить
                    </button>
                    <button className="button-secondary" type="button" disabled={isSubmitting} onClick={cancelEdit}>
                      <X size={17} />
                      Отмена
                    </button>
                  </>
                ) : (
                  <button className="button-secondary" type="button" onClick={() => startEdit(metric)}>
                    <Edit3 size={17} />
                    Редактировать
                  </button>
                )}
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function CoinProductManagementPanel({ brandId }: { brandId: string }) {
  const [products, setProducts] = useState<CoinProductResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [newName, setNewName] = useState('');
  const [newPrice, setNewPrice] = useState('');
  const [editingProductId, setEditingProductId] = useState('');
  const [editName, setEditName] = useState('');
  const [editPrice, setEditPrice] = useState('');

  useEffect(() => {
    void loadProducts();
  }, [brandId]);

  async function loadProducts() {
    setIsLoading(true);
    setError('');

    try {
      const response = await getManageCoinProducts(brandId);
      setProducts(response);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function submitCreate() {
    const parsedPrice = Number(newPrice);
    if (!newName.trim() || !Number.isInteger(parsedPrice) || parsedPrice <= 0) {
      setError('Укажите название товара и положительную цену в монетках.');
      setStatus('');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      await createCoinProduct(brandId, {
        name: newName.trim(),
        price: parsedPrice
      });
      setNewName('');
      setNewPrice('');
      setStatus('Товар создан.');
      await loadProducts();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  function startEdit(product: CoinProductResponse) {
    setEditingProductId(product.id);
    setEditName(product.name);
    setEditPrice(String(product.price));
    setError('');
    setStatus('');
  }

  function cancelEdit() {
    setEditingProductId('');
    setEditName('');
    setEditPrice('');
  }

  async function submitUpdate(productId: string) {
    const parsedPrice = Number(editPrice);
    if (!editName.trim() || !Number.isInteger(parsedPrice) || parsedPrice <= 0) {
      setError('Укажите название товара и положительную цену в монетках.');
      setStatus('');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      await updateCoinProduct(productId, {
        name: editName.trim(),
        price: parsedPrice
      });
      cancelEdit();
      setStatus('Товар обновлен.');
      await loadProducts();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitDelete(product: CoinProductResponse) {
    if (!window.confirm(`Удалить товар "${product.name}"?`)) {
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      await deleteCoinProduct(product.id);
      if (editingProductId === product.id) {
        cancelEdit();
      }
      setStatus('Товар удален.');
      await loadProducts();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="surface-panel metric-management">
      <div className="section-heading section-heading--split">
        <div>
          <div className="section-heading__title">
            <Gift size={22} />
            <h2>Управление товарами</h2>
          </div>
          <p>Создание, редактирование и удаление товаров за монетки.</p>
        </div>
        <button className="button-secondary button-compact" type="button" onClick={() => void loadProducts()}>
          <RefreshCw size={17} />
          Обновить
        </button>
      </div>

      <div className="metric-create-form">
        <label>
          Название
          <input value={newName} onChange={(event) => setNewName(event.target.value)} />
        </label>
        <label>
          Цена
          <input value={newPrice} inputMode="numeric" onChange={(event) => setNewPrice(event.target.value)} />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submitCreate()}>
          <Plus size={17} />
          Создать
        </button>
      </div>

      {isLoading ? <p className="muted-text">Загружаем товары...</p> : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}

      {!isLoading && products.length === 0 ? (
        <p className="muted-text">Товаров пока нет.</p>
      ) : null}

      <div className="metric-management-list">
        {products.map((product) => {
          const isEditing = editingProductId === product.id;

          return (
            <article className="metric-management-row" key={product.id}>
              {isEditing ? (
                <div className="metric-edit-form">
                  <label>
                    Название
                    <input value={editName} onChange={(event) => setEditName(event.target.value)} />
                  </label>
                  <label>
                    Цена
                    <input value={editPrice} inputMode="numeric" onChange={(event) => setEditPrice(event.target.value)} />
                  </label>
                </div>
              ) : (
                <div className="metric-management-row__summary">
                  <div>
                    <h3>{product.name}</h3>
                    <p>Цена: {product.price} монеток</p>
                  </div>
                  <span className={`status-pill ${product.isActive ? 'status-pill--ok' : ''}`}>
                    {product.isActive ? 'Активен' : 'Выключен'}
                  </span>
                </div>
              )}

              <div className="metric-management-row__actions">
                {isEditing ? (
                  <>
                    <button type="button" disabled={isSubmitting} onClick={() => void submitUpdate(product.id)}>
                      <Save size={17} />
                      Сохранить
                    </button>
                    <button className="button-secondary" type="button" disabled={isSubmitting} onClick={cancelEdit}>
                      <X size={17} />
                      Отмена
                    </button>
                  </>
                ) : (
                  <>
                    <button className="button-secondary" type="button" onClick={() => startEdit(product)}>
                      <Edit3 size={17} />
                      Редактировать
                    </button>
                    <button
                      className="button-secondary"
                      type="button"
                      disabled={isSubmitting || !product.isActive}
                      onClick={() => void submitDelete(product)}
                    >
                      <Trash2 size={17} />
                      Удалить
                    </button>
                  </>
                )}
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function StaffManagementPanel({ brandId }: { brandId: string }) {
  const [staff, setStaff] = useState<BrandStaffResponse[]>([]);
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<StaffOperationResult | null>(null);

  useEffect(() => {
    void loadStaff();
  }, [brandId]);

  async function loadStaff() {
    setIsLoading(true);
    setError('');

    try {
      const response = await getBrandStaff(brandId);
      setStaff(response);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function submitAdd() {
    if (!isRuPhoneInputComplete(phoneNumber)) {
      setError('Укажите телефон сотрудника.');
      setResult(null);
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await addBrandStaffByPhone(brandId, phoneNumber.trim());
      setPhoneNumber(formatRuPhoneInput(''));
      setResult({ kind: 'add', response });
      await loadStaff();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitRemove(staffMember: BrandStaffResponse) {
    if (!window.confirm(`Удалить сотрудника "${staffMember.userName}"?`)) {
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await removeBrandStaff(brandId, staffMember.userId);
      setResult({
        kind: 'remove',
        response: {
          userName: response.userName,
          phoneNumber: response.phoneNumber
        }
      });
      await loadStaff();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="surface-panel staff-management">
      <div className="section-heading section-heading--split">
        <div>
          <div className="section-heading__title">
            <Users size={22} />
            <h2>Сотрудники</h2>
          </div>
          <p>Добавление сотрудников по телефону и управление доступом к бренду.</p>
        </div>
        <button className="button-secondary button-compact" type="button" onClick={() => void loadStaff()}>
          <RefreshCw size={17} />
          Обновить
        </button>
      </div>

      <div className="staff-add-form">
        <label>
          Телефон сотрудника
          <RuPhoneInput value={phoneNumber} onValueChange={setPhoneNumber} />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submitAdd()}>
          <UserPlus size={17} />
          Добавить
        </button>
      </div>

      {isLoading ? <p className="muted-text">Загружаем сотрудников...</p> : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      <StaffFeedback result={result} />

      {!isLoading && staff.length === 0 ? (
        <p className="muted-text">Сотрудников пока нет.</p>
      ) : null}

      <div className="staff-list">
        {staff.map((staffMember) => (
          <article className="staff-row" key={staffMember.userId}>
            <div>
              <h3>{staffMember.userName}</h3>
              <p>Телефон: {staffMember.phoneNumber || '-'}</p>
              <p>Добавлен: {formatRuDateTime(staffMember.membershipCreatedAt)}</p>
            </div>
            <button
              className="button-secondary"
              type="button"
              disabled={isSubmitting}
              onClick={() => void submitRemove(staffMember)}
            >
              <Trash2 size={17} />
              Удалить
            </button>
          </article>
        ))}
      </div>
    </section>
  );
}

function StaffFeedback({ result }: { result: StaffOperationResult | null }) {
  if (!result) {
    return null;
  }

  const userName = result.response.userName;
  const phoneNumber = result.response.phoneNumber || '-';

  return (
    <div className="operation-result">
      <strong>{result.kind === 'add' ? 'Сотрудник добавлен' : 'Сотрудник удален'}</strong>
      <span>{userName}</span>
      <span>{phoneNumber}</span>
    </div>
  );
}

function BrandSettingsPanel({
  workspace,
  onWorkspaceUpdated
}: {
  workspace: BrandWorkspaceResponse;
  onWorkspaceUpdated: (workspace: BrandWorkspaceResponse) => void;
}) {
  const [isMetricsEnabled, setIsMetricsEnabled] = useState(workspace.isMetricsEnabled);
  const [isCoinsEnabled, setIsCoinsEnabled] = useState(workspace.isCoinsEnabled);
  const [isCoinProductRedemptionEnabled, setIsCoinProductRedemptionEnabled] = useState(
    workspace.isCoinProductRedemptionEnabled
  );
  const [isManualCoinRedemptionEnabled, setIsManualCoinRedemptionEnabled] = useState(
    workspace.isManualCoinRedemptionEnabled
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  useEffect(() => {
    setIsMetricsEnabled(workspace.isMetricsEnabled);
    setIsCoinsEnabled(workspace.isCoinsEnabled);
    setIsCoinProductRedemptionEnabled(workspace.isCoinProductRedemptionEnabled);
    setIsManualCoinRedemptionEnabled(workspace.isManualCoinRedemptionEnabled);
  }, [
    workspace.brandId,
    workspace.isMetricsEnabled,
    workspace.isCoinsEnabled,
    workspace.isCoinProductRedemptionEnabled,
    workspace.isManualCoinRedemptionEnabled
  ]);

  useEffect(() => {
    setError('');
    setStatus('');
  }, [workspace.brandId]);

  const canSave = (isMetricsEnabled || isCoinsEnabled)
    && (!isCoinsEnabled || isCoinProductRedemptionEnabled || isManualCoinRedemptionEnabled);

  async function submit() {
    if (!canSave) {
      setError('Включите хотя бы один тип наград. Для монеток нужен хотя бы один способ списания.');
      setStatus('');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setStatus('');

    try {
      const response = await updateBrandRewardSettings(workspace.brandId, {
        isMetricsEnabled,
        isCoinsEnabled,
        isCoinProductRedemptionEnabled,
        isManualCoinRedemptionEnabled
      });
      onWorkspaceUpdated(mapUpdatedBrandToWorkspace(workspace, response));
      setStatus('Настройки сохранены.');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="surface-panel brand-settings-panel">
      <div className="section-heading section-heading--split">
        <div>
          <div className="section-heading__title">
            <Settings size={22} />
            <h2>Настройки бренда</h2>
          </div>
          <p>Включение метрик, монеток и способов списания.</p>
        </div>
        <button type="button" disabled={isSubmitting || !canSave} onClick={() => void submit()}>
          <Save size={17} />
          Сохранить
        </button>
      </div>

      <div className="settings-grid">
        <ToggleRow
          title="Учитывать метрики"
          description="Метрики доступны клиентам и сотрудникам."
          checked={isMetricsEnabled}
          onChange={setIsMetricsEnabled}
        />
        <ToggleRow
          title="Учитывать монетки"
          description="Монетки доступны для начислений, списаний и товаров."
          checked={isCoinsEnabled}
          onChange={setIsCoinsEnabled}
        />
        <ToggleRow
          title="Списывать за товары"
          description="Сотрудники могут выдавать товары за монетки."
          checked={isCoinProductRedemptionEnabled}
          onChange={setIsCoinProductRedemptionEnabled}
        />
        <ToggleRow
          title="Списывать произвольно"
          description="Сотрудники могут списывать произвольное количество монеток."
          checked={isManualCoinRedemptionEnabled}
          onChange={setIsManualCoinRedemptionEnabled}
        />
      </div>

      {!canSave ? (
        <p className="form-status form-status--error">
          Включите хотя бы один тип наград. Для монеток нужен хотя бы один способ списания.
        </p>
      ) : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}
    </section>
  );
}

function ToggleRow({
  title,
  description,
  checked,
  onChange
}: {
  title: string;
  description: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <label className="settings-toggle">
      <input
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
      <span className="settings-toggle__content">
        <strong>{title}</strong>
        <span>{description}</span>
      </span>
    </label>
  );
}

function mapUpdatedBrandToWorkspace(
  workspace: BrandWorkspaceResponse,
  response: UpdateBrandResponse
): BrandWorkspaceResponse {
  return {
    ...workspace,
    brandName: response.brandName,
    isMetricsEnabled: response.isMetricsEnabled,
    isCoinsEnabled: response.isCoinsEnabled,
    isCoinProductRedemptionEnabled: response.isCoinProductRedemptionEnabled,
    isManualCoinRedemptionEnabled: response.isManualCoinRedemptionEnabled
  };
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
