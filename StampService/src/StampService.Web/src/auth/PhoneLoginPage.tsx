import { FormEvent, useMemo, useState } from 'react';
import { CheckCircle2, LogIn, MessageSquareText, ShieldCheck } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import { useAuth } from './AuthContext';
import { requestPhoneAuthCode, verifyPhoneAuthCode } from './authApi';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import { formatRuTime } from '../format/dateTime';
import { RuPhoneInput } from '../components/RuPhoneInput';

type LoginStep = 'phone' | 'code';

export function PhoneLoginPage() {
  const { signIn } = useAuth();
  const [step, setStep] = useState<LoginStep>('phone');
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [code, setCode] = useState('');
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [status, setStatus] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const formattedExpiresAt = useMemo(() => {
    if (!expiresAt) {
      return null;
    }

    return formatRuTime(expiresAt);
  }, [expiresAt]);

  async function handleRequestCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setStatus('');
    setIsSubmitting(true);

    try {
      const normalizedPhone = normalizePhoneNumber(phoneNumber);
      if (!normalizedPhone.ok) {
        setError(normalizedPhone.message);
        return;
      }

      const response = await requestPhoneAuthCode(normalizedPhone.value);
      setPhoneNumber(formatRuPhoneInput(normalizedPhone.value));
      setExpiresAt(response.expiresAt);
      setStep('code');
      setStatus('Код подтверждения отправлен.');
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleVerifyCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setStatus('');
    setIsSubmitting(true);

    try {
      const normalizedPhone = normalizePhoneNumber(phoneNumber);
      if (!normalizedPhone.ok) {
        setError(normalizedPhone.message);
        return;
      }

      const response = await verifyPhoneAuthCode(normalizedPhone.value, code.trim());
      signIn(response.token, response.expiresAt);
    } catch (requestError) {
      setError(getUserMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-panel" aria-labelledby="login-title">
        <div className="auth-panel__header">
          <div className="auth-panel__icon" aria-hidden="true">
            <ShieldCheck size={28} />
          </div>
          <div>
            <h1 id="login-title">Вход в StampService</h1>
            <p>Введите телефон и код подтверждения.</p>
          </div>
        </div>

        {step === 'phone' ? (
          <form className="auth-form" onSubmit={handleRequestCode}>
            <label htmlFor="phoneNumber">Телефон</label>
            <RuPhoneInput
              id="phoneNumber"
              name="phoneNumber"
              value={phoneNumber}
              onValueChange={setPhoneNumber}
              required
            />

            <button type="submit" disabled={isSubmitting || !isRuPhoneInputComplete(phoneNumber)}>
              <MessageSquareText size={18} />
              Получить код
            </button>
          </form>
        ) : (
          <form className="auth-form" onSubmit={handleVerifyCode}>
            <div className="auth-summary">
              <CheckCircle2 size={18} />
              <span>
                Код отправлен для номера <strong>{phoneNumber}</strong>
                {formattedExpiresAt ? `, действует до ${formattedExpiresAt}` : ''}.
              </span>
            </div>

            <label htmlFor="authCode">Код подтверждения</label>
            <input
              id="authCode"
              name="authCode"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={8}
              value={code}
              onChange={(event) => setCode(event.target.value)}
              required
            />

            <div className="auth-actions">
              <button type="submit" disabled={isSubmitting || !code.trim()}>
                <LogIn size={18} />
                Войти
              </button>
              <button
                className="button-secondary"
                type="button"
                disabled={isSubmitting}
                onClick={() => {
                  setStep('phone');
                  setPhoneNumber(formatRuPhoneInput(phoneNumber));
                  setCode('');
                  setStatus('');
                  setError('');
                }}
              >
                Изменить телефон
              </button>
            </div>
          </form>
        )}

        {status ? <p className="form-status form-status--ok">{status}</p> : null}
        {error ? <p className="form-status form-status--error">{error}</p> : null}
      </section>
    </main>
  );
}

function getUserMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    if (error.errors.some((item) => item.code === 'auth.phone_invalid')) {
      return 'Введите телефон в формате +7 (999) 123-45-67.';
    }

    if (error.errors.some((item) => item.code === 'auth.phone_code_invalid')) {
      return 'Код неверен или устарел.';
    }

    return error.message;
  }

  return 'Не удалось выполнить запрос. Попробуйте ещё раз.';
}
