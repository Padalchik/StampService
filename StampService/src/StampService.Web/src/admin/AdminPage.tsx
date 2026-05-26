import { Building2, RefreshCw, ShieldCheck, UserRoundCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { getApiErrorMessage } from '../api/errorMessages';
import { formatRuPhoneInput } from '../validation/phoneNumber';
import {
  createBrandWithOwner,
  getAdminBrands,
  reassignBrandOwner,
  type AdminBrandResponse
} from './adminApi';

export function AdminPage() {
  const [brands, setBrands] = useState<AdminBrandResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

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
      <section className="surface-panel">
        <div className="section-heading section-heading--split">
          <div className="section-heading__title">
            <ShieldCheck size={24} />
            <h2>Админка брендов</h2>
          </div>
          <button className="button-secondary button-compact" type="button" onClick={() => void loadBrands()}>
            <RefreshCw size={16} />
            Обновить
          </button>
        </div>

        {error ? <p className="form-status form-status--error">{error}</p> : null}
        {status ? <p className="form-status form-status--ok">{status}</p> : null}

        <CreateBrandPanel
          onCreated={async (brandName) => {
            setStatus(`Бренд "${brandName}" создан.`);
            await loadBrands();
          }}
        />
      </section>

      <section className="surface-panel">
        <div className="section-heading">
          <Building2 size={24} />
          <h2>Бренды</h2>
        </div>

        {isLoading ? <p className="muted-text">Загружаем бренды...</p> : null}
        {!isLoading && brands.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__meta">Брендов пока нет.</p>
          </div>
        ) : null}

        <div className="brand-list">
          {brands.map((brand) => (
            <AdminBrandCard
              key={brand.brandId}
              brand={brand}
              onOwnerChanged={async (brandName, ownerPhoneNumber) => {
                setStatus(`Владелец бренда "${brandName}" обновлен: ${ownerPhoneNumber}.`);
                await loadBrands();
              }}
            />
          ))}
        </div>
      </section>
    </div>
  );
}

function CreateBrandPanel({ onCreated }: { onCreated: (brandName: string) => Promise<void> }) {
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
    <div className="operation-panel admin-create-panel">
      <div className="operation-panel__heading">
        <Building2 size={20} />
        <h3>Создать бренд</h3>
      </div>
      <div className="metric-create-form">
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
    </div>
  );
}

function AdminBrandCard({
  brand,
  onOwnerChanged
}: {
  brand: AdminBrandResponse;
  onOwnerChanged: (brandName: string, ownerPhoneNumber: string) => Promise<void>;
}) {
  const [ownerPhoneNumber, setOwnerPhoneNumber] = useState(formatRuPhoneInput(''));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function submitOwnerChange() {
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
    <article className="brand-list-item admin-brand-card">
      <div>
        <h3>{brand.brandName}</h3>
        <p>
          Владелец: {brand.ownerName || 'не назначен'}
          {brand.ownerPhoneNumber ? ` · ${brand.ownerPhoneNumber}` : ''}
        </p>
        <div className="workspace-flags admin-brand-card__flags">
          {brand.isMetricsEnabled ? <span>Метрики</span> : null}
          {brand.isCoinsEnabled ? <span>Монетки</span> : null}
          {brand.isCoinProductRedemptionEnabled ? <span>Товары</span> : null}
          {brand.isManualCoinRedemptionEnabled ? <span>Ручное списание</span> : null}
        </div>
        {error ? <p className="form-status form-status--error">{error}</p> : null}
      </div>
      <div className="admin-owner-form">
        <label>
          Новый владелец
          <input
            value={ownerPhoneNumber}
            onChange={(event) => setOwnerPhoneNumber(formatRuPhoneInput(event.target.value))}
          />
        </label>
        <button className="button-secondary" type="button" disabled={isSubmitting} onClick={() => void submitOwnerChange()}>
          <UserRoundCheck size={16} />
          Назначить
        </button>
      </div>
    </article>
  );
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
