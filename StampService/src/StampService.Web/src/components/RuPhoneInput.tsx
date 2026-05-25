import { type ChangeEvent, type InputHTMLAttributes } from 'react';
import { formatRuPhoneInput } from '../validation/phoneNumber';

type RuPhoneInputProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'inputMode' | 'onChange' | 'placeholder' | 'type' | 'value'
> & {
  value: string;
  onValueChange: (value: string) => void;
  placeholder?: string;
};

export function RuPhoneInput({
  value,
  onValueChange,
  placeholder = '+7 (999) 123-45-67',
  autoComplete = 'tel',
  ...props
}: RuPhoneInputProps) {
  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    onValueChange(formatRuPhoneInput(event.target.value, {
      previousValue: value,
      inputType: (event.nativeEvent as InputEvent).inputType,
      selectionStart: event.target.selectionStart
    }));
  }

  return (
    <input
      {...props}
      type="tel"
      inputMode="tel"
      autoComplete={autoComplete}
      placeholder={placeholder}
      value={value}
      onChange={handleChange}
    />
  );
}
