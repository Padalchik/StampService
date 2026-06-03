import { FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { CheckCircle2, LogOut, MessageSquareText, Phone, RefreshCw, Send, UserRound } from 'lucide-react';
import { ApiRequestError } from '../api/apiClient';
import { RuPhoneInput } from '../components/RuPhoneInput';
import { formatRuPhoneInput, isRuPhoneInputComplete, normalizePhoneNumber } from '../validation/phoneNumber';
import { formatRuTime } from '../format/dateTime';
import {
  confirmPhoneChangeCode,
  confirmPhoneLinkCode,
  getMyProfile,
  requestPhoneChangeCode,
  requestPhoneLinkCode,
  requestTelegramLink,
  type MyProfileResponse
} from './profileApi';

type PhoneStep = 'idle' | 'code';

export function ProfilePage({ onSignOut }: { onSignOut: () => void }) {
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

  const [telegramStatus, setTelegramStatus] = useState('');
  const [telegramError, setTelegramError] = useState('');
  const [isTelegramSubmitting, setIsTelegramSubmitting] = useState(false);

  useEffect(() => {
    if (hasRequestedProfile.current) {
      return;
    }

    hasRequestedProfile.current = true;
    void loadProfile();
  }, []);

  const phoneExpiresText = useFormattedTime(phoneExpiresAt);

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

      const isChangingPhone = profile?.phone.linked === true;
      const response = isChangingPhone
        ? await requestPhoneChangeCode(normalizedPhone.value)
        : await requestPhoneLinkCode(normalizedPhone.value);
      setPhoneNumber(formatRuPhoneInput(normalizedPhone.value));
      setPhoneAuthCodeId(response.authCodeId);
      setPhoneExpiresAt(response.expiresAtUtc);
      setPhoneStep('code');
      setPhoneStatus(isChangingPhone ? 'Код отправлен на новый номер.' : 'Код подтверждения отправлен.');
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
      const isChangingPhone = profile?.phone.linked === true;
      const response = isChangingPhone
        ? await confirmPhoneChangeCode(phoneNumber, phoneCode.trim(), phoneAuthCodeId)
        : await confirmPhoneLinkCode(phoneNumber, phoneCode.trim(), phoneAuthCodeId);
      setPhoneStatus(
        isChangingPhone
          ? `Телефон изменён: ${response.maskedPhoneNumber}.`
          : `Телефон привязан: ${response.maskedPhoneNumber}.`
      );
      setPhoneStep('idle');
      setPhoneCode('');
      setPhoneNumber(formatRuPhoneInput(''));
      await loadProfile();
    } catch (requestError) {
      setPhoneError(getUserMessage(requestError));
    } finally {
      setIsPhoneSubmitting(false);
    }
  }

  async function handleRequestTelegramLink() {
    setTelegramError('');
    setTelegramStatus('');
    setIsTelegramSubmitting(true);

    try {
      const response = await requestTelegramLink();
      setTelegramStatus('Открываем Telegram. Нажмите Start в боте.');
      window.location.href = response.telegramLinkUrl;
    } catch (requestError) {
      setTelegramError(getUserMessage(requestError));
    } finally {
      setIsTelegramSubmitting(false);
    }
  }

  if (isLoading) {
    return (
      <section className="surface-panel">
        <p className="muted-text">Загружаем личный кабинет...</p>
      </section>
    );
  }

  return (
    <div className="profile-page">
      {loadError ? <p className="form-status form-status--error">{loadError}</p> : null}

      <section className="surface-panel profile-summary">
        <div className="section-heading">
          <UserRound size={22} />
          <h2>Профиль</h2>
        </div>

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

        <button className="button-secondary button-compact" type="button" onClick={() => void loadProfile()}>
          <RefreshCw size={17} />
          Обновить
        </button>
      </section>

      <section className="profile-actions-grid">
        {profile ? (
        <div className="surface-panel profile-action-panel">
          <div className="section-heading">
            <Phone size={22} />
            <h2>{profile.phone.linked ? 'Изменить телефон' : 'Привязать телефон'}</h2>
          </div>

          {phoneStep === 'idle' ? (
            <form className="auth-form" onSubmit={handleRequestPhoneCode}>
              {profile.phone.linked ? (
                <p className="muted-text">Текущий номер: {profile.phone.value ?? 'привязан'}</p>
              ) : null}
              <label htmlFor="profilePhone">{profile.phone.linked ? 'Новый телефон' : 'Телефон'}</label>
              <RuPhoneInput
                id="profilePhone"
                value={phoneNumber}
                onValueChange={setPhoneNumber}
                required
              />
              <button type="submit" disabled={isPhoneSubmitting || !isRuPhoneInputComplete(phoneNumber)}>
                <MessageSquareText size={18} />
                Получить код
              </button>
            </form>
          ) : (
            <form className="auth-form" onSubmit={handleConfirmPhoneCode}>
              <CodeSummary
                text={`Код отправлен для ${phoneNumber}${phoneExpiresText ? `, действует до ${phoneExpiresText}` : ''}.`}
              />
              <label htmlFor="profilePhoneCode">Код подтверждения</label>
              <input
                id="profilePhoneCode"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={8}
                value={phoneCode}
                onChange={(event) => setPhoneCode(event.target.value)}
                required
              />
              <div className="auth-actions">
                <button type="submit" disabled={isPhoneSubmitting || !phoneCode.trim()}>
                  <CheckCircle2 size={18} />
                  Подтвердить
                </button>
                <button
                  className="button-secondary"
                  type="button"
                  disabled={isPhoneSubmitting}
                  onClick={() => {
                    setPhoneStep('idle');
                    setPhoneNumber(formatRuPhoneInput(phoneNumber));
                    setPhoneCode('');
                    setPhoneError('');
                    setPhoneStatus('');
                  }}
                >
                  Исправить номер
                </button>
              </div>
            </form>
          )}

          {phoneStatus ? <p className="form-status form-status--ok">{phoneStatus}</p> : null}
          {phoneError ? <p className="form-status form-status--error">{phoneError}</p> : null}
        </div>
        ) : null}

        {!profile?.telegram.linked ? (
        <div className="surface-panel profile-action-panel">
          <div className="section-heading">
            <Send size={22} />
            <h2>Привязать Telegram</h2>
          </div>

          <div className="auth-form">
            <div className="auth-actions">
              <button type="button" disabled={isTelegramSubmitting} onClick={() => void handleRequestTelegramLink()}>
                <Send size={18} />
                Привязать Telegram
              </button>
              <button className="button-secondary" type="button" onClick={() => void loadProfile()}>
                <RefreshCw size={18} />
                Проверить привязку
              </button>
            </div>
          </div>

          {telegramStatus ? <p className="form-status form-status--ok">{telegramStatus}</p> : null}
          {telegramError ? <p className="form-status form-status--error">{telegramError}</p> : null}
        </div>
        ) : null}
      </section>

      <section className="surface-panel profile-action-panel">
        <div className="section-heading">
          <LogOut size={22} />
          <h2>Сессия</h2>
        </div>

        <button className="button-secondary" type="button" onClick={onSignOut}>
          <LogOut size={18} />
          Выйти из аккаунта
        </button>
      </section>
    </div>
  );
}

function CodeSummary({ text }: { text: string }) {
  return (
    <div className="auth-summary">
      <CheckCircle2 size={18} />
      <span>{text}</span>
    </div>
  );
}

function useFormattedTime(value: string | null): string | null {
  return useMemo(() => {
    if (!value) {
      return null;
    }

    return formatRuTime(value);
  }, [value]);
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

    if (error.errors.some((item) => item.code === 'user.identity_already_linked')) {
      return 'Этот номер уже привязан к вашему профилю.';
    }

    if (error.errors.some((item) => item.code === 'user.identity_not_linked')) {
      return 'Сначала привяжите телефон к профилю.';
    }

    if (error.errors.some((item) => item.code === 'user.telegram_identity_not_linked')) {
      return 'Сначала войдите по подтвержденному телефону.';
    }

    return error.message;
  }

  return 'Не удалось выполнить запрос. Попробуйте ещё раз.';
}
