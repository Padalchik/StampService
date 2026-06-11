import { RefreshCw } from 'lucide-react';

export function RedemptionCodeCard({
  code,
  isRefreshing,
  error,
  className = '',
  onRefresh
}: {
  code?: string | null;
  isRefreshing: boolean;
  error?: string;
  className?: string;
  onRefresh: () => void;
}) {
  const cardClassName = ['wallet-code-card', className].filter(Boolean).join(' ');

  return (
    <section className={cardClassName} aria-label="Код для списания">
      <div className="wallet-code-card__content">
        <span className="wallet-code-card__label">Код для списания</span>
        <div className="wallet-code-card__row">
          <div className="redemption-code wallet-code-card__code">{code ?? '----'}</div>
          <button
            className="wallet-code-card__refresh"
            type="button"
            aria-label="Обновить код"
            disabled={isRefreshing}
            onClick={onRefresh}
          >
            <RefreshCw size={20} aria-hidden="true" />
          </button>
        </div>
      </div>

      {error ? <p className="form-status form-status--error">{error}</p> : null}
    </section>
  );
}
