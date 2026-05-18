import { apiRequest } from '../api/apiClient';

export type AuthResponse = {
  token: string;
  userId: string;
  expiresAt: string;
};

export type RequestPhoneAuthCodeResponse = {
  expiresAt: string;
};

export function requestPhoneAuthCode(phoneNumber: string): Promise<RequestPhoneAuthCodeResponse> {
  return apiRequest<RequestPhoneAuthCodeResponse>('/api/auth/phone/code', {
    method: 'POST',
    authenticated: false,
    body: { phoneNumber }
  });
}

export function verifyPhoneAuthCode(phoneNumber: string, code: string): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/phone/verify', {
    method: 'POST',
    authenticated: false,
    body: { phoneNumber, code }
  });
}
