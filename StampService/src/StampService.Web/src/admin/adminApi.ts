import { apiRequest } from '../api/apiClient';

export type AdminBrandResponse = {
  brandId: string;
  brandName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  isManualCoinRedemptionEnabled: boolean;
  ownerUserId?: string | null;
  ownerName?: string | null;
  ownerPhoneNumber?: string | null;
};

export type CreateBrandWithOwnerResponse = {
  brandId: string;
  brandName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  isManualCoinRedemptionEnabled: boolean;
  ownerUserId: string;
  ownerName: string;
  ownerPhoneNumber: string;
  membershipId: string;
  createdAt: string;
};

export type ReassignBrandOwnerResponse = {
  brandId: string;
  newOwnerUserId: string;
  newOwnerName: string;
  newOwnerPhoneNumber: string;
  membershipId: string;
  removedOwnerUserId?: string | null;
};

export function getAdminAccess(): Promise<boolean> {
  return apiRequest<boolean>('/api/admin/access');
}

export function getAdminBrands(): Promise<AdminBrandResponse[]> {
  return apiRequest<AdminBrandResponse[]>('/api/admin/brands');
}

export function createBrandWithOwner(
  request: { brandName: string; ownerPhoneNumber: string }
): Promise<CreateBrandWithOwnerResponse> {
  return apiRequest<CreateBrandWithOwnerResponse>('/api/admin/brands', {
    method: 'POST',
    body: request
  });
}

export function reassignBrandOwner(
  brandId: string,
  newOwnerPhoneNumber: string
): Promise<ReassignBrandOwnerResponse> {
  return apiRequest<ReassignBrandOwnerResponse>(`/api/admin/brands/${brandId}/owner`, {
    method: 'PUT',
    body: { newOwnerPhoneNumber }
  });
}

export function createDemoBrands(): Promise<boolean> {
  return apiRequest<boolean>('/api/admin/demo/brands', {
    method: 'POST'
  });
}

export function createUserDemoData(request: { phoneNumber: string; brandId: string }): Promise<boolean> {
  return apiRequest<boolean>('/api/admin/demo/user-data', {
    method: 'POST',
    body: request
  });
}

export function resetDemoDatabase(): Promise<boolean> {
  return apiRequest<boolean>('/api/admin/demo/reset', {
    method: 'POST'
  });
}
