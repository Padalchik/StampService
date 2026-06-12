import { useEffect, useState, type ReactNode } from 'react';
import { Search } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import { getApiErrorMessage } from '../api/errorMessages';
import { RuPhoneInput } from '../components/RuPhoneInput';
import { formatRuDateTime } from '../format/dateTime';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import {
  getBrandCustomerCard,
  type BrandCustomerCardResponse
} from './brandWorkspaceApi';

const recentCustomerPhonesStoragePrefix = 'stampservice.brandCustomerSearch.recentPhones';
const recentCustomerPhonesLimit = 10;

type RecentCustomerPhone = {
  phoneNumber: string;
  openedAt: string;
};

type BrandCustomerSearchScreenProps = {
  brandId: string;
  onCustomerFound: (customer: BrandCustomerCardResponse) => void;
};

export function BrandCustomerSearchScreen({
  brandId,
  onCustomerFound
}: BrandCustomerSearchScreenProps) {
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [openingPhoneNumber, setOpeningPhoneNumber] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [notFound, setNotFound] = useState(false);
  const [recentPhones, setRecentPhones] = useState<RecentCustomerPhone[]>(() => loadRecentCustomerPhones(brandId));

  useEffect(() => {
    setRecentPhones(loadRecentCustomerPhones(brandId));
  }, [brandId]);

  async function submit() {
    const normalizedPhone = normalizePhoneNumber(phoneNumber);
    if (!isRuPhoneInputComplete(phoneNumber) || !normalizedPhone.ok) {
      setError(normalizedPhone.ok ? 'Укажите телефон клиента.' : normalizedPhone.message);
      setNotFound(false);
      return;
    }

    await openCustomer(normalizedPhone.value);
  }

  async function openCustomer(customerPhoneNumber: string) {
    setOpeningPhoneNumber(customerPhoneNumber);
    setError('');
    setNotFound(false);

    try {
      const response = await getBrandCustomerCard(brandId, customerPhoneNumber);
      setRecentPhones(rememberRecentCustomerPhone(brandId, response.customerPhoneNumber));
      onCustomerFound(response);
    } catch (requestError) {
      if (requestError instanceof ApiRequestError && requestError.status === 404) {
        setNotFound(true);
        return;
      }

      setError(getUserMessage(requestError));
    } finally {
      setOpeningPhoneNumber(null);
    }
  }

  return (
    <>
      <OperationPanel icon={<Search size={20} />} title="Найти клиента">
        <div className="work-form">
          <label>
            Телефон клиента
            <RuPhoneInput value={phoneNumber} onValueChange={setPhoneNumber} />
          </label>
          <button
            type="button"
            disabled={openingPhoneNumber !== null || !isRuPhoneInputComplete(phoneNumber)}
            onClick={() => void submit()}
          >
            Найти клиента
          </button>
        </div>

        {notFound ? (
          <p className="form-status form-status--error">Клиент не найден. Проверьте номер телефона.</p>
        ) : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}
      </OperationPanel>

      <RecentCustomerPhonesTable
        phones={recentPhones}
        openingPhoneNumber={openingPhoneNumber}
        onOpen={(recentPhoneNumber) => void openCustomer(recentPhoneNumber)}
      />
    </>
  );
}

function RecentCustomerPhonesTable({
  phones,
  openingPhoneNumber,
  onOpen
}: {
  phones: RecentCustomerPhone[];
  openingPhoneNumber: string | null;
  onOpen: (phoneNumber: string) => void;
}) {
  return (
    <section className="operation-panel recent-customer-phones">
      <div className="operation-panel__heading">
        <h3>Недавние номера</h3>
      </div>

      {phones.length === 0 ? (
        <p className="muted-text">После открытия карточки номер появится здесь.</p>
      ) : (
        <table className="recent-customer-phones__table">
          <thead>
            <tr>
              <th scope="col">Телефон</th>
              <th scope="col">Дата открытия</th>
              <th scope="col" aria-label="Открыть карточку" />
            </tr>
          </thead>
          <tbody>
            {phones.map((phone) => (
              <tr key={phone.phoneNumber}>
                <td>{formatRuPhoneInput(phone.phoneNumber)}</td>
                <td>{formatRuDateTime(phone.openedAt)}</td>
                <td>
                  <button
                    className="button-secondary button-compact"
                    type="button"
                    disabled={openingPhoneNumber !== null}
                    onClick={() => onOpen(phone.phoneNumber)}
                  >
                    {openingPhoneNumber === phone.phoneNumber ? 'Открываем...' : 'Открыть'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
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

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}

function rememberRecentCustomerPhone(brandId: string, phoneNumber: string): RecentCustomerPhone[] {
  const nextPhones = [
    { phoneNumber, openedAt: new Date().toISOString() },
    ...loadRecentCustomerPhones(brandId).filter((phone) => phone.phoneNumber !== phoneNumber)
  ].slice(0, recentCustomerPhonesLimit);

  saveRecentCustomerPhones(brandId, nextPhones);
  return nextPhones;
}

function loadRecentCustomerPhones(brandId: string): RecentCustomerPhone[] {
  try {
    const rawValue = localStorage.getItem(getRecentCustomerPhonesStorageKey(brandId));
    if (!rawValue) {
      return [];
    }

    const parsedValue: unknown = JSON.parse(rawValue);
    if (!Array.isArray(parsedValue)) {
      return [];
    }

    return parsedValue.filter(isRecentCustomerPhone).slice(0, recentCustomerPhonesLimit);
  } catch {
    return [];
  }
}

function saveRecentCustomerPhones(brandId: string, phones: RecentCustomerPhone[]) {
  try {
    localStorage.setItem(getRecentCustomerPhonesStorageKey(brandId), JSON.stringify(phones));
  } catch {
    // localStorage is a best-effort convenience cache.
  }
}

function getRecentCustomerPhonesStorageKey(brandId: string): string {
  return `${recentCustomerPhonesStoragePrefix}.${brandId}`;
}

function isRecentCustomerPhone(value: unknown): value is RecentCustomerPhone {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const phone = value as Partial<RecentCustomerPhone>;
  return typeof phone.phoneNumber === 'string' && typeof phone.openedAt === 'string';
}
