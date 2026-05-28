import { Building2, Plus, RefreshCw, Search, ShieldCheck, UserRoundCheck, X } from 'lucide-react';
import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { getApiErrorMessage } from '../api/errorMessages';
import { useAuth } from '../auth/AuthContext';
import { formatRuPhoneInput } from '../validation/phoneNumber';
import {
  createBrandWithOwner,
  createDemoBrands,
  createUserDemoData,
  getAdminBrands,
  reassignBrandOwner,
  resetDemoDatabase,
  type AdminBrandResponse
} from './adminApi';

type AdminTab = 'brands' | 'demo';

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
          onRefresh={() => void loadBrands()}
          onReassignOwner={setOwnerSheetBrand}
        />
      ) : (
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
      )}

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

function AdminBrandsTab({
  brands,
  isLoading,
  onCreateBrand,
  onRefresh,
  onReassignOwner
}: {
  brands: AdminBrandResponse[];
  isLoading: boolean;
  onCreateBrand: () => void;
  onRefresh: () => void;
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
          <button className="button-secondary button-compact admin-icon-button" type="button" onClick={onRefresh}>
            <RefreshCw size={16} />
            <span>Обновить</span>
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
          <p>Создает набор брендов с разными настройками, метриками и товарами.</p>
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

function getBrandInitial(brandName: string): string {
  const trimmedName = brandName.trim();
  return trimmedName.length > 0 ? trimmedName[0].toUpperCase() : 'Б';
}
