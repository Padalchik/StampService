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

export type BusinessAuditLogResponse = {
  occurredAt: string;
  operationType: string;
  operationName: string;
  operationStatus: string;
  operationStatusText: string;
  channel: string;
  brandName?: string | null;
  actorName?: string | null;
  customerName?: string | null;
  targetEntityType?: string | null;
  amount?: number | null;
  balanceBefore?: number | null;
  balanceAfter?: number | null;
  reasonCode?: string | null;
  comment?: string | null;
  summary: string;
};

export type BusinessAuditLogsResponse = {
  items: BusinessAuditLogResponse[];
  totalCount: number;
  take: number;
};

export type BusinessAuditLogFilters = {
  occurredFromUtc?: string;
  occurredToUtc?: string;
  brandId?: string;
  customerPhoneNumber?: string;
  actorName?: string;
  operationType?: string;
  operationStatus?: string;
  take?: number;
};

export function getAdminAccess(): Promise<boolean> {
  return apiRequest<boolean>('/api/admin/access');
}

export function getAdminBrands(): Promise<AdminBrandResponse[]> {
  return apiRequest<AdminBrandResponse[]>('/api/admin/brands');
}

export function getBusinessAuditLogs(filters: BusinessAuditLogFilters): Promise<BusinessAuditLogsResponse> {
  const params = new URLSearchParams();
  appendParam(params, 'occurredFromUtc', filters.occurredFromUtc);
  appendParam(params, 'occurredToUtc', filters.occurredToUtc);
  appendParam(params, 'brandId', filters.brandId);
  appendParam(params, 'customerPhoneNumber', filters.customerPhoneNumber);
  appendParam(params, 'actorName', filters.actorName);
  appendParam(params, 'operationType', filters.operationType);
  appendParam(params, 'operationStatus', filters.operationStatus);
  if (filters.take) {
    params.set('take', filters.take.toString());
  }

  const query = params.toString();
  return apiRequest<BusinessAuditLogsResponse>(`/api/admin/audit-logs${query ? `?${query}` : ''}`);
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

function appendParam(params: URLSearchParams, key: string, value?: string) {
  if (value?.trim()) {
    params.set(key, value.trim());
  }
}
