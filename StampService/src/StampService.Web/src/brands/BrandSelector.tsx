import { useEffect, useState } from 'react';
import { Store } from 'lucide-react';
import { getApiErrorMessage } from '../api/errorMessages';
import { getMyBrands, type MyBrandResponse } from './brandWorkspaceApi';

type BrandSelectorProps = {
  initialBrands?: MyBrandResponse[];
  isOpeningBrand?: boolean;
  workspaceError?: string;
  onOpenBrand: (brandId: string) => void;
};

export function BrandSelector({
  initialBrands,
  isOpeningBrand = false,
  workspaceError = '',
  onOpenBrand
}: BrandSelectorProps) {
  const [brands, setBrands] = useState<MyBrandResponse[]>(() => initialBrands ?? []);
  const [isLoading, setIsLoading] = useState(!initialBrands);
  const [error, setError] = useState('');

  useEffect(() => {
    if (initialBrands) {
      setBrands(initialBrands);
      setIsLoading(false);
      return;
    }

    void loadBrands();
  }, [initialBrands]);

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

  return (
    <div className="brand-selector-page">
      <section className="surface-panel">
        <div className="brand-selector-header">
          <div>
            <div className="section-heading__title">
              <Store size={22} />
              <h2>Рабочие бренды</h2>
            </div>
            <p>Выберите бренд для операций с клиентами.</p>
          </div>
        </div>

        {isLoading ? <p className="muted-text">Загружаем бренды...</p> : null}
        {isOpeningBrand ? <p className="muted-text">Открываем рабочую область...</p> : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}
        {workspaceError ? <p className="form-status form-status--error">{workspaceError}</p> : null}

        {!isLoading && brands.length === 0 ? (
          <p className="muted-text">У вас пока нет рабочих брендов.</p>
        ) : null}

        <div className="brand-selector-list">
          {brands.map((brand) => {
            const role = getRolePresentation(brand.roleSystemName);

            return (
              <article className="brand-selector-item" key={brand.brandId}>
                <div className="brand-selector-avatar" aria-hidden="true">
                  {getBrandInitial(brand.brandName)}
                </div>
                <div className="brand-selector-item__content">
                  <h3>{brand.brandName}</h3>
                  <span className={`brand-selector-role brand-selector-role--${role.modifier}`}>
                    {role.label}
                  </span>
                </div>
                <button
                  className="button-secondary button-compact"
                  type="button"
                  disabled={isOpeningBrand}
                  onClick={() => onOpenBrand(brand.brandId)}
                >
                  Открыть
                </button>
              </article>
            );
          })}
        </div>
      </section>
    </div>
  );
}

function getBrandInitial(brandName: string): string {
  const trimmedName = brandName.trim();
  return trimmedName.length > 0 ? trimmedName[0].toUpperCase() : 'Б';
}

function getRolePresentation(roleSystemName: string): { label: string; modifier: 'owner' | 'staff' } {
  const normalizedRole = roleSystemName.trim().toUpperCase();

  if (normalizedRole === 'OWNER') {
    return { label: 'Владелец', modifier: 'owner' };
  }

  return { label: normalizedRole === 'STAFF' ? 'Сотрудник' : 'Участник', modifier: 'staff' };
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
