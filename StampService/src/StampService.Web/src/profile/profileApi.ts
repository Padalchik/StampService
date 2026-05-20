import { apiRequest } from '../api/apiClient';

export type IdentityStatusResponse = {
  linked: boolean;
  value?: string | null;
};

export type MyProfileResponse = {
  userId: string;
  displayName: string;
  customerCode: string;
  telegram: IdentityStatusResponse;
  phone: IdentityStatusResponse;
};

export type RequestPhoneLinkCodeResponse = {
  expiresAtUtc: string;
  authCodeId: string;
};

export type ConfirmPhoneLinkCodeResponse = {
  phoneNumber: string;
  maskedPhoneNumber: string;
};

export type RequestTelegramLinkResponse = {
  telegramLinkUrl: string;
  expiresAtUtc: string;
};

export type ConfirmTelegramLinkResponse = {
  telegramUserId: number;
  displayName: string;
};

export function getMyProfile(): Promise<MyProfileResponse> {
  return apiRequest<MyProfileResponse>('/api/users/me');
}

export function requestPhoneLinkCode(phoneNumber: string): Promise<RequestPhoneLinkCodeResponse> {
  return apiRequest<RequestPhoneLinkCodeResponse>('/api/users/me/phone/code', {
    method: 'POST',
    body: { phoneNumber }
  });
}

export function confirmPhoneLinkCode(
  phoneNumber: string,
  code: string,
  authCodeId: string
): Promise<ConfirmPhoneLinkCodeResponse> {
  return apiRequest<ConfirmPhoneLinkCodeResponse>('/api/users/me/phone/verify', {
    method: 'POST',
    body: { phoneNumber, code, authCodeId }
  });
}

export function requestTelegramLink(): Promise<RequestTelegramLinkResponse> {
  return apiRequest<RequestTelegramLinkResponse>('/api/users/me/telegram/link', {
    method: 'POST'
  });
}
