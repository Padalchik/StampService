import { apiRequest } from '../api/apiClient';

export type AuthResponse = {
  token: string;
  userId: string;
  expiresAt: string;
};

export type RequestPhoneAuthCodeResponse = {
  expiresAt: string;
};

export type PhoneAuthSmsSettingsResponse = {
  isEnabled: boolean;
};

export function requestPhoneAuthCode(phoneNumber: string, sendSms = false): Promise<RequestPhoneAuthCodeResponse> {
  return apiRequest<RequestPhoneAuthCodeResponse>('/api/auth/phone/code', {
    method: 'POST',
    authenticated: false,
    body: { phoneNumber, sendSms }
  });
}

export function getPhoneAuthSmsSettings(): Promise<PhoneAuthSmsSettingsResponse> {
  return apiRequest<PhoneAuthSmsSettingsResponse>('/api/auth/phone/sms-settings', {
    authenticated: false
  });
}

export function verifyPhoneAuthCode(phoneNumber: string, code: string): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/phone/verify', {
    method: 'POST',
    authenticated: false,
    body: { phoneNumber, code }
  });
}
