import { useEffect, useState, type ReactNode } from 'react';
import {
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
  TicketMinus,
  Trash2,
  UserPlus,
  Users,
  X
} from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import {
  addBrandStaffByPhone,
  createBrandCustomerByPhone,
  createCoinProduct,
  createMetric,
  deleteCoinProduct,
  getBrandCustomerCard,
  getBrandStaff,
  getCoinProductPurchaseOptions,
  getIssueMetricOptions,
  getManageCoinProducts,
  getManageMetrics,
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
  type BrandCustomerCardResponse,
  type BrandStaffResponse,
  type BrandWorkspaceResponse,
  type CoinOperationResponse,
  type CoinProductResponse,
  type CoinProductPurchaseOptionsResponse,
  type IssueMetricResponse,
  type MetricResponse,
  type RedeemMetricOptionsResponse,
  type RedeemMetricResponse,
  type UpdateBrandResponse
} from './brandWorkspaceApi';
import { formatRuPhoneInput, isRuPhoneInputComplete } from '../validation/phoneNumber';
import { RuPhoneInput } from '../components/RuPhoneInput';
import { formatRuDateTime } from '../format/dateTime';
import { WalletBrandDetailsBlock } from '../wallet/WalletBrandDetailsBlock';
import { BrandCustomerSearchScreen, rememberRecentCustomerPhone } from './BrandCustomerSearchScreen';

type OperationResult =
  | { kind: 'metric'; title: string; response: IssueMetricResponse | RedeemMetricResponse }
  | { kind: 'coins'; title: string; response: CoinOperationResponse };

type StaffOperationResult =
  | { kind: 'add'; response: AddBrandStaffByPhoneResponse }
  | { kind: 'remove'; response: { userName: string; phoneNumber?: string | null } };

type OperationType = 'metrics' | 'coins';
type OperationAction = 'issue' | 'purchase' | 'redeem';
type ManagementType = 'metrics' | 'products' | 'staff' | 'brand';
type BrandWorkspaceScreen = 'customer-search' | 'customer-work' | 'settings';
type DraftCustomerCard = {
  kind: 'draft';
  brandId: string;
  customerName: string;
  customerPhoneNumber: string;
};
type SavedCustomerCard = BrandCustomerCardResponse & { kind?: 'saved' };
type SelectedCustomerCard = DraftCustomerCard | SavedCustomerCard;
type CustomerOperationTarget = {
  customerName: string;
  customerPhoneNumber: string;
};

type WorkspaceTabItem<T extends string> = {
  id: T;
  label: string;
};

export function BrandWorkspace({
  workspace,
  onWorkspaceUpdated
}: {
  workspace: BrandWorkspaceResponse;
  onWorkspaceUpdated: (workspace: BrandWorkspaceResponse) => void;
}) {
  const [metrics, setMetrics] = useState<MetricResponse[]>([]);
  const [metricsError, setMetricsError] = useState('');
  const [activeOperationType, setActiveOperationType] = useState<OperationType>('metrics');
  const [activeOperationAction, setActiveOperationAction] = useState<OperationAction>('issue');
  const [selectedCustomer, setSelectedCustomer] = useState<SelectedCustomerCard | null>(null);
  const [customerCardError, setCustomerCardError] = useState('');
  const [activeScreen, setActiveScreen] = useState<BrandWorkspaceScreen>('customer-search');

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

  const showMetricManagement = workspace.canManageMetrics && workspace.isMetricsEnabled;
  const showCoinProductManagement =
    workspace.canManageMetrics && workspace.isCoinsEnabled && workspace.isCoinProductRedemptionEnabled;
  const showStaffManagement = workspace.canManageStaff;
  const showBrandSettings = workspace.canManageBrand;

  const operationTabs = selectedCustomer?.kind === 'draft' ? [] : getOperationTabs(workspace);
  const managementTabs = getManagementTabs({
    showMetricManagement,
    showCoinProductManagement,
    showStaffManagement,
    showBrandSettings
  });
  const currentOperationType = operationTabs.some((tab) => tab.id === activeOperationType)
    ? activeOperationType
    : operationTabs[0]?.id;
  const operationActionTabs = currentOperationType
    ? getOperationActionTabs(workspace, currentOperationType)
    : [];
  const currentOperationAction = operationActionTabs.some((tab) => tab.id === activeOperationAction)
    ? activeOperationAction
    : operationActionTabs[0]?.id;

  useEffect(() => {
    if (currentOperationType && currentOperationType !== activeOperationType) {
      setActiveOperationType(currentOperationType);
    }
  }, [activeOperationType, currentOperationType]);

  useEffect(() => {
    if (currentOperationAction && currentOperationAction !== activeOperationAction) {
      setActiveOperationAction(currentOperationAction);
    }
  }, [activeOperationAction, currentOperationAction]);

  if (activeScreen === 'settings') {
    return (
      <BrandSettingsPage
        workspace={workspace}
        managementTabs={managementTabs}
        onWorkspaceUpdated={onWorkspaceUpdated}
        onMetricsChanged={workspace.canIssue ? loadIssueMetrics : undefined}
      />
    );
  }

  return (
    <div className="brand-workspace-page brand-workspace-console">
      <section className="brand-workspace-hero">
        <div>
          <h2>{workspace.brandName}</h2>
          <p>Роль: {formatRoleName(workspace.roleSystemName)}</p>
        </div>
        {managementTabs.length > 0 ? (
          <button
            className="brand-workspace-hero__settings"
            type="button"
            aria-label="Управление брендом"
            title="Управление брендом"
            onClick={() => setActiveScreen('settings')}
          >
            <Settings size={20} aria-hidden="true" />
          </button>
        ) : null}
      </section>

      {!workspace.canViewBalances ? (
        <section className="operation-panel">
          <p className="muted-text">Для открытия карточки клиента нет доступа.</p>
        </section>
      ) : null}

      {workspace.canViewBalances && activeScreen === 'customer-search' ? (
        <BrandCustomerSearchScreen
          brandId={workspace.brandId}
          onCustomerFound={(customer) => {
            setSelectedCustomer(customer);
            setCustomerCardError('');
            setActiveScreen('customer-work');
          }}
          onCustomerNotFound={(phoneNumber) => {
            setSelectedCustomer(createDraftCustomerCard(workspace.brandId, phoneNumber));
            setCustomerCardError('');
            setActiveOperationAction('issue');
            setActiveScreen('customer-work');
          }}
        />
      ) : null}

      {workspace.canViewBalances && selectedCustomer && activeScreen === 'customer-work' ? (
        <SelectedCustomerWorkspace
          customer={selectedCustomer}
          customerCardError={customerCardError}
          operationTabs={operationTabs}
          actionTabs={operationActionTabs}
          activeOperationType={currentOperationType}
          activeOperationAction={currentOperationAction}
          workspace={workspace}
          metrics={metrics}
          metricsError={metricsError}
          onOperationTypeChange={(nextType) => {
            setActiveOperationType(nextType);
            const nextActions = getOperationActionTabs(workspace, nextType);
            if (nextActions[0]) {
              setActiveOperationAction(nextActions[0].id);
            }
          }}
          onOperationActionChange={setActiveOperationAction}
          onCustomerChanged={refreshSelectedCustomer}
          onCreateCustomer={createSelectedDraftCustomer}
        />
      ) : null}
    </div>
  );

  async function createSelectedDraftCustomer(): Promise<void> {
    if (!selectedCustomer || selectedCustomer.kind !== 'draft') {
      return;
    }

    setCustomerCardError('');
    const card = await createAndReloadCustomerCard(selectedCustomer.customerPhoneNumber);
    setSelectedCustomer(card);
    rememberRecentCustomerPhone(workspace.brandId, card.customerPhoneNumber);
  }

  async function createAndReloadCustomerCard(customerPhoneNumber: string): Promise<BrandCustomerCardResponse> {
    await createBrandCustomerByPhone(workspace.brandId, customerPhoneNumber);
    return await loadCustomerCardByPhone(customerPhoneNumber);
  }

  async function refreshSelectedCustomer() {
    if (!selectedCustomer) {
      return;
    }

    setCustomerCardError('');

    try {
      const wasDraftCustomer = selectedCustomer.kind === 'draft';
      const response = await getBrandCustomerCard(workspace.brandId, selectedCustomer.customerPhoneNumber);
      if (!response.found || !response.card) {
        setCustomerCardError('Клиент пока не найден. Повторите обновление после создания.');
        return;
      }

      setSelectedCustomer(response.card);
      if (wasDraftCustomer) {
        rememberRecentCustomerPhone(workspace.brandId, response.card.customerPhoneNumber);
      }
    } catch (requestError) {
      setCustomerCardError(getUserMessage(requestError));
    }
  }

  async function loadCustomerCardByPhone(customerPhoneNumber: string): Promise<BrandCustomerCardResponse> {
    const response = await getBrandCustomerCard(workspace.brandId, customerPhoneNumber);
    if (!response.found || !response.card) {
      throw new Error('Клиент пока не найден. Повторите обновление после создания.');
    }

    return response.card;
  }
}

function WorkspaceTabs<T extends string>({
  items,
  activeId,
  onSelect
}: {
  items: WorkspaceTabItem<T>[];
  activeId?: T;
  onSelect: (id: T) => void;
}) {
  if (items.length <= 1) {
    return null;
  }

  return (
    <div className="workspace-tabs-card">
      <div className="workspace-tabs">
        {items.map((item) => (
          <button
            className={`workspace-tabs__item ${item.id === activeId ? 'workspace-tabs__item--active' : ''}`}
            key={item.id}
            type="button"
            onClick={() => onSelect(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
    </div>
  );
}

function SelectedCustomerWorkspace({
  customer,
  customerCardError,
  operationTabs,
  actionTabs,
  activeOperationType,
  activeOperationAction,
  workspace,
  metrics,
  metricsError,
  onOperationTypeChange,
  onOperationActionChange,
  onCustomerChanged,
  onCreateCustomer
}: {
  customer: SelectedCustomerCard;
  customerCardError: string;
  operationTabs: WorkspaceTabItem<OperationType>[];
  actionTabs: WorkspaceTabItem<OperationAction>[];
  activeOperationType?: OperationType;
  activeOperationAction?: OperationAction;
  workspace: BrandWorkspaceResponse;
  metrics: MetricResponse[];
  metricsError: string;
  onOperationTypeChange: (type: OperationType) => void;
  onOperationActionChange: (action: OperationAction) => void;
  onCustomerChanged: () => Promise<void>;
  onCreateCustomer: () => Promise<void>;
}) {
  if (customer.kind === 'draft') {
    return (
      <NewCustomerWorkspace
        customer={customer}
        workspace={workspace}
        customerCardError={customerCardError}
        onCreateCustomer={onCreateCustomer}
      />
    );
  }

  return (
    <div className="workspace-active-area">
      <section className="brand-customer-card">
        <div className="brand-customer-card__header">
          <div>
            <span className="brand-customer-card__eyebrow">Клиент</span>
            <h3>{customer.customerName}</h3>
            <p>{formatRuPhoneInput(customer.customerPhoneNumber)}</p>
          </div>
        </div>

        {customerCardError ? <p className="form-status form-status--error">{customerCardError}</p> : null}
        <WalletBrandDetailsBlock details={customer.details} ariaLabel="Разделы карточки клиента" />
      </section>

      <section className="brand-customer-operations">
        <div className="section-heading">
          <h2>Операции</h2>
        </div>

        {activeOperationType && activeOperationAction ? (
          <OperationWorkspace
            workspace={workspace}
            customer={customer}
            metrics={metrics}
            metricsError={metricsError}
            operationTabs={operationTabs}
            actionTabs={actionTabs}
            activeOperationType={activeOperationType}
            activeOperationAction={activeOperationAction}
            onOperationTypeChange={onOperationTypeChange}
            onOperationActionChange={onOperationActionChange}
            onCustomerChanged={onCustomerChanged}
          />
        ) : (
          <section className="operation-panel">
            <p className="muted-text">Для этого бренда нет доступных операций.</p>
          </section>
        )}
      </section>
    </div>
  );
}

function NewCustomerWorkspace({
  customer,
  workspace,
  customerCardError,
  onCreateCustomer
}: {
  customer: DraftCustomerCard;
  workspace: BrandWorkspaceResponse;
  customerCardError: string;
  onCreateCustomer: () => Promise<void>;
}) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const willIssueWelcomeRewards = hasConfiguredWelcomeRewards(workspace);

  async function submitCreateOnly() {
    setIsSubmitting(true);
    setError('');

    try {
      await onCreateCustomer();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="workspace-active-area">
      <section className="brand-customer-card">
        <div className="brand-customer-card__header">
          <div>
            <span className="brand-customer-card__eyebrow">Новый клиент</span>
            <h3>{customer.customerName}</h3>
            <p>{formatRuPhoneInput(customer.customerPhoneNumber)}</p>
          </div>
        </div>

        <div className="brand-customer-card__draft">
          <p>Клиент с этим телефоном ещё не создан.</p>
          <p>Начисления и другие операции будут доступны только после явного создания клиента.</p>
        </div>

        <div className="work-form">
          <div className="form-actions">
            <button type="button" disabled={isSubmitting} onClick={() => void submitCreateOnly()}>
              {willIssueWelcomeRewards
                ? 'Создать клиента и выдать приветственные награды'
                : 'Создать клиента'}
            </button>
          </div>
        </div>

        {customerCardError ? <p className="form-status form-status--error">{customerCardError}</p> : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}
      </section>
    </div>
  );
}

function hasConfiguredWelcomeRewards(workspace: BrandWorkspaceResponse): boolean {
  const hasMetrics = workspace.isMetricsEnabled && workspace.welcomeRewards.metrics.length > 0;
  const hasCoins = workspace.isCoinsEnabled && workspace.welcomeRewards.coinsAmount > 0;

  return workspace.welcomeRewards.isEnabled && (hasMetrics || hasCoins);
}

function OperationWorkspace({
  workspace,
  customer,
  metrics,
  metricsError,
  operationTabs,
  actionTabs,
  activeOperationType,
  activeOperationAction,
  onOperationTypeChange,
  onOperationActionChange,
  onCustomerChanged
}: {
  workspace: BrandWorkspaceResponse;
  customer: SelectedCustomerCard;
  metrics: MetricResponse[];
  metricsError: string;
  operationTabs: WorkspaceTabItem<OperationType>[];
  actionTabs: WorkspaceTabItem<OperationAction>[];
  activeOperationType: OperationType;
  activeOperationAction: OperationAction;
  onOperationTypeChange: (type: OperationType) => void;
  onOperationActionChange: (action: OperationAction) => void;
  onCustomerChanged: () => Promise<void>;
}) {
  return (
    <div className="workspace-active-area">
      <WorkspaceTabs items={operationTabs} activeId={activeOperationType} onSelect={onOperationTypeChange} />
      <WorkspaceTabs items={actionTabs} activeId={activeOperationAction} onSelect={onOperationActionChange} />

      {activeOperationType === 'metrics' && activeOperationAction === 'issue' ? (
        <IssueMetricPanel
          customer={customer}
          metrics={metrics}
          metricsError={metricsError}
          onCustomerChanged={onCustomerChanged}
        />
      ) : null}
      {activeOperationType === 'metrics' && activeOperationAction === 'redeem' ? (
        <RedeemMetricPanel brandId={workspace.brandId} onCustomerChanged={onCustomerChanged} />
      ) : null}
      {activeOperationType === 'coins' && activeOperationAction === 'issue' ? (
        <IssueCoinsPanel brandId={workspace.brandId} customer={customer} onCustomerChanged={onCustomerChanged} />
      ) : null}
      {activeOperationType === 'coins' && activeOperationAction === 'purchase' ? (
        <PurchaseCoinProductPanel brandId={workspace.brandId} onCustomerChanged={onCustomerChanged} />
      ) : null}
      {activeOperationType === 'coins' && activeOperationAction === 'redeem' ? (
        <RedeemCoinsPanel brandId={workspace.brandId} onCustomerChanged={onCustomerChanged} />
      ) : null}
    </div>
  );
}

function BrandSettingsPage({
  workspace,
  managementTabs,
  onMetricsChanged,
  onWorkspaceUpdated
}: {
  workspace: BrandWorkspaceResponse;
  managementTabs: WorkspaceTabItem<ManagementType>[];
  onMetricsChanged?: () => Promise<void>;
  onWorkspaceUpdated: (workspace: BrandWorkspaceResponse) => void;
}) {
  const [activeManagementType, setActiveManagementType] = useState<ManagementType>('metrics');
  const currentManagementType = managementTabs.some((tab) => tab.id === activeManagementType)
    ? activeManagementType
    : managementTabs[0]?.id;

  useEffect(() => {
    if (currentManagementType && currentManagementType !== activeManagementType) {
      setActiveManagementType(currentManagementType);
    }
  }, [activeManagementType, currentManagementType]);

  return (
    <div className="brand-workspace-page brand-workspace-console">
      <section className="brand-workspace-hero">
        <div>
          <h2>Управление брендом</h2>
          <p>{workspace.brandName}</p>
        </div>
      </section>

      {currentManagementType ? (
        <ManagementWorkspace
          workspace={workspace}
          managementTabs={managementTabs}
          activeManagementType={currentManagementType}
          onManagementTypeChange={setActiveManagementType}
          onMetricsChanged={onMetricsChanged}
          onWorkspaceUpdated={onWorkspaceUpdated}
        />
      ) : (
        <section className="operation-panel">
          <p className="muted-text">Для этого бренда нет доступных настроек.</p>
        </section>
      )}
    </div>
  );
}

function ManagementWorkspace({
  workspace,
  managementTabs,
  activeManagementType,
  onManagementTypeChange,
  onMetricsChanged,
  onWorkspaceUpdated
}: {
  workspace: BrandWorkspaceResponse;
  managementTabs: WorkspaceTabItem<ManagementType>[];
  activeManagementType: ManagementType;
  onManagementTypeChange: (type: ManagementType) => void;
  onMetricsChanged?: () => Promise<void>;
  onWorkspaceUpdated: (workspace: BrandWorkspaceResponse) => void;
}) {
  return (
    <div className="workspace-active-area">
      <WorkspaceTabs items={managementTabs} activeId={activeManagementType} onSelect={onManagementTypeChange} />

      {activeManagementType === 'metrics' ? (
        <MetricManagementPanel brandId={workspace.brandId} onMetricsChanged={onMetricsChanged} />
      ) : null}
      {activeManagementType === 'products' ? (
        <CoinProductManagementPanel brandId={workspace.brandId} />
      ) : null}
      {activeManagementType === 'staff' ? (
        <StaffManagementPanel brandId={workspace.brandId} />
      ) : null}
      {activeManagementType === 'brand' ? (
        <BrandSettingsPanel workspace={workspace} onWorkspaceUpdated={onWorkspaceUpdated} />
      ) : null}
    </div>
  );
}

function getOperationTabs(workspace: BrandWorkspaceResponse): WorkspaceTabItem<OperationType>[] {
  return [
    workspace.isMetricsEnabled && (workspace.canIssue || workspace.canRedeem)
      ? { id: 'metrics', label: 'Штампы' }
      : null,
    workspace.isCoinsEnabled
      && (
        workspace.canIssue
        || (workspace.canRedeem && workspace.isCoinProductRedemptionEnabled)
        || (workspace.canRedeem && workspace.isManualCoinRedemptionEnabled)
      )
      ? { id: 'coins', label: 'Монетки' }
      : null
  ].filter((tab): tab is WorkspaceTabItem<OperationType> => tab !== null);
}

function getOperationActionTabs(
  workspace: BrandWorkspaceResponse,
  operationType: OperationType
): WorkspaceTabItem<OperationAction>[] {
  if (operationType === 'metrics') {
    return [
      workspace.isMetricsEnabled && workspace.canIssue ? { id: 'issue', label: 'Выдать' } : null,
      workspace.isMetricsEnabled && workspace.canRedeem ? { id: 'redeem', label: 'Списать' } : null
    ].filter((tab): tab is WorkspaceTabItem<OperationAction> => tab !== null);
  }

  if (operationType === 'coins') {
    return [
      workspace.isCoinsEnabled && workspace.canIssue ? { id: 'issue', label: 'Начислить' } : null,
      workspace.isCoinsEnabled && workspace.canRedeem && workspace.isCoinProductRedemptionEnabled
        ? { id: 'purchase', label: 'Выдать товар' }
        : null,
      workspace.isCoinsEnabled && workspace.canRedeem && workspace.isManualCoinRedemptionEnabled
        ? { id: 'redeem', label: 'Списать вручную' }
        : null
    ].filter((tab): tab is WorkspaceTabItem<OperationAction> => tab !== null);
  }

  return [{ id: 'issue', label: 'Выдать' }];
}

function getManagementTabs({
  showMetricManagement,
  showCoinProductManagement,
  showStaffManagement,
  showBrandSettings
}: {
  showMetricManagement: boolean;
  showCoinProductManagement: boolean;
  showStaffManagement: boolean;
  showBrandSettings: boolean;
}): WorkspaceTabItem<ManagementType>[] {
  return [
    showMetricManagement ? { id: 'metrics', label: 'Штампы' } : null,
    showCoinProductManagement ? { id: 'products', label: 'Товары' } : null,
    showStaffManagement ? { id: 'staff', label: 'Сотрудники' } : null,
    showBrandSettings ? { id: 'brand', label: 'Бренд' } : null
  ].filter((tab): tab is WorkspaceTabItem<ManagementType> => tab !== null);
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
      setError('Укажите название штампа и положительное количество для списания.');
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
      setStatus('Штамп создан.');
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
      setError('Укажите название штампа и положительное количество для списания.');
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
      setStatus('Штамп обновлен.');
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
            <h2>Управление штампами</h2>
          </div>
          <p>Создание и редактирование штампов бренда.</p>
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

      {isLoading ? <p className="muted-text">Загружаем штампы...</p> : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}

      {!isLoading && metrics.length === 0 ? (
        <p className="muted-text">Штампов пока нет.</p>
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
  const [isWelcomeRewardsEnabled, setIsWelcomeRewardsEnabled] = useState(workspace.welcomeRewards.isEnabled);
  const [welcomeMetrics, setWelcomeMetrics] = useState(
    workspace.welcomeRewards.metrics.map((metric) => ({
      metricDefinitionId: metric.metricDefinitionId,
      amount: String(metric.amount)
    }))
  );
  const [welcomeCoinsAmount, setWelcomeCoinsAmount] = useState(
    workspace.welcomeRewards.coinsAmount > 0 ? String(workspace.welcomeRewards.coinsAmount) : ''
  );
  const [welcomeRewardComment, setWelcomeRewardComment] = useState(workspace.welcomeRewards.comment);
  const [settingsMetrics, setSettingsMetrics] = useState<MetricResponse[]>([]);
  const [settingsMetricsError, setSettingsMetricsError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  useEffect(() => {
    setIsMetricsEnabled(workspace.isMetricsEnabled);
    setIsCoinsEnabled(workspace.isCoinsEnabled);
    setIsCoinProductRedemptionEnabled(workspace.isCoinProductRedemptionEnabled);
    setIsManualCoinRedemptionEnabled(workspace.isManualCoinRedemptionEnabled);
    setIsWelcomeRewardsEnabled(workspace.welcomeRewards.isEnabled);
    setWelcomeMetrics(workspace.welcomeRewards.metrics.map((metric) => ({
      metricDefinitionId: metric.metricDefinitionId,
      amount: String(metric.amount)
    })));
    setWelcomeCoinsAmount(workspace.welcomeRewards.coinsAmount > 0 ? String(workspace.welcomeRewards.coinsAmount) : '');
    setWelcomeRewardComment(workspace.welcomeRewards.comment);
  }, [
    workspace.brandId,
    workspace.isMetricsEnabled,
    workspace.isCoinsEnabled,
    workspace.isCoinProductRedemptionEnabled,
    workspace.isManualCoinRedemptionEnabled,
    workspace.welcomeRewards
  ]);

  useEffect(() => {
    if (!workspace.canManageMetrics) {
      setSettingsMetrics([]);
      return;
    }

    let isCurrent = true;
    setSettingsMetricsError('');

    getManageMetrics(workspace.brandId)
      .then((response) => {
        if (isCurrent) {
          setSettingsMetrics(response.filter((metric) => metric.isActive));
        }
      })
      .catch((requestError) => {
        if (isCurrent) {
          setSettingsMetrics([]);
          setSettingsMetricsError(getUserMessage(requestError));
        }
      });

    return () => {
      isCurrent = false;
    };
  }, [workspace.brandId, workspace.canManageMetrics]);

  useEffect(() => {
    setError('');
    setStatus('');
  }, [workspace.brandId]);

  const parsedWelcomeCoinsAmount = welcomeCoinsAmount.trim() ? Number(welcomeCoinsAmount) : 0;
  const requestWelcomeMetrics = isMetricsEnabled
    ? welcomeMetrics
        .map((metric) => ({
          metricDefinitionId: metric.metricDefinitionId,
          amount: Number(metric.amount)
        }))
        .filter((metric) => metric.metricDefinitionId)
    : [];
  const requestWelcomeCoinsAmount = isCoinsEnabled && Number.isInteger(parsedWelcomeCoinsAmount)
    ? parsedWelcomeCoinsAmount
    : 0;
  const canSave = (isMetricsEnabled || isCoinsEnabled)
    && (!isCoinsEnabled || isCoinProductRedemptionEnabled || isManualCoinRedemptionEnabled)
    && Number.isInteger(parsedWelcomeCoinsAmount)
    && parsedWelcomeCoinsAmount >= 0
    && requestWelcomeMetrics.every((metric) => Number.isInteger(metric.amount) && metric.amount > 0)
    && (!isWelcomeRewardsEnabled || requestWelcomeMetrics.length > 0 || requestWelcomeCoinsAmount > 0);

  async function submit() {
    if (!canSave) {
      setError('Проверьте типы наград, способы списания и состав приветственных наград.');
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
        isManualCoinRedemptionEnabled,
        welcomeRewards: {
          isEnabled: isWelcomeRewardsEnabled,
          metrics: requestWelcomeMetrics,
          coinsAmount: requestWelcomeCoinsAmount,
          comment: welcomeRewardComment.trim() || undefined
        }
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
          <p>Включение штампов, монеток и способов списания.</p>
        </div>
        <button type="button" disabled={isSubmitting || !canSave} onClick={() => void submit()}>
          <Save size={17} />
          Сохранить
        </button>
      </div>

      <div className="settings-grid">
        <ToggleRow
          title="Учитывать штампы"
          description="Штампы доступны клиентам и сотрудникам."
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

      <div className="welcome-rewards-settings">
        <ToggleRow
          title="Приветственные награды"
          description="Награды будут выданы автоматически при ручном создании нового клиента."
          checked={isWelcomeRewardsEnabled}
          onChange={setIsWelcomeRewardsEnabled}
        />

        {isWelcomeRewardsEnabled ? (
          <div className="welcome-rewards-settings__body">
            {isMetricsEnabled && workspace.canManageMetrics ? (
              <div className="work-form">
                <span className="field-label">Штампы</span>
                {settingsMetricsError ? <p className="form-status form-status--error">{settingsMetricsError}</p> : null}
                {settingsMetrics.length === 0 ? (
                  <p className="muted-text">Активных штампов пока нет.</p>
                ) : (
                  <div className="settings-check-list">
                    {settingsMetrics.map((metric) => (
                      <label className="settings-check-row" key={metric.id}>
                        <input
                          type="checkbox"
                          checked={welcomeMetrics.some((reward) => reward.metricDefinitionId === metric.id)}
                          onChange={(event) => {
                            setWelcomeMetrics((current) => event.target.checked
                              ? [...current, { metricDefinitionId: metric.id, amount: '1' }]
                              : current.filter((reward) => reward.metricDefinitionId !== metric.id));
                          }}
                        />
                        <span>{metric.name}</span>
                        {welcomeMetrics.some((reward) => reward.metricDefinitionId === metric.id) ? (
                          <input
                            aria-label={`Количество штампов ${metric.name}`}
                            inputMode="numeric"
                            value={welcomeMetrics.find((reward) => reward.metricDefinitionId === metric.id)?.amount ?? '1'}
                            onChange={(event) => {
                              setWelcomeMetrics((current) => current.map((reward) => reward.metricDefinitionId === metric.id
                                ? { ...reward, amount: event.target.value }
                                : reward));
                            }}
                          />
                        ) : null}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            ) : null}

            {isCoinsEnabled ? (
              <div className="work-form">
                <label>
                  Монетки
                  <input
                    value={welcomeCoinsAmount}
                    inputMode="numeric"
                    onChange={(event) => setWelcomeCoinsAmount(event.target.value)}
                    placeholder="0"
                  />
                </label>
              </div>
            ) : null}

            <div className="work-form">
              <label>
                Комментарий
                <input
                  value={welcomeRewardComment}
                  maxLength={200}
                  onChange={(event) => setWelcomeRewardComment(event.target.value)}
                />
              </label>
            </div>
          </div>
        ) : null}
      </div>

      {!canSave ? (
        <p className="form-status form-status--error">
          Проверьте типы наград, способы списания и состав приветственных наград.
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
    isManualCoinRedemptionEnabled: response.isManualCoinRedemptionEnabled,
    welcomeRewards: response.welcomeRewards
  };
}

function IssueMetricPanel({
  customer,
  metrics,
  metricsError,
  onCustomerChanged
}: {
  customer: CustomerOperationTarget;
  metrics: MetricResponse[];
  metricsError: string;
  onCustomerChanged: () => Promise<void>;
}) {
  const [metricId, setMetricId] = useState('');
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
    if (!metricId || !Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Выберите штамп и укажите положительное количество.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await issueMetricByPhone(metricId, {
        phoneNumber: customer.customerPhoneNumber,
        amount: parsedAmount,
        comment: comment.trim() || undefined
      });
      setResult({ kind: 'metric', title: 'Штамп выдан', response });
      setAmount('');
      setComment('');
      await onCustomerChanged();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <OperationPanel icon={<BadgePlus size={20} />} title="Выдать штамп">
      {metricsError ? <p className="form-status form-status--error">{metricsError}</p> : null}
      <div className="work-form">
        <label>
          Штамп
          <select value={metricId} onChange={(event) => setMetricId(event.target.value)}>
            {metrics.map((metric) => (
              <option key={metric.id} value={metric.id}>
                {metric.name}
              </option>
            ))}
          </select>
        </label>
        <p className="operation-options__customer">Клиент: {customer.customerName}</p>
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
        </div>
      </div>
      <OperationFeedback error={error} result={result} />
    </OperationPanel>
  );
}

function RedeemMetricPanel({
  brandId,
  onCustomerChanged
}: {
  brandId: string;
  onCustomerChanged: () => Promise<void>;
}) {
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
      setResult({ kind: 'metric', title: 'Штамп списан', response });
      setOptions(null);
      setRedemptionCode('');
      await onCustomerChanged();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmittingId('');
    }
  }

  return (
    <OperationPanel icon={<TicketMinus size={20} />} title="Списать штамп">
      <div className="work-form">
        <label>
          Код списания клиента
          <input value={redemptionCode} inputMode="numeric" onChange={(event) => setRedemptionCode(event.target.value)} />
        </label>
        <button className="button-secondary" type="button" disabled={isLoading} onClick={() => void loadOptions()}>
          <Search size={17} />
          Показать штампы
        </button>
      </div>

      {options ? (
        <div className="operation-options">
          <p className="operation-options__customer">Клиент: {options.customerName}</p>
          {options.metrics.length === 0 ? <p className="muted-text">Доступных штампов нет.</p> : null}
          {[...options.metrics].sort(compareRedeemableMetrics).map((metric) => (
            <div className="operation-option" key={metric.metricDefinitionId}>
              <div>
                <strong>{metric.metricName}</strong>
                <p>
                  Баланс {metric.currentBalance} из {metric.requiredAmount}
                </p>
                {!metric.canRedeem ? (
                  <p>Недоступно: не хватает {Math.max(metric.requiredAmount - metric.currentBalance, 0)}</p>
                ) : null}
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

function IssueCoinsPanel({
  brandId,
  customer,
  onCustomerChanged
}: {
  brandId: string;
  customer: CustomerOperationTarget;
  onCustomerChanged: () => Promise<void>;
}) {
  const [amount, setAmount] = useState('');
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<OperationResult | null>(null);

  async function submit() {
    const parsedAmount = Number(amount);
    if (!Number.isInteger(parsedAmount) || parsedAmount <= 0) {
      setError('Укажите положительное количество монеток.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await issueCoinsByPhone(brandId, {
        phoneNumber: customer.customerPhoneNumber,
        amount: parsedAmount,
        comment: comment.trim() || undefined
      });
      setResult({ kind: 'coins', title: 'Монетки начислены', response });
      setAmount('');
      setComment('');
      await onCustomerChanged();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <OperationPanel icon={<Coins size={20} />} title="Начислить монетки">
      <div className="work-form">
        <p className="operation-options__customer">Клиент: {customer.customerName}</p>
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

function RedeemCoinsPanel({
  brandId,
  onCustomerChanged
}: {
  brandId: string;
  onCustomerChanged: () => Promise<void>;
}) {
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
      await onCustomerChanged();
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

function PurchaseCoinProductPanel({
  brandId,
  onCustomerChanged
}: {
  brandId: string;
  onCustomerChanged: () => Promise<void>;
}) {
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
      setResult({ kind: 'coins', title: 'Товар выдан', response });
      setOptions(null);
      setRedemptionCode('');
      await onCustomerChanged();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmittingId('');
    }
  }

  return (
    <OperationPanel icon={<Gift size={20} />} title="Выдать товар">
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
          {[...options.products].sort(comparePurchasableProducts).map((product) => (
            <div className="operation-option" key={product.productId}>
              <div>
                <strong>{product.productName}</strong>
                <p>
                  Цена {product.price}, баланс {product.currentBalance}
                </p>
                {!product.canPurchase ? (
                  <p>Недоступно: не хватает {Math.max(product.price - product.currentBalance, 0)}</p>
                ) : null}
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

function createDraftCustomerCard(brandId: string, customerPhoneNumber: string): DraftCustomerCard {
  return {
    kind: 'draft',
    brandId,
    customerName: 'Новый клиент',
    customerPhoneNumber
  };
}

function formatRoleName(roleSystemName: string): string {
  const normalizedRole = roleSystemName.trim().toUpperCase();

  if (normalizedRole === 'OWNER') {
    return 'Владелец';
  }

  if (normalizedRole === 'STAFF') {
    return 'Сотрудник';
  }

  return 'Участник';
}

function compareRedeemableMetrics(
  left: { canRedeem: boolean; metricName: string },
  right: { canRedeem: boolean; metricName: string }
): number {
  if (left.canRedeem !== right.canRedeem) {
    return left.canRedeem ? -1 : 1;
  }

  return left.metricName.localeCompare(right.metricName, 'ru');
}

function comparePurchasableProducts(
  left: { canPurchase: boolean; productName: string },
  right: { canPurchase: boolean; productName: string }
): number {
  if (left.canPurchase !== right.canPurchase) {
    return left.canPurchase ? -1 : 1;
  }

  return left.productName.localeCompare(right.productName, 'ru');
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
