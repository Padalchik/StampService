import { useState, type ReactNode } from 'react';
import { Search } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import { getApiErrorMessage } from '../api/errorMessages';
import { RuPhoneInput } from '../components/RuPhoneInput';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import {
  getBrandCustomerCard,
  type BrandCustomerCardResponse
} from './brandWorkspaceApi';

type BrandCustomerSearchScreenProps = {
  brandId: string;
  onCustomerFound: (customer: BrandCustomerCardResponse) => void;
};

export function BrandCustomerSearchScreen({
  brandId,
  onCustomerFound
}: BrandCustomerSearchScreenProps) {
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [notFound, setNotFound] = useState(false);

  async function submit() {
    const normalizedPhone = normalizePhoneNumber(phoneNumber);
    if (!isRuPhoneInputComplete(phoneNumber) || !normalizedPhone.ok) {
      setError(normalizedPhone.ok ? 'Укажите телефон клиента.' : normalizedPhone.message);
      setNotFound(false);
      return;
    }

    setIsLoading(true);
    setError('');
    setNotFound(false);

    try {
      const response = await getBrandCustomerCard(brandId, normalizedPhone.value);
      onCustomerFound(response);
    } catch (requestError) {
      if (requestError instanceof ApiRequestError && requestError.status === 404) {
        setNotFound(true);
        return;
      }

      setError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <OperationPanel icon={<Search size={20} />} title="Найти клиента">
      <div className="work-form">
        <label>
          Телефон клиента
          <RuPhoneInput value={phoneNumber} onValueChange={setPhoneNumber} />
        </label>
        <button type="button" disabled={isLoading || !isRuPhoneInputComplete(phoneNumber)} onClick={() => void submit()}>
          Найти клиента
        </button>
      </div>

      {notFound ? (
        <p className="form-status form-status--error">Клиент не найден. Проверьте номер телефона.</p>
      ) : null}
      {error ? <p className="form-status form-status--error">{error}</p> : null}
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

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
