import { Building2, ClipboardList, Plus, RefreshCw, Search, ShieldCheck, Smartphone, UserRoundCheck, X } from 'lucide-react';
import { type ReactNode, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { getApiErrorMessage } from '../api/errorMessages';
import { useAuth } from '../auth/AuthContext';
import { formatRuPhoneInput, isRuPhoneInputComplete } from '../validation/phoneNumber';
import {
  createBrandWithOwner,
  createDemoBrands,
  createUserDemoData,
  getAdminBrands,
  getBusinessAuditLogs,
  getPhoneAuthSmsSettings,
  reassignBrandOwner,
  resetDemoDatabase,
  updatePhoneAuthSmsSettings,
  type AdminBrandResponse,
  type BusinessAuditLogResponse
} from './adminApi';

type AdminTab = 'brands' | 'audit' | 'settings' | 'demo';

export function AdminPage() {
  const auth = useAuth();
  const [brands, setBrands] = useState<AdminBrandResponse[]>([]);
  const [activeTab, setActiveTab] = useState<AdminTab>('brands');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isCreateSheetOpen, setIsCreateSheetOpen] = useState(false);
  const [ownerSheetBrand, setOwnerSheetBrand] = useState<AdminBrandResponse | null>(null);

  async function loadBrands() {
    setIsLoading(true);
    setError('');
    try {
      setBrands(await getAdminBrands());
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadBrands();
  }, []);

  return (
    <div className="admin-page">
      <AdminTabs activeTab={activeTab} onChange={setActiveTab} />

      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}

      {activeTab === 'brands' ? (
        <AdminBrandsTab
          brands={brands}
          isLoading={isLoading}
          onCreateBrand={() => setIsCreateSheetOpen(true)}
          onReassignOwner={setOwnerSheetBrand}
        />
      ) : null}

      {activeTab === 'audit' ? <AdminAuditTab brands={brands} /> : null}

      {activeTab === 'settings' ? <AdminSettingsTab /> : null}

      {activeTab === 'demo' ? (
        <AdminDemoTab
          brands={brands}
          onChanged={async (message) => {
            setStatus(message);
            await loadBrands();
          }}
          onResetCompleted={() => {
            setStatus('База очищена. Нужно войти заново.');
            auth.signOut();
          }}
        />
      ) : null}

      {isCreateSheetOpen ? (
        <CreateBrandSheet
          onClose={() => setIsCreateSheetOpen(false)}
          onCreated={async (brandName) => {
            setStatus(`Бренд "${brandName}" создан.`);
            await loadBrands();
            setIsCreateSheetOpen(false);
          }}
        />
      ) : null}

      {ownerSheetBrand ? (
        <ReassignOwnerSheet
          brand={ownerSheetBrand}
          onClose={() => setOwnerSheetBrand(null)}
          onOwnerChanged={async (brandName, ownerPhoneNumber) => {
            setStatus(`Владелец бренда "${brandName}" обновлен: ${ownerPhoneNumber}.`);
            await loadBrands();
            setOwnerSheetBrand(null);
          }}
        />
      ) : null}
    </div>
  );
}

function AdminTabs({ activeTab, onChange }: { activeTab: AdminTab; onChange: (tab: AdminTab) => void }) {
  return (
    <div className="admin-tabs" role="tablist" aria-label="Разделы админки">
      <button
        className={activeTab === 'brands' ? 'admin-tabs__item admin-tabs__item--active' : 'admin-tabs__item'}
        type="button"
        role="tab"
        aria-selected={activeTab === 'brands'}
        onClick={() => onChange('brands')}
      >
        Бренды
      </button>
      <button
        className={activeTab === 'audit' ? 'admin-tabs__item admin-tabs__item--active' : 'admin-tabs__item'}
        type="button"
        role="tab"
        aria-selected={activeTab === 'audit'}
        onClick={() => onChange('audit')}
      >
        Журнал
      </button>
      <button
        className={activeTab === 'settings' ? 'admin-tabs__item admin-tabs__item--active' : 'admin-tabs__item'}
        type="button"
        role="tab"
        aria-selected={activeTab === 'settings'}
        onClick={() => onChange('settings')}
      >
        Настройки
      </button>
      <button
        className={activeTab === 'demo' ? 'admin-tabs__item admin-tabs__item--active' : 'admin-tabs__item'}
        type="button"
        role="tab"
        aria-selected={activeTab === 'demo'}
        onClick={() => onChange('demo')}
      >
        Демо
      </button>
    </div>
  );
}

function AdminSettingsTab() {
  const [isSmsEnabled, setIsSmsEnabled] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  async function loadSettings() {
    setIsLoading(true);
    setError('');
    try {
      const response = await getPhoneAuthSmsSettings();
      setIsSmsEnabled(response.isEnabled);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadSettings();
  }, []);

  async function toggleSms(nextValue: boolean) {
    setIsSaving(true);
    setError('');
    setStatus('');
    try {
      const response = await updatePhoneAuthSmsSettings(nextValue);
      setIsSmsEnabled(response.isEnabled);
      setStatus(response.isEnabled ? 'SMS-коды входа включены.' : 'SMS-коды входа выключены.');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section className="admin-tab-panel" aria-label="Настройки">
      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {status ? <p className="form-status form-status--ok">{status}</p> : null}

      <div className="admin-demo-grid">
        <article className="admin-demo-card">
          <div className="admin-demo-card__header">
            <Smartphone size={20} />
            <h3>SMS-коды входа</h3>
          </div>
          <p>
            {isSmsEnabled
              ? 'Коды входа отправляются клиенту по SMS и администратору в Telegram.'
              : 'Коды входа отправляются только администратору в Telegram.'}
          </p>
          <label className="admin-switch-row">
            <input
              type="checkbox"
              checked={isSmsEnabled}
              disabled={isLoading || isSaving}
              onChange={(event) => void toggleSms(event.target.checked)}
            />
            <span className="admin-switch-row__control" aria-hidden="true" />
            <span className="admin-switch-row__label">
              {isSmsEnabled ? 'SMS включены' : 'SMS выключены'}
            </span>
          </label>
          {isLoading ? <p className="muted-text">Загружаем настройки...</p> : null}
        </article>
      </div>
    </section>
  );
}

function AdminBrandsTab({
  brands,
  isLoading,
  onCreateBrand,
  onReassignOwner
}: {
  brands: AdminBrandResponse[];
  isLoading: boolean;
  onCreateBrand: () => void;
  onReassignOwner: (brand: AdminBrandResponse) => void;
}) {
  const [searchQuery, setSearchQuery] = useState('');
  const normalizedQuery = searchQuery.trim().toLowerCase();
  const filteredBrands = useMemo(() => {
    if (!normalizedQuery) {
      return brands;
    }

    return brands.filter((brand) =>
      [brand.brandName, brand.ownerName, brand.ownerPhoneNumber]
        .filter(Boolean)
        .some((value) => value?.toLowerCase().includes(normalizedQuery))
    );
  }, [brands, normalizedQuery]);

  return (
    <section className="admin-tab-panel" aria-label="Бренды">
      <div className="admin-toolbar">
        <label className="admin-search">
          <Search size={18} aria-hidden="true" />
          <span className="sr-only">Поиск по брендам</span>
          <input
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Поиск по бренду, владельцу или телефону"
          />
        </label>
        <div className="admin-toolbar__actions">
          <button className="admin-add-brand-button" type="button" aria-label="Создать бренд" onClick={onCreateBrand}>
            <Plus size={16} />
          </button>
        </div>
      </div>

      {isLoading ? <p className="muted-text">Загружаем бренды...</p> : null}

      {!isLoading && brands.length === 0 ? (
        <div className="empty-state admin-empty-state">
          <p className="empty-state__meta">Брендов пока нет.</p>
        </div>
      ) : null}

      {!isLoading && brands.length > 0 && filteredBrands.length === 0 ? (
        <div className="empty-state admin-empty-state">
          <p className="empty-state__meta">По этому запросу бренды не найдены.</p>
        </div>
      ) : null}

      <div className="admin-brand-list">
        {filteredBrands.map((brand) => (
          <AdminBrandCard key={brand.brandId} brand={brand} onReassignOwner={() => onReassignOwner(brand)} />
        ))}
      </div>
    </section>
  );
}

function AdminBrandCard({
  brand,
  onReassignOwner
}: {
  brand: AdminBrandResponse;
  onReassignOwner: () => void;
}) {
  const ownerActionText = brand.ownerName ? 'Сменить' : 'Назначить';

  return (
    <article className="admin-brand-card">
      <div className="admin-brand-card__main">
        <div className="admin-brand-card__avatar" aria-hidden="true">
          {getBrandInitial(brand.brandName)}
        </div>
        <div className="admin-brand-card__content">
          <h3>{brand.brandName}</h3>
          <p>
            <span>Владелец:</span> {brand.ownerName || 'не назначен'}
          </p>
          {brand.ownerPhoneNumber ? (
            <p>
              <span>Телефон:</span> {brand.ownerPhoneNumber}
            </p>
          ) : null}
        </div>
        <button className="button-secondary button-compact" type="button" onClick={onReassignOwner}>
          <UserRoundCheck size={16} />
          {ownerActionText}
        </button>
      </div>
    </article>
  );
}

function CreateBrandSheet({
  onClose,
  onCreated
}: {
  onClose: () => void;
  onCreated: (brandName: string) => Promise<void>;
}) {
  const [brandName, setBrandName] = useState('');
  const [ownerPhoneNumber, setOwnerPhoneNumber] = useState(formatRuPhoneInput(''));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function submit() {
    setIsSubmitting(true);
    setError('');
    try {
      const response = await createBrandWithOwner({
        brandName: brandName.trim(),
        ownerPhoneNumber: ownerPhoneNumber.trim()
      });
      setBrandName('');
      setOwnerPhoneNumber(formatRuPhoneInput(''));
      await onCreated(response.brandName);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AdminBottomSheet title="Создать бренд" onClose={onClose}>
      <div className="admin-sheet-form">
        <label>
          Название бренда
          <input value={brandName} onChange={(event) => setBrandName(event.target.value)} />
        </label>
        <label>
          Телефон владельца
          <input
            value={ownerPhoneNumber}
            onChange={(event) => setOwnerPhoneNumber(formatRuPhoneInput(event.target.value))}
          />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submit()}>
          Создать
        </button>
      </div>
      {error ? <p className="form-status form-status--error">{error}</p> : null}
    </AdminBottomSheet>
  );
}

function ReassignOwnerSheet({
  brand,
  onClose,
  onOwnerChanged
}: {
  brand: AdminBrandResponse;
  onClose: () => void;
  onOwnerChanged: (brandName: string, ownerPhoneNumber: string) => Promise<void>;
}) {
  const [ownerPhoneNumber, setOwnerPhoneNumber] = useState(formatRuPhoneInput(''));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const title = brand.ownerName ? 'Сменить владельца' : 'Назначить владельца';

  async function submit() {
    setIsSubmitting(true);
    setError('');
    try {
      const response = await reassignBrandOwner(brand.brandId, ownerPhoneNumber.trim());
      setOwnerPhoneNumber(formatRuPhoneInput(''));
      await onOwnerChanged(brand.brandName, response.newOwnerPhoneNumber);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AdminBottomSheet title={title} subtitle={brand.brandName} onClose={onClose}>
      <div className="admin-sheet-form">
        <label>
          Новый владелец
          <input
            value={ownerPhoneNumber}
            onChange={(event) => setOwnerPhoneNumber(formatRuPhoneInput(event.target.value))}
          />
        </label>
        <button type="button" disabled={isSubmitting} onClick={() => void submit()}>
          Назначить
        </button>
      </div>
      {error ? <p className="form-status form-status--error">{error}</p> : null}
    </AdminBottomSheet>
  );
}

function AdminBottomSheet({
  title,
  subtitle,
  onClose,
  children
}: {
  title: string;
  subtitle?: string;
  onClose: () => void;
  children: ReactNode;
}) {
  return (
    <div className="admin-sheet-backdrop" role="presentation" onClick={onClose}>
      <section
        className="admin-bottom-sheet"
        role="dialog"
        aria-modal="true"
        aria-labelledby="admin-sheet-title"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="admin-bottom-sheet__handle" aria-hidden="true" />
        <div className="admin-bottom-sheet__header">
          <div>
            <h3 id="admin-sheet-title">{title}</h3>
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          <button className="button-secondary button-compact" type="button" onClick={onClose}>
            <X size={16} />
            Закрыть
          </button>
        </div>
        <div className="admin-bottom-sheet__content">{children}</div>
      </section>
    </div>
  );
}

function AdminAuditTab({ brands }: { brands: AdminBrandResponse[] }) {
  const [occurredFrom, setOccurredFrom] = useState('');
  const [occurredTo, setOccurredTo] = useState('');
  const [brandId, setBrandId] = useState('');
  const [customerPhoneNumber, setCustomerPhoneNumber] = useState('');
  const [actorName, setActorName] = useState('');
  const [operationType, setOperationType] = useState('');
  const [operationStatus, setOperationStatus] = useState('');
  const [take, setTake] = useState(50);
  const [logs, setLogs] = useState<BusinessAuditLogResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const requestSequenceRef = useRef(0);
  const isPhoneFilterIncomplete = customerPhoneNumber.trim().length > 0
    && !isRuPhoneInputComplete(customerPhoneNumber);

  const loadLogs = useCallback(async () => {
    if (isPhoneFilterIncomplete) {
      return;
    }

    const requestId = requestSequenceRef.current + 1;
    requestSequenceRef.current = requestId;
    setIsLoading(true);
    setError('');
    try {
      const response = await getBusinessAuditLogs({
        occurredFromUtc: toStartOfDayUtc(occurredFrom),
        occurredToUtc: toEndOfDayUtc(occurredTo),
        brandId,
        customerPhoneNumber: isRuPhoneInputComplete(customerPhoneNumber) ? customerPhoneNumber : undefined,
        actorName,
        operationType,
        operationStatus,
        take
      });
      if (requestSequenceRef.current !== requestId) {
        return;
      }

      setLogs(response.items);
      setTotalCount(response.totalCount);
      setLastUpdatedAt(new Date());
    } catch (requestError) {
      if (requestSequenceRef.current !== requestId) {
        return;
      }

      setError(getUserMessage(requestError));
    } finally {
      if (requestSequenceRef.current === requestId) {
        setIsLoading(false);
      }
    }
  }, [
    actorName,
    brandId,
    customerPhoneNumber,
    isPhoneFilterIncomplete,
    occurredFrom,
    occurredTo,
    operationStatus,
    operationType,
    take
  ]);

  useEffect(() => {
    if (isPhoneFilterIncomplete) {
      requestSequenceRef.current += 1;
      setIsLoading(false);
      return;
    }

    const delayMs = actorName.trim() || customerPhoneNumber.trim() ? 500 : 0;
    const timeoutId = window.setTimeout(() => {
      void loadLogs();
    }, delayMs);

    return () => window.clearTimeout(timeoutId);
  }, [actorName, customerPhoneNumber, isPhoneFilterIncomplete, loadLogs]);

  function resetFilters() {
    setOccurredFrom('');
    setOccurredTo('');
    setBrandId('');
    setCustomerPhoneNumber('');
    setActorName('');
    setOperationType('');
    setOperationStatus('');
    setTake(50);
  }

  return (
    <section className="admin-tab-panel" aria-label="Журнал операций">
      <div className="admin-audit-filters">
        <label>
          С
          <input type="date" value={occurredFrom} onChange={(event) => setOccurredFrom(event.target.value)} />
        </label>
        <label>
          По
          <input type="date" value={occurredTo} onChange={(event) => setOccurredTo(event.target.value)} />
        </label>
        <label>
          Бренд
          <select value={brandId} onChange={(event) => setBrandId(event.target.value)}>
            <option value="">Все бренды</option>
            {brands.map((brand) => (
              <option key={brand.brandId} value={brand.brandId}>
                {brand.brandName}
              </option>
            ))}
          </select>
        </label>
        <label>
          Клиент по телефону
          <input
            value={customerPhoneNumber}
            placeholder="+7 (999) 123-45-67"
            onChange={(event) => setCustomerPhoneNumber(formatAuditPhoneFilter(event.target.value))}
          />
        </label>
        <label>
          Исполнитель
          <input
            value={actorName}
            onChange={(event) => setActorName(event.target.value)}
            placeholder="Имя сотрудника, владельца или админа"
          />
        </label>
        <label>
          Операция
          <select value={operationType} onChange={(event) => setOperationType(event.target.value)}>
            <option value="">Все операции</option>
            {operationTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <div className="admin-audit-filter-group">
          <span>Статус</span>
          <div className="admin-audit-status-filter" role="group" aria-label="Статус операции">
            <button
              className={operationStatus === '' ? 'admin-audit-filter-button admin-audit-filter-button--active' : 'admin-audit-filter-button'}
              type="button"
              onClick={() => setOperationStatus('')}
            >
              Все
            </button>
            {operationStatusOptions.map((option) => (
              <button
                key={option.value}
                className={operationStatus === option.value ? 'admin-audit-filter-button admin-audit-filter-button--active' : 'admin-audit-filter-button'}
                type="button"
                onClick={() => setOperationStatus(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
        <label>
          Записей
          <select value={take} onChange={(event) => setTake(Number(event.target.value))}>
            <option value={25}>25</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
            <option value={200}>200</option>
          </select>
        </label>
      </div>

      <div className="admin-audit-actions">
        <button className="button-secondary" type="button" disabled={isLoading} onClick={() => void loadLogs()}>
          <RefreshCw size={16} />
          Обновить
        </button>
        <button className="button-secondary" type="button" onClick={resetFilters}>
          <X size={16} />
          Сбросить
        </button>
      </div>

      {error ? <p className="form-status form-status--error">{error}</p> : null}
      {isPhoneFilterIncomplete ? (
        <p className="form-status form-status--error">Введите телефон клиента полностью или очистите поле.</p>
      ) : null}
      {isLoading ? <p className="muted-text">Загружаем журнал...</p> : null}
      {!isLoading && logs.length === 0 ? (
        <div className="empty-state admin-empty-state">
          <p className="empty-state__meta">Операций по этим фильтрам нет.</p>
        </div>
      ) : null}

      {!isLoading && lastUpdatedAt ? (
        <div className="admin-audit-summary">
          <span>Показано {logs.length} из {totalCount}</span>
          <span>Обновлено: {formatTime(lastUpdatedAt)}</span>
        </div>
      ) : null}

      <div className="admin-audit-list">
        {logs.map((log, index) => (
          <AdminAuditLogCard key={`${log.occurredAt}-${index}`} log={log} />
        ))}
      </div>
    </section>
  );
}

function AdminAuditLogCard({ log }: { log: BusinessAuditLogResponse }) {
  const amountText = formatAuditAmount(log);
  const balanceText = formatBalance(log);
  const reasonText = log.reasonCode ? formatReasonCode(log.reasonCode) : '';

  return (
    <article className="admin-audit-card">
      <div className="admin-audit-card__icon" aria-hidden="true">
        <ClipboardList size={18} />
      </div>
      <div className="admin-audit-card__content">
        <div className="admin-audit-card__topline">
          <span>{formatRuDateTime(log.occurredAt)}</span>
          <span className={getAuditStatusClassName(log.operationStatus)}>{log.operationStatusText}</span>
        </div>
        <h3>{log.summary}</h3>
        <div className="admin-audit-detail-grid">
          <AdminAuditDetail label="Операция" value={log.operationName} />
          <AdminAuditDetail label="Канал" value={log.channel} />
          <AdminAuditDetail label="Бренд" value={log.brandName} />
          <AdminAuditDetail label="Исполнитель" value={log.actorName} />
          <AdminAuditDetail label="Клиент" value={log.customerName} />
        </div>
        {amountText || balanceText ? (
          <div className="admin-audit-card__numbers">
            {amountText ? <span>{amountText}</span> : null}
            {balanceText ? <span>{balanceText}</span> : null}
          </div>
        ) : null}
        {reasonText ? <p className="admin-audit-card__detail">Причина: {reasonText}</p> : null}
        {log.comment ? <p className="admin-audit-card__detail">Комментарий: {log.comment}</p> : null}
      </div>
    </article>
  );
}

function AdminAuditDetail({ label, value }: { label: string; value?: string | null }) {
  if (!value) {
    return null;
  }

  return (
    <div className="admin-audit-detail">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function AdminDemoTab({
  brands,
  onChanged,
  onResetCompleted
}: {
  brands: AdminBrandResponse[];
  onChanged: (message: string) => Promise<void>;
  onResetCompleted: () => void;
}) {
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [brandId, setBrandId] = useState('');
  const [resetConfirmation, setResetConfirmation] = useState('');
  const [isSubmitting, setIsSubmitting] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (brands.length === 0) {
      setBrandId('');
      return;
    }

    if (!brands.some((brand) => brand.brandId === brandId)) {
      setBrandId(brands[0].brandId);
    }
  }, [brandId, brands]);

  async function submitCreateDemoBrands() {
    setIsSubmitting('brands');
    setError('');
    try {
      await createDemoBrands();
      await onChanged('Демо-бренды созданы.');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting('');
    }
  }

  async function submitCreateUserDemoData() {
    setIsSubmitting('user-data');
    setError('');
    try {
      await createUserDemoData({
        phoneNumber: phoneNumber.trim(),
        brandId
      });
      setPhoneNumber(formatRuPhoneInput(''));
      await onChanged('Демо-данные пользователя созданы.');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting('');
    }
  }

  async function submitReset() {
    setIsSubmitting('reset');
    setError('');
    try {
      await resetDemoDatabase();
      setResetConfirmation('');
      onResetCompleted();
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting('');
    }
  }

  return (
    <section className="admin-tab-panel" aria-label="Демо">
      {error ? <p className="form-status form-status--error">{error}</p> : null}

      <div className="admin-demo-grid">
        <article className="admin-demo-card">
          <div className="admin-demo-card__header">
            <Building2 size={20} />
            <h3>Демо-бренды</h3>
          </div>
          <p>Создает набор брендов с разными настройками, штампами и товарами.</p>
          <button type="button" disabled={isSubmitting === 'brands'} onClick={() => void submitCreateDemoBrands()}>
            Создать демо-бренды
          </button>
        </article>

        <article className="admin-demo-card">
          <div className="admin-demo-card__header">
            <UserRoundCheck size={20} />
            <h3>Данные пользователю</h3>
          </div>
          <div className="admin-sheet-form">
            <label>
              Телефон пользователя
              <input value={phoneNumber} onChange={(event) => setPhoneNumber(formatRuPhoneInput(event.target.value))} />
            </label>
            <label>
              Бренд
              <select value={brandId} onChange={(event) => setBrandId(event.target.value)}>
                {brands.map((brand) => (
                  <option key={brand.brandId} value={brand.brandId}>
                    {brand.brandName}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              disabled={!brandId || isSubmitting === 'user-data'}
              onClick={() => void submitCreateUserDemoData()}
            >
              Создать данные
            </button>
          </div>
        </article>

        <article className="admin-demo-card admin-danger-card">
          <div className="admin-demo-card__header">
            <ShieldCheck size={20} />
            <h3>Очистить всю БД</h3>
          </div>
          <p>Операция удаляет данные стенда и восстанавливает системные роли.</p>
          <div className="admin-sheet-form">
            <label>
              Подтверждение
              <input
                value={resetConfirmation}
                onChange={(event) => setResetConfirmation(event.target.value)}
                placeholder="ОЧИСТИТЬ"
              />
            </label>
            <button
              type="button"
              disabled={resetConfirmation !== 'ОЧИСТИТЬ' || isSubmitting === 'reset'}
              onClick={() => void submitReset()}
            >
              Очистить всю БД
            </button>
          </div>
        </article>
      </div>
    </section>
  );
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}

const operationTypeOptions = [
  { value: 'IssueCoins', label: 'Начисление монет' },
  { value: 'RedeemCoins', label: 'Списание монет' },
  { value: 'IssueMetric', label: 'Начисление штампа' },
  { value: 'RedeemMetric', label: 'Списание штампа' },
  { value: 'PurchaseCoinProduct', label: 'Выдача товара' },
  { value: 'AddStaff', label: 'Добавление сотрудника' },
  { value: 'UpdateRewardSettings', label: 'Настройки наград' }
];

const operationStatusOptions = [
  { value: 'Succeeded', label: 'Выполнено' },
  { value: 'Rejected', label: 'Отклонено' },
  { value: 'Failed', label: 'Ошибка' }
];

function getBrandInitial(brandName: string): string {
  const trimmedName = brandName.trim();
  return trimmedName.length > 0 ? trimmedName[0].toUpperCase() : 'Б';
}

function toStartOfDayUtc(value: string): string | undefined {
  if (!value) {
    return undefined;
  }

  return new Date(`${value}T00:00:00`).toISOString();
}

function toEndOfDayUtc(value: string): string | undefined {
  if (!value) {
    return undefined;
  }

  return new Date(`${value}T23:59:59.999`).toISOString();
}

function formatRuDateTime(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

function formatTime(value: Date): string {
  return new Intl.DateTimeFormat('ru-RU', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(value);
}

function formatAuditPhoneFilter(input: string): string {
  if (!input.trim()) {
    return '';
  }

  return formatRuPhoneInput(input);
}

function formatAuditAmount(log: BusinessAuditLogResponse): string {
  if (log.amount === null || log.amount === undefined) {
    return '';
  }

  const sign = log.operationType === 'IssueCoins' || log.operationType === 'IssueMetric' ? '+' : '-';
  const unit = log.operationType.includes('Coin') ? 'монет' : 'ед.';
  return `${sign}${log.amount} ${unit}`;
}

function formatBalance(log: BusinessAuditLogResponse): string {
  if (log.balanceAfter === null || log.balanceAfter === undefined) {
    return '';
  }

  if (log.balanceBefore === null || log.balanceBefore === undefined) {
    return `Баланс после: ${log.balanceAfter}`;
  }

  return `Баланс: ${log.balanceBefore} -> ${log.balanceAfter}`;
}

function formatReasonCode(reasonCode: string): string {
  const reasons: Record<string, string> = {
    'access.denied': 'нет доступа',
    'brand.not_found': 'бренд не найден',
    'brand.coins_disabled': 'монеты выключены',
    'brand.metrics_disabled': 'штампы выключены',
    'brand.coin_product_redemption_disabled': 'выдача товаров выключена',
    'brand.manual_coin_redemption_disabled': 'ручное списание монет выключено',
    'coin.insufficient_funds': 'недостаточно монет',
    'coin.wallet_not_found': 'кошелек не найден',
    'coin_product.inactive': 'товар неактивен',
    'coin_product.not_found': 'товар не найден',
    'metric.inactive': 'штамп неактивен',
    'metric.not_found': 'штамп не найден',
    'metric_balance.insufficient_funds': 'недостаточно баланса',
    'metric_balance.not_found': 'баланс не найден',
    'recipient.not_found': 'пользователь не найден',
    'redemption_code.invalid': 'код списания некорректен',
    'redemption_code.not_found_or_expired': 'код списания не найден или истек'
  };

  return reasons[reasonCode] ?? reasonCode;
}

function getAuditStatusClassName(status: string): string {
  if (status === 'Succeeded') {
    return 'admin-audit-status admin-audit-status--success';
  }

  if (status === 'Rejected') {
    return 'admin-audit-status admin-audit-status--rejected';
  }

  return 'admin-audit-status admin-audit-status--failed';
}
