const ruTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit'
});

const ruDateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
});

export function formatRuTime(value: string | Date): string {
  return ruTimeFormatter.format(new Date(value));
}

export function formatRuDateTime(value: string | Date): string {
  return ruDateTimeFormatter.format(new Date(value));
}
