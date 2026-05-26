import { Alert, Button, Card, Input } from '@heroui/react';
import { FormEvent, useMemo, useState } from 'react';
import { CheckCircle2, LogIn, MessageSquareText, ShieldCheck } from 'lucide-react';
import { Link } from 'react-router-dom';
import { ApiRequestError } from '../api/apiClient';
import { formatRuTime } from '../format/dateTime';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import { requestPhoneAuthCode, verifyPhoneAuthCode } from './authApi';
import { useAuth } from './AuthContext';

type LoginStep = 'phone' | 'code';

export function PhoneLoginHeroUIPage() {
  const { signIn } = useAuth();
  const [step, setStep] = useState<LoginStep>('phone');
  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [code, setCode] = useState('');
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const formattedExpiresAt = useMemo(() => {
    return expiresAt ? formatRuTime(expiresAt) : null;
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
    <main className="auth-page heroui-experiment">
      <Card className="heroui-auth-card" aria-labelledby="login-heroui-title">
        <Card.Header className="heroui-card-header">
          <div className="auth-panel__icon" aria-hidden="true">
            <ShieldCheck size={28} />
          </div>
          <div>
            <Card.Title id="login-heroui-title">Вход в StampService</Card.Title>
            <Card.Description>Копия формы входа с частичным применением HeroUI.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          {step === 'phone' ? (
            <form className="heroui-form" onSubmit={handleRequestCode}>
              <label className="heroui-field">
                <span>Телефон</span>
                <Input
                  name="phoneNumber"
                  type="tel"
                  inputMode="tel"
                  autoComplete="tel"
                  value={phoneNumber}
                  onChange={(event) => setPhoneNumber(formatRuPhoneInput(event.target.value))}
                  fullWidth
                  required
                />
              </label>

              <Button
                type="submit"
                variant="primary"
                size="lg"
                fullWidth
                isDisabled={isSubmitting || !isRuPhoneInputComplete(phoneNumber)}
              >
                <MessageSquareText size={18} />
                Получить код
              </Button>
            </form>
          ) : (
            <form className="heroui-form" onSubmit={handleVerifyCode}>
              <Alert status="success">
                <Alert.Indicator>
                  <CheckCircle2 size={18} />
                </Alert.Indicator>
                <Alert.Content>
                  <Alert.Title>Код отправлен</Alert.Title>
                  <Alert.Description>
                    Для номера {phoneNumber}
                    {formattedExpiresAt ? `, действует до ${formattedExpiresAt}` : ''}.
                  </Alert.Description>
                </Alert.Content>
              </Alert>

              <label className="heroui-field">
                <span>Код подтверждения</span>
                <Input
                  name="authCode"
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  maxLength={8}
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  fullWidth
                  required
                />
              </label>

              <div className="heroui-actions">
                <Button type="submit" variant="primary" size="lg" isDisabled={isSubmitting || !code.trim()}>
                  <LogIn size={18} />
                  Войти
                </Button>
                <Button
                  type="button"
                  className="heroui-button-outline"
                  variant="outline"
                  size="lg"
                  isDisabled={isSubmitting}
                  onPress={() => {
                    setStep('phone');
                    setPhoneNumber(formatRuPhoneInput(phoneNumber));
                    setCode('');
                    setStatus('');
                    setError('');
                  }}
                >
                  Изменить телефон
                </Button>
              </div>
            </form>
          )}

          <HeroUIStatus status={status} error={error} />
        </Card.Content>

        <Card.Footer>
          <Link className="button-link button-secondary" to="/login">
            Открыть обычную форму
          </Link>
        </Card.Footer>
      </Card>
    </main>
  );
}

function HeroUIStatus({ status, error }: { status: string; error: string }) {
  if (error) {
    return (
      <Alert className="heroui-status" status="danger">
        <Alert.Content>
          <Alert.Title>Не удалось выполнить действие</Alert.Title>
          <Alert.Description>{error}</Alert.Description>
        </Alert.Content>
      </Alert>
    );
  }

  if (status) {
    return (
      <Alert className="heroui-status" status="success">
        <Alert.Content>
          <Alert.Title>Готово</Alert.Title>
          <Alert.Description>{status}</Alert.Description>
        </Alert.Content>
      </Alert>
    );
  }

  return null;
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

  return 'Не удалось выполнить запрос. Попробуйте еще раз.';
}
