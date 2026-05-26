import { Alert, Button, Card, Input } from '@heroui/react';
import { FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { CheckCircle2, MessageSquareText, Phone, RefreshCw, UserRound } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import { formatRuTime } from '../format/dateTime';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import {
  confirmPhoneLinkCode,
  getMyProfile,
  requestPhoneLinkCode,
  type MyProfileResponse
} from './profileApi';

type PhoneStep = 'idle' | 'code';

export function ProfileHeroUIFormsPage() {
  const [profile, setProfile] = useState<MyProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const hasRequestedProfile = useRef(false);

  const [phoneNumber, setPhoneNumber] = useState(formatRuPhoneInput(''));
  const [phoneCode, setPhoneCode] = useState('');
  const [phoneAuthCodeId, setPhoneAuthCodeId] = useState('');
  const [phoneExpiresAt, setPhoneExpiresAt] = useState<string | null>(null);
  const [phoneStep, setPhoneStep] = useState<PhoneStep>('idle');
  const [phoneStatus, setPhoneStatus] = useState('');
  const [phoneError, setPhoneError] = useState('');
  const [isPhoneSubmitting, setIsPhoneSubmitting] = useState(false);

  const phoneExpiresText = useMemo(() => {
    return phoneExpiresAt ? formatRuTime(phoneExpiresAt) : null;
  }, [phoneExpiresAt]);

  useEffect(() => {
    if (hasRequestedProfile.current) {
      return;
    }

    hasRequestedProfile.current = true;
    void loadProfile();
  }, []);

  async function loadProfile() {
    setLoadError('');
    setIsLoading(true);

    try {
      const response = await getMyProfile();
      setProfile(response);
    } catch (requestError) {
      setLoadError(getUserMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function handleRequestPhoneCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPhoneError('');
    setPhoneStatus('');
    setIsPhoneSubmitting(true);

    try {
      const normalizedPhone = normalizePhoneNumber(phoneNumber);
      if (!normalizedPhone.ok) {
        setPhoneError(normalizedPhone.message);
        return;
      }

      const response = await requestPhoneLinkCode(normalizedPhone.value);
      setPhoneNumber(formatRuPhoneInput(normalizedPhone.value));
      setPhoneAuthCodeId(response.authCodeId);
      setPhoneExpiresAt(response.expiresAtUtc);
      setPhoneStep('code');
      setPhoneStatus('Код подтверждения отправлен.');
    } catch (requestError) {
      setPhoneError(getUserMessage(requestError));
    } finally {
      setIsPhoneSubmitting(false);
    }
  }

  async function handleConfirmPhoneCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPhoneError('');
    setPhoneStatus('');
    setIsPhoneSubmitting(true);

    try {
      const response = await confirmPhoneLinkCode(phoneNumber, phoneCode.trim(), phoneAuthCodeId);
      setPhoneStatus(`Телефон привязан: ${response.maskedPhoneNumber}.`);
      setPhoneStep('idle');
      setPhoneCode('');
      await loadProfile();
    } catch (requestError) {
      setPhoneError(getUserMessage(requestError));
    } finally {
      setIsPhoneSubmitting(false);
    }
  }

  if (isLoading) {
    return (
      <section className="surface-panel">
        <p className="muted-text">Загружаем данные профиля...</p>
      </section>
    );
  }

  return (
    <div className="profile-page heroui-experiment">
      {loadError ? (
        <Alert status="danger">
          <Alert.Content>
            <Alert.Title>Не удалось загрузить профиль</Alert.Title>
            <Alert.Description>{loadError}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      <Card className="heroui-work-card">
        <Card.Header className="heroui-card-header">
          <UserRound size={22} />
          <div>
            <Card.Title>Профиль</Card.Title>
            <Card.Description>Копия внутренней формы с частичным применением HeroUI.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          {profile ? (
            <dl className="profile-fields">
              <div>
                <dt>Имя</dt>
                <dd>{profile.displayName}</dd>
              </div>
              <div>
                <dt>Телефон</dt>
                <dd>{profile.phone.linked ? profile.phone.value ?? 'привязан' : 'не привязан'}</dd>
              </div>
              <div>
                <dt>Telegram</dt>
                <dd>{profile.telegram.linked ? profile.telegram.value ?? 'привязан' : 'не привязан'}</dd>
              </div>
            </dl>
          ) : null}
        </Card.Content>

        <Card.Footer>
          <Button
            type="button"
            className="heroui-button-outline"
            variant="outline"
            size="md"
            onPress={() => void loadProfile()}
          >
            <RefreshCw size={17} />
            Обновить
          </Button>
        </Card.Footer>
      </Card>

      <Card className="heroui-work-card">
        <Card.Header className="heroui-card-header">
          <Phone size={22} />
          <div>
            <Card.Title>Привязать телефон</Card.Title>
            <Card.Description>Та же операция профиля, но на базовых компонентах HeroUI.</Card.Description>
          </div>
        </Card.Header>

        <Card.Content>
          {profile?.phone.linked ? (
            <Alert status="default">
              <Alert.Content>
                <Alert.Title>Телефон уже привязан</Alert.Title>
                <Alert.Description>Повторная привязка недоступна для обычного сценария профиля.</Alert.Description>
              </Alert.Content>
            </Alert>
          ) : phoneStep === 'idle' ? (
            <form className="heroui-form" onSubmit={handleRequestPhoneCode}>
              <label className="heroui-field">
                <span>Телефон</span>
                <Input
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
                isDisabled={isPhoneSubmitting || !isRuPhoneInputComplete(phoneNumber)}
              >
                <MessageSquareText size={18} />
                Получить код
              </Button>
            </form>
          ) : (
            <form className="heroui-form" onSubmit={handleConfirmPhoneCode}>
              <Alert status="success">
                <Alert.Indicator>
                  <CheckCircle2 size={18} />
                </Alert.Indicator>
                <Alert.Content>
                  <Alert.Title>Код отправлен</Alert.Title>
                  <Alert.Description>
                    Для номера {phoneNumber}
                    {phoneExpiresText ? `, действует до ${phoneExpiresText}` : ''}.
                  </Alert.Description>
                </Alert.Content>
              </Alert>

              <label className="heroui-field">
                <span>Код подтверждения</span>
                <Input
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  maxLength={8}
                  value={phoneCode}
                  onChange={(event) => setPhoneCode(event.target.value)}
                  fullWidth
                  required
                />
              </label>

              <div className="heroui-actions">
                <Button
                  type="submit"
                  variant="primary"
                  size="lg"
                  isDisabled={isPhoneSubmitting || !phoneCode.trim()}
                >
                  <CheckCircle2 size={18} />
                  Подтвердить
                </Button>
                <Button
                  type="button"
                  className="heroui-button-outline"
                  variant="outline"
                  size="lg"
                  isDisabled={isPhoneSubmitting}
                  onPress={() => {
                    setPhoneStep('idle');
                    setPhoneNumber(formatRuPhoneInput(phoneNumber));
                    setPhoneCode('');
                    setPhoneError('');
                    setPhoneStatus('');
                  }}
                >
                  Исправить номер
                </Button>
              </div>
            </form>
          )}

          <HeroUIStatus status={phoneStatus} error={phoneError} />
        </Card.Content>
      </Card>
    </div>
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
      return 'Код телефона неверен или устарел.';
    }

    if (error.errors.some((item) => item.code === 'user.identity_linked_to_another_user')) {
      return 'Этот способ входа уже привязан к другому пользователю.';
    }

    return error.message;
  }

  return 'Не удалось выполнить запрос. Попробуйте еще раз.';
}
