const minDigitsLength = 2;
const maxDigitsLength = 15;
const maxInputLength = 32;
const countryCodeSevenDigitsLength = 11;

export type PhoneNumberValidationResult =
  | { ok: true; value: string }
  | { ok: false; message: string };

export function formatRuPhoneInput(input: string): string {
  const localDigits = getRuLocalDigits(input);
  const operator = localDigits.slice(0, 3);
  const firstPart = localDigits.slice(3, 6);
  const secondPart = localDigits.slice(6, 8);
  const thirdPart = localDigits.slice(8, 10);

  let value = '+7';

  if (operator) {
    value += ` (${operator}`;
  }

  if (operator.length === 3) {
    value += ')';
  }

  if (firstPart) {
    value += ` ${firstPart}`;
  }

  if (secondPart) {
    value += `-${secondPart}`;
  }

  if (thirdPart) {
    value += `-${thirdPart}`;
  }

  return value;
}

export function isRuPhoneInputComplete(input: string): boolean {
  return getRuLocalDigits(input).length === 10;
}

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

function getRuLocalDigits(input: string): string {
  const trimmed = input.trim();
  const digits = Array.from(input).filter(isAsciiDigit).join('');
  if (!digits) {
    return '';
  }

  let localDigits = digits;
  if (trimmed.startsWith('+7') && localDigits[0] === '7') {
    localDigits = localDigits.slice(1);
  } else if ((localDigits[0] === '7' || localDigits[0] === '8') && localDigits.length > 10) {
    localDigits = localDigits.slice(1);
  }

  while (localDigits[0] === '7' || localDigits[0] === '8') {
    localDigits = localDigits.slice(1);
  }

  return localDigits.slice(0, 10);
}

function invalid(): PhoneNumberValidationResult {
  return {
    ok: false,
    message: 'Введите телефон в формате +7 (999) 123-45-67.'
  };
}
