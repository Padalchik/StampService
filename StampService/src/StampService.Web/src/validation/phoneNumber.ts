const minDigitsLength = 2;
const maxDigitsLength = 15;
const maxInputLength = 32;
const countryCodeSevenDigitsLength = 11;

export type PhoneNumberValidationResult =
  | { ok: true; value: string }
  | { ok: false; message: string };

export function normalizePhoneNumber(input: string): PhoneNumberValidationResult {
  const trimmed = input.trim();
  if (!trimmed) {
    return invalid();
  }

  if (trimmed.length > maxInputLength) {
    return invalid();
  }

  if (!trimmed.startsWith('+')) {
    return invalid();
  }

  if (countPlus(trimmed) !== 1) {
    return invalid();
  }

  for (const character of trimmed.slice(1)) {
    if (!isAllowedAfterPlus(character)) {
      return invalid();
    }
  }

  const digits = Array.from(trimmed).filter(isAsciiDigit).join('');
  if (digits.length < minDigitsLength || digits.length > maxDigitsLength) {
    return invalid();
  }

  if (digits[0] === '0') {
    return invalid();
  }

  if (digits[0] === '7' && digits.length !== countryCodeSevenDigitsLength) {
    return invalid();
  }

  return { ok: true, value: `+${digits}` };
}

function countPlus(value: string): number {
  return Array.from(value).filter((character) => character === '+').length;
}

function isAllowedAfterPlus(character: string): boolean {
  return isAsciiDigit(character)
    || character === ' '
    || character === '-'
    || character === '('
    || character === ')';
}

function isAsciiDigit(character: string): boolean {
  return character >= '0' && character <= '9';
}

function invalid(): PhoneNumberValidationResult {
  return {
    ok: false,
    message: 'Введите телефон в международном формате, например +7 999 123-45-67.'
  };
}
